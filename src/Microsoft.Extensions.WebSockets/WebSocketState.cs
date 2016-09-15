namespace Microsoft.Extensions.WebSockets
{
    public enum WebSocketState
    {
        None,
        Connecting,
        Open,
        CloseReceived,
        CloseSent,
        Closed
    }
}