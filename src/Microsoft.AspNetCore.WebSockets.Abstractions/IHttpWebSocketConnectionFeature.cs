using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.WebSockets;

namespace Microsoft.AspNetCore.WebSockets.Abstractions
{
    /// <summary>
    /// Represents features related to handling WebSocket requests using <see cref="IWebSocketConnection"/>.
    /// </summary>
    public interface IHttpWebSocketConnectionFeature
    {
        /// <summary>
        /// Indicates if this is a WebSocket upgrade request.
        /// </summary>
        bool IsWebSocketRequest { get; }

        /// <summary>
        /// Attempts to upgrade the request to a <see cref="IWebSocketConnection"/>. Check <see cref="IsWebSocketRequest"/>
        /// before invoking this.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        // REVIEW: Should this even be async?
        ValueTask<IWebSocketConnection> AcceptWebSocketAsync(WebSocketAcceptContext context);
    }
}
