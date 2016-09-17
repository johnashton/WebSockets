using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities.Internal;
using Microsoft.Extensions.WebSockets.Internal;

namespace AwaitableStreamSpelunk
{
    public class Program
    {
        public static void Main(string[] args)
        {
            AsyncMain(args).Wait();
        }
        
        private static async Task AsyncMain(string[] args)
        {
            const string Payload1 = "Hello, World";
            var bytPayload1 = Encoding.UTF8.GetBytes(Payload1);
            const string Payload2 = "Hello, again!";
            var bytPayload2 = Encoding.UTF8.GetBytes(Payload2);
            var payloads = new[] {
                new byte[] { 0x11 }, // FIN=1, Opcode=Text
                new byte[] { (byte)(bytPayload1.Length << 1) },
                bytPayload1.Take(bytPayload1.Length / 2).ToArray(),

                // Send the second half of the payload along with an entire second message.
                Enumerable.Concat(
                    bytPayload1.Skip(bytPayload1.Length / 2),
                    Enumerable.Concat(new byte[] { 0x11, (byte)(bytPayload2.Length << 1) }, bytPayload2)).ToArray()
            };
            var s = new AwaitableStream();
            var writes = Task.Run(async () => {
                foreach (var payload in payloads)
                {
                    await s.WriteAsync(payload, 0, payload.Length);
                }
            });

            var socket = new WebSocketConnection(s, "foo");
            var message = await socket.ReceiveAsync(CancellationToken.None);
            Console.WriteLine($"Message Received: {message.Opcode} (FIN={message.EndOfMessage}): {Encoding.UTF8.GetString(message.Payload.Array, message.Payload.Offset, message.Payload.Count)}");
            message = await socket.ReceiveAsync(CancellationToken.None);
            Console.WriteLine($"Message Received: {message.Opcode} (FIN={message.EndOfMessage}): {Encoding.UTF8.GetString(message.Payload.Array, message.Payload.Offset, message.Payload.Count)}");
        }
    }
}
