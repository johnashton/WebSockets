using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.WebSockets
{
    // REVIEW: THIS IS A TERRIBLE NAME!
    public abstract class WebSocket2
    {
        /// <summary>
        /// Gets the subprotocol that was negotiated during handshaking
        /// </summary>
        public abstract string SubProtocol { get; }

        // REVIEW: This could actually be handled by having the Send/Receive methods be 'protected abstract' and then having wrappers
        // handle the actual state transitions. As it is, we would depend upon the sub-class properly handling state transitions when certain
        // messages arrive (i.e. Close frames)
        /// <summary>
        /// Gets the current state of the socket
        /// </summary>
        public abstract WebSocketState State { get; }

        /// <summary>
        /// Sends the specified message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the send is cancelled.</param>
        /// <returns>A <see cref="Task"/> that completes when the message has been written to the outbound stream.</returns>
        public abstract Task SendAsync(WebSocketFrame message, CancellationToken cancellationToken);

        /// <summary>
        /// Receives the next available message.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the receive is cancelled.</param>
        /// <returns>A <see cref="Task{WebSocketMessage}"/> that completes with the message when it has been received.</returns>
        public abstract Task<WebSocketFrame> ReceiveAsync(CancellationToken cancellationToken);

        // Proposed additional methods, which can call be implemented using the above:
        /// <summary>
        /// Initiate or complete the close handshake with the specified <see cref="WebSocketCloseStatus"/> and description.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If called by a socket in the <see cref="WebSocketState.Open"/> state (i.e. neither party has initiated a close yet),
        /// this sends the Close frame AND waits for the handshake to complete (i.e. for the other party to return a close frame,
        /// or for the underlying connection to be closed). If a close frame is received from the other party, a
        /// <see cref="WebSocketCloseResult"/> is returned with the status and description sent by the other party. If the underlying
        /// connection is terminated, a <see cref="WebSocketCloseResult"/> with a status code of
        /// <see cref="WebSocketCloseStatus.AbnormalClosure"/> is returned.
        /// </para>
        /// <para>
        /// If called by a socket in the <see cref="WebSocketState.CloseReceived"/> state (i.e. the socket has received a close message from the
        /// other party), this sends the Close frame and completes when the Close frame has been successfully written to the outbound stream.
        /// The original <see cref="WebSocketCloseResult"/> from the initial Close frame is returned.
        /// </para>
        /// <param name="status">A <see cref="WebSocketCloseStatus"/> indicating the status code to use for the close.</param>
        /// <param name="description">A description of the reason for the closure.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that indicates when/if the close is cancelled.</param>
        /// <returns>A <see cref="Task{WebSocketCloseResult}"/> that completes when both parties have completed the close handshake (or the underlying connection has terminated).</returns>
        public virtual Task<WebSocketCloseResult> CloseAsync(WebSocketCloseStatus status, string description, CancellationToken cancellationToken) { throw new NotImplementedException(); }
    }
}
