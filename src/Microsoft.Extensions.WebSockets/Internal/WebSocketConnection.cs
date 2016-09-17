using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities.Internal;

namespace Microsoft.Extensions.WebSockets.Internal
{
    /// <summary>
    /// Provides the default implementation of <see cref="IWebSocketConnection"/>.
    /// </summary>
    public class WebSocketConnection : IWebSocketConnection
    {
        private readonly AwaitableStream _connection;
        private readonly Task _connectionCompleted;
        private ByteBuffer _activeBuffer = default(ByteBuffer);

        /// <summary>
        /// Gets the subprotocol that was negotiated during handshaking
        /// </summary>
        public string SubProtocol { get; }

        /// <summary>
        /// Constructs a new <see cref="WebSocketConnection"/> from an <see cref="Stream"/> that represents an established WebSocket connection (i.e. after handshaking)
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="subProtocol"></param>
        public WebSocketConnection(Stream connection, string subProtocol)
        {
            SubProtocol = subProtocol;

            // Start the AwaitableStream via CopyToAsync and capture the Task
            // It represents the end of the connection.
            _connection = new AwaitableStream();
            _connectionCompleted = connection.CopyToAsync(_connection);
        }

        /// <summary>
        /// Intended only to support testing and internal infrastructure. Do not use unless you really know what you're doing.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="subProtocol"></param>
        public WebSocketConnection(AwaitableStream connection, string subProtocol)
        {
            SubProtocol = subProtocol;
            _connection = connection;
        }

        /// <summary>
        /// Receives the next available frame.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the receive is cancelled.</param>
        /// <returns>A <see cref="Task{WebSocketMessage}"/> that completes with the message when it has been received.</returns>
        // TODO: De-taskify this to allow consumers to create their own awaiter.
        public async Task<WebSocketFrame> ReceiveAsync(CancellationToken cancellationToken)
        {
            // WebSocket Frame layout (https://tools.ietf.org/html/rfc6455#section-5.2):
            //      0                   1                   2                   3
            //      0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            //     +-+-+-+-+-------+-+-------------+-------------------------------+
            //     |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
            //     |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
            //     |N|V|V|V|       |S|             |   (if payload len==126/127)   |
            //     | |1|2|3|       |K|             |                               |
            //     +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
            //     |     Extended payload length continued, if payload len == 127  |
            //     + - - - - - - - - - - - - - - - +-------------------------------+
            //     |                               |Masking-key, if MASK set to 1  |
            //     +-------------------------------+-------------------------------+
            //     | Masking-key (continued)       |          Payload Data         |
            //     +-------------------------------- - - - - - - - - - - - - - - - +
            //     :                     Payload Data continued ...                :
            //     + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
            //     |                     Payload Data continued ...                |
            //     +---------------------------------------------------------------+

            var nextField = NextField.Opcode;
            var state = new ParseState();
            while (nextField != NextField.Complete)
            {
                if(state.BufferOffset >= _activeBuffer.Length)
                {
                    _activeBuffer = await _connection.ReadAsync();
                    state.BufferOffset = 0;
                }

                var startOffset = state.BufferOffset;
                while(nextField != NextField.Complete && state.BufferOffset < _activeBuffer.Length)
                {
                    nextField = ParseNextField(nextField, ref state);
                }
                _connection.Consumed(state.BufferOffset - startOffset);
            }

            Debug.Assert(state.Frame != null);

            // Save the rest (if any) for later
            _activeBuffer = _activeBuffer.Slice(state.BufferOffset);
            return state.Frame;
        }

        private NextField ParseNextField(NextField currentField, ref ParseState state)
        {
            switch (currentField)
            {
                case NextField.Opcode: return ParseOpcode(ref state);
                case NextField.MaskedAndLen: return ParseMaskedAndLen(ref state);
                case NextField.ExtendedLen: return ParseExtendedLength(ref state);
                case NextField.MaskingKey: return ParseMaskingKey(ref state);
                case NextField.Payload: return ParsePayload(ref state);
                default:
                    throw new InvalidOperationException("Unexpected state: " + currentField.ToString());
            }
        }

        private NextField ParsePayload(ref ParseState state)
        {
            var bytesToRead = (int)Math.Min(_activeBuffer.Length - state.BufferOffset, state.PayloadLength);
            state.Payload = ByteBuffer.Concat(state.Payload, _activeBuffer.Slice(state.BufferOffset, bytesToRead));
            state.BufferOffset += bytesToRead;
            state.PayloadLength -= bytesToRead;

            if(state.PayloadLength == 0)
            {
                // We've read it all, we're done!
                // TODO: Unmasking
                // TODO: Close payload
                // TODO: Work out how we actually want to propagate the payload, doing it this way will often force a copy
                state.Frame = new WebSocketFrame(state.Fin, state.Opcode, state.Payload.GetArraySegment());
                return NextField.Complete;
            }
            else
            {
                return NextField.Payload;
            }
        }

        private NextField ParseMaskingKey(ref ParseState state)
        {
            var bytesLeft = 4 - state.MaskingKey.Length;
            var bytesToRead = (int)Math.Min(_activeBuffer.Length - state.BufferOffset, bytesLeft);
            state.MaskingKey = ByteBuffer.Concat(state.MaskingKey, _activeBuffer.Slice(state.BufferOffset, bytesToRead));
            state.BufferOffset += bytesToRead;

            if (state.MaskingKey.Length == 4)
            {
                return NextField.Payload;
            }
            else
            {
                return NextField.MaskingKey;
            }
        }

        private NextField ParseExtendedLength(ref ParseState state)
        {
            var byt = _activeBuffer[state.BufferOffset];
            state.BufferOffset += 1;

            state.PayloadLength = (state.PayloadLength << 8) + byt;
            state.LengthSizeInBytes--;
            if (state.LengthSizeInBytes == 0)
            {
                return state.Masked ? NextField.MaskingKey : NextField.Payload;
            }
            else
            {
                return NextField.ExtendedLen;
            }
        }

        private NextField ParseMaskedAndLen(ref ParseState state)
        {
            var byt = _activeBuffer[state.BufferOffset];
            state.BufferOffset += 1;

            state.Masked = (byt & 0x01) != 0;
            var len = (byt & 0xFE) >> 1;
            if (len == 127)
            {
                state.LengthSizeInBytes = 8;
                return NextField.ExtendedLen;
            }
            else if (len == 126)
            {
                state.LengthSizeInBytes = 2;
                return NextField.ExtendedLen;
            }
            else
            {
                state.PayloadLength = len;
                return state.Masked ? NextField.MaskingKey : NextField.Payload;
            }
        }

        private NextField ParseOpcode(ref ParseState state)
        {
            var byt = _activeBuffer[state.BufferOffset];
            state.BufferOffset += 1;

            state.Fin = (byt & 0x01) != 0;
            state.Opcode = (WebSocketOpcode)((byt & 0xF0) >> 4);
            return NextField.MaskedAndLen;
        }

        private enum NextField
        {
            Opcode,
            Payload,
            MaskedAndLen,
            ExtendedLen,
            MaskingKey,
            Complete
        }

        private struct ParseState
        {
            public bool Fin;
            public WebSocketOpcode Opcode;
            public long PayloadLength;
            public bool Masked;
            public ByteBuffer MaskingKey;
            public ByteBuffer Payload;
            public int BufferOffset;
            public int LengthSizeInBytes;
            public WebSocketFrame Frame;
        }

        /// <summary>
        /// Sends the specified frame.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been written to the outbound stream.</returns>
        // TODO: De-taskify this to allow consumers to create their own awaiter.
        public Task SendAsync(WebSocketFrame message, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
