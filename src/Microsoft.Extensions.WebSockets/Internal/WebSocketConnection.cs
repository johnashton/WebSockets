using System;
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
        /// Receives the next available frame.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the receive is cancelled.</param>
        /// <returns>A <see cref="Task{WebSocketMessage}"/> that completes with the message when it has been received.</returns>
        // TODO: De-taskify this to allow consumers to create their own awaiter.
        public async Task<WebSocketFrame> ReceiveAsync(CancellationToken cancellationToken)
        {
            // WebSocket Frame layout:
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
            var header = new WebSocketFrameHeader();
            byte[] payload = null;
            var remainingPayloadBytes = 0;
            var maskOffset = 0;
            WebSocketFrame completedFrame = null;
            while (completedFrame == null)
            {
                if (_activeBuffer.IsEmpty)
                {
                    _activeBuffer = await _connection.ReadAsync();
                }

                // Read the header
                if (nextField != NextField.Payload)
                {
                    var consumed = 0;
                    foreach (var segment in _activeBuffer)
                    {
                        foreach (var byt in segment)
                        {
                            switch (nextField)
                            {
                                case NextField.Opcode:
                                    header.Fin = (byt & 0x1) != 0;
                                    header.Opcode = (WebSocketOpcode)((byt & 0xF0) << 4);

                                    nextField = NextField.MaskedAndLen;
                                    consumed++;
                                    break;
                                case NextField.MaskedAndLen:
                                    header.Masked = (byt & 0x1) != 0;
                                    var len = (byt & 0xFE) << 1;
                                    if (len == 127)
                                    {
                                        remainingPayloadBytes = 8;
                                        nextField = NextField.ExtendedLen;
                                    }
                                    else if (len == 126)
                                    {
                                        remainingPayloadBytes = 2;
                                        nextField = NextField.ExtendedLen;
                                    }
                                    else
                                    {
                                        header.PayloadLength = len;
                                        nextField = header.Masked ? NextField.MaskingKey : NextField.Payload;
                                    }
                                    consumed++;
                                    break;
                                case NextField.ExtendedLen:
                                    // Add the byte to the value
                                    header.PayloadLength = (header.PayloadLength << 8) + byt;
                                    remainingPayloadBytes--;
                                    if (remainingPayloadBytes == 0)
                                    {
                                        nextField = header.Masked ? NextField.MaskingKey : NextField.Payload;
                                    }
                                    consumed++;
                                    break;
                                case NextField.MaskingKey:
                                    if (header.MaskingKey == null)
                                    {
                                        header.MaskingKey = new byte[4];
                                    }
                                    header.MaskingKey[maskOffset] = byt;
                                    maskOffset++;
                                    if (maskOffset == 4)
                                    {
                                        nextField = NextField.Payload;
                                    }
                                    consumed++;
                                    break;
                                default:
                                    throw new InvalidOperationException("Invalid state!");
                            }
                        }
                    }
                    _connection.Consumed(consumed);
                    _activeBuffer = _activeBuffer.Slice(consumed);
                }
            }
            return completedFrame;
        }

        private enum NextField
        {
            Opcode,
            Payload,
            MaskedAndLen,
            ExtendedLen,
            MaskingKey
        }

        private struct WebSocketFrameHeader
        {
            public bool Fin;
            public WebSocketOpcode Opcode;
            public long PayloadLength;
            public bool Masked;
            public byte[] MaskingKey; // TODO: Slice ByteBuffer?
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
