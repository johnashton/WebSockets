using System;

namespace Microsoft.Extensions.WebSockets
{
    /// <summary>
    /// Represents a single Frame received or sent on a <see cref="IWebSocketConnection"/>.
    /// </summary>
    public class WebSocketFrame
    {
        /// <summary>
        /// Indicates if the "FIN" flag is set on this frame, which indicates it is the final frame of a message.
        /// </summary>
        public bool EndOfMessage { get; }

        /// <summary>
        /// Gets the <see cref="WebSocketOpcode"/> value describing the opcode of the WebSocket frame.
        /// </summary>
        public WebSocketOpcode Opcode { get; }

        /// <summary>
        /// Gets the payload of the WebSocket frame, if any.
        /// </summary>
        /// <remarks>
        /// When there is no payload expected (i.e. in the case of a Close frame), this value is an empty array.
        /// </remarks>
        public ArraySegment<byte> Payload { get; }

        /// <summary>
        /// Gets the close status and reason from the payload, if this frame is a Close frame
        /// (i.e. <see cref="Opcode"/> is <see cref="WebSocketOpcode.Close"/>). If this frame is not a Close frame,
        /// this property is null.
        /// </summary>
        public WebSocketCloseResult? CloseResult { get; }

        public WebSocketFrame(bool endOfMessage, WebSocketOpcode opcode, ArraySegment<byte> payload)
        {
            EndOfMessage = endOfMessage;
            Opcode = opcode;
            Payload = payload;
        }

        public WebSocketFrame(bool endOfMessage, WebSocketOpcode opcode, WebSocketCloseResult closeResult)
        {
            EndOfMessage = endOfMessage;
            Opcode = opcode;
            CloseResult = closeResult;
        }
    }
}