using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities.Internal;

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
            var payloads = new[] {
                new byte[] { 1, 2 },
                new byte[] { 3, 4 },
                new byte[] { 5, 6 },
                new byte[] { 7, 8 },
                new byte[] { 9, 10 },
            };
            var s = new AwaitableStream();
            var writes = Task.Run(async () => {
                foreach (var payload in payloads)
                {
                    await s.WriteAsync(payload, 0, payload.Length);
                }
            });

            // Start the process
            ByteBuffer buf = default(ByteBuffer);
            foreach (var payload in payloads)
            {
                s.Consumed(0);
                buf = await s.ReadAsync();
            }

            // What have we got
            Console.WriteLine("Slice(2, 5): " + string.Join(",", buf.Slice(2, 5).GetArraySegment()));
            Console.WriteLine("Slice(3): " + string.Join(",", buf.Slice(3).GetArraySegment()));
        }
    }
}
