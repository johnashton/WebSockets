using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.WebUtilities.Internal
{
    [DebuggerTypeProxy(typeof(ByteBufferDisplayProxy))]
    public struct ByteBuffer : IEnumerable<ArraySegment<byte>>
    {
        private readonly BufferSegment _head;
        private readonly BufferSegment _tail;
        private readonly ArraySegment<byte> _data;

        public bool IsEmpty => (_head == null && _data.Count == 0) || (_head == _tail && (_head?.Buffer.Count == 0));

        private bool IsSingleBuffer => _head == null || _head == _tail;

        public int Length
        {
            get
            {
                // TODO: Cache
                int length = 0;
                var segment = _head;

                if (segment == null)
                {
                    return _data.Count;
                }

                do
                {
                    length += segment.Buffer.Count;
                    segment = segment.Next;
                } while (segment != null);

                return length;
            }
        }

        public ByteBuffer(ArraySegment<byte> data)
        {
            _data = data;
            _head = null;
            _tail = null;
        }

        public ByteBuffer(BufferSegment head, BufferSegment tail)
        {
            _data = default(ArraySegment<byte>);
            _head = head;
            _tail = tail;
        }

        public int IndexOf(byte data)
        {
            return IndexOf(data, 0);
        }

        // TODO: Support start
        public int IndexOf(byte value, int start)
        {
            var segment = _head;

            if (segment == null)
            {
                // Just return the data directly
                return Array.IndexOf(_data.Array, value, _data.Offset, _data.Count);
            }

            int count = 0;

            while (true)
            {
                int index = Array.IndexOf(segment.Buffer.Array, value, segment.Buffer.Offset, segment.Buffer.Count);

                if (index == -1)
                {
                    count += segment.Buffer.Count;
                }
                else
                {
                    count += index;
                    return count;
                }

                if (segment == _tail)
                {
                    break;
                }

                segment = segment.Next;
            }

            return -1;
        }

        public ByteBuffer Slice(int offset, int length)
        {
            var segment = _head;
            if (segment == null)
            {
                return new ByteBuffer(new ArraySegment<byte>(_data.Array, _data.Offset + offset, length));
            }
            else
            {
                // Find the new first segment (start[offset] is the first character of the sliced buffer) and slice it.
                var start = FindSegment(segment, ref offset);
                start = start.Slice(offset);

                // Walk from here forward to find the end point
                BufferSegment prev = null;
                var current = start;
                while (current != null)
                {
                    // Check if this is the end
                    if(current.Buffer.Count < length)
                    {
                        length -= current.Buffer.Count;
                        prev = current;
                        current.Next = current.Next?.Clone(); // We need to clone so that we get a parallel buffer chain and don't accidentally break the existing chain.
                        current = current.Next;
                    }
                    else
                    {
                        // This is the end!
                        current = current.Slice(0, length);
                        if(prev == null)
                        {
                            // We stayed in the "starting" segment, so just replace the start segment with our newly sliced segment
                            start = current;
                        }
                        else
                        {
                            // Wire the sliced end segment up to the end of the chain
                            prev.Next = current;
                        }
                        break;
                    }
                }

                return new ByteBuffer(start, current);
            }
        }

        /// <summary>
        /// Slices off the first 'offset' bytes, meaning what was previously at index `offset` is now at offset 0
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public ByteBuffer Slice(int offset) => Slice(offset, Length - offset);

        public ArraySegment<byte> GetArraySegment()
        {
            if (_head == null)
            {
                return _data;
            }

            List<ArraySegment<byte>> buffers = null;
            var length = 0;

            foreach (var span in this)
            {
                if (IsSingleBuffer)
                {
                    return span;
                }
                else
                {
                    if (buffers == null)
                    {
                        buffers = new List<ArraySegment<byte>>();
                    }
                    buffers.Add(span);
                    length += span.Count;
                }
            }

            var data = new byte[length];
            int offset = 0;
            foreach (var span in buffers)
            {
                Buffer.BlockCopy(span.Array, span.Offset, data, offset, span.Count);
                offset += span.Count;
            }

            return new ArraySegment<byte>(data, 0, length);
        }

        private BufferSegment FindSegment(BufferSegment segment, ref int offset)
        {
            while (segment != null)
            {
                if (segment.End <= offset)
                {
                    // Move to the next segment
                    offset -= segment.Buffer.Count;
                    segment = segment.Next;
                }
                else
                {
                    // We found it!
                    break;
                }
            }
            return segment;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_head, _tail, _data);
        }

        IEnumerator<ArraySegment<byte>> IEnumerable<ArraySegment<byte>>.GetEnumerator()
        {
            return new Enumerator(_head, _tail, _data);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(_head, _tail, _data);
        }

        public struct Enumerator : IEnumerator<ArraySegment<byte>>
        {
            private BufferSegment _head;
            private readonly BufferSegment _tail;
            private ArraySegment<byte> _current;
            private ArraySegment<byte> _data;
            private int _offset;

            public Enumerator(BufferSegment head, BufferSegment tail, ArraySegment<byte> data)
            {
                _head = head;
                _tail = tail;
                _current = default(ArraySegment<byte>);
                _data = data;
                _offset = head?.Buffer.Offset ?? data.Offset;
            }

            public ArraySegment<byte> Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_head == null)
                {
                    if (_data.Array != null)
                    {
                        _current = _data;
                        _data = default(ArraySegment<byte>);
                        return true;
                    }

                    return false;
                }

                if (_head == _tail && _offset == (_tail.Buffer.Offset + _tail.Buffer.Count))
                {
                    return false;
                }

                _current = _head.Buffer;

                if (_head != _tail)
                {
                    _head = _head.Next;
                }
                else
                {
                    _offset = _tail.Buffer.Offset + _tail.Buffer.Count;
                }

                return true;
            }

            public void Reset()
            {

            }
        }

        internal class ByteBufferDisplayProxy
        {
            private readonly ByteBuffer _buffer;

            public ByteBufferDisplayProxy(ByteBuffer buffer)
            {
                _buffer = buffer;
            }

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public string[] Buffers
            {
                get
                {
                    return _buffer.Select(a => "{" + string.Join(",", a) + "}").ToArray();
                }
            }
        }
    }
}
