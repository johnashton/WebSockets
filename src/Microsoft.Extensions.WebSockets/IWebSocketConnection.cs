using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.WebSockets
{
    /// <summary>
    /// Represents a connection to a WebSocket endpoint.
    /// </summary>
    public interface IWebSocketConnection
    {
        /// <summary>
        /// Gets the subprotocol that was negotiated during handshaking
        /// </summary>
        string SubProtocol { get; }

        /// <summary>
        /// Sends the specified frame.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been written to the outbound stream.</returns>
        // TODO: De-taskify this to allow consumers to create their own awaiter.
        Task SendAsync(WebSocketFrame message, CancellationToken cancellationToken);

        /// <summary>
        /// Receives the next available frame.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the receive is cancelled.</param>
        /// <returns>A <see cref="Task{WebSocketMessage}"/> that completes with the message when it has been received.</returns>
        // TODO: De-taskify this to allow consumers to create their own awaiter.
        Task<WebSocketFrame> ReceiveAsync(CancellationToken cancellationToken);
    }
}
