namespace Microsoft.Extensions.WebSockets
{
    public enum WebSocketOpcode
    {
        Continuation = 0x0,
        Text = 0x1,
        Binary = 0x2,
        /* 0x3 - 0x7 are reserved */
        Close = 0x8,
        Ping = 0x9,
        Pong = 0xA,
        /* 0xB-0xF are reserved */

        /* all opcodes above 0xF are invalid */
    }
}