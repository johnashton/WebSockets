using System;

namespace Microsoft.Extensions.WebSockets
{
    public class WebSocketFrame
    {
        public bool EndOfMessage { get; }
        public WebSocketOpcode Opcode { get; }
        public ArraySegment<byte> Payload { get; }

        public WebSocketCloseResult CloseResult { get; }
    }
}