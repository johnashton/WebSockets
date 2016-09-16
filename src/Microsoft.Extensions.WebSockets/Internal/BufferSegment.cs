using System;

namespace Microsoft.AspNetCore.WebUtilities.Internal
{
    public class BufferSegment
    {
        public ArraySegment<byte> Buffer;
        public bool Owned;

        public BufferSegment Next;

        public int End => Buffer.Offset + Buffer.Count;

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
