namespace Microsoft.Extensions.WebSockets
{
    /// <summary>
    /// Represents the payload of a Close frame (i.e. a <see cref="WebSocketFrame"/> with an <see cref="WebSocketFrame.Opcode"/> of <see cref="WebSocketOpcode.Close"/>).
    /// </summary>
    public struct WebSocketCloseResult
    {
        /// <summary>
        /// Gets the close status code specified in the frame.
        /// </summary>
        public WebSocketCloseStatus Status { get; }

        /// <summary>
        /// Gets the close status description specified in the frame.
        /// </summary>
        public string Description { get; }
    }
}