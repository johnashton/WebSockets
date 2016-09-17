using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.WebUtilities.Internal
{
    [DebuggerDisplay("{DebuggerDisplayContent,nq} (Owned={Owned})")]
    public class BufferSegment
    {
        public ArraySegment<byte> Buffer;
        public bool Owned;

        public BufferSegment Next;

        public int End => Buffer.Offset + Buffer.Count;

        public byte this[int index]
        {
            get
            {
                if (index >= Buffer.Count)
                {
                    throw new IndexOutOfRangeException();
                }
                return Buffer.Array[Buffer.Offset + index];
            }
        }

        private string DebuggerDisplayContent
        {
            get
            {
                var data = new byte[Buffer.Count];
                Array.Copy(Buffer.Array, Buffer.Offset, data, 0, Buffer.Count);
                return "{" + string.Join(",", data.Select(b => "0x" + b.ToString("X"))) + "} \"" + Encoding.UTF8.GetString(data) + "\"";
            }
        }

        /// <summary>
        /// Slice off the front of this segment, maintaining everything after 'offset', AND the remainder of the buffer chain
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal BufferSegment Slice(int offset)
        {
            return new BufferSegment()
            {
                Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + offset, Buffer.Count - offset),
                Owned = Owned,
                Next = Next
            };
        }

        /// <summary>
        /// Slice out a piece of this segment, REMOVING the link to the next buffer.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        internal BufferSegment Slice(int offset, int length)
        {
            return new BufferSegment()
            {
                Buffer = new ArraySegment<byte>(Buffer.Array, Buffer.Offset + offset, length),
                Owned = Owned,
                Next = null
            };
        }

        internal BufferSegment Clone()
        {
            return new BufferSegment()
            {
                Buffer = Buffer,
                Owned = Owned,
                Next = Next
            };
        }
    }
}
