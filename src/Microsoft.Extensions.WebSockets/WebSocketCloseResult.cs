namespace Microsoft.Extensions.WebSockets
{
    public class WebSocketCloseResult
    {
        public WebSocketCloseStatus? CloseStatus { get; }
        public string CloseStatusDescription { get; }
    }
}