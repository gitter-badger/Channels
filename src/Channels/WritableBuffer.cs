// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Channels
{
    public struct WritableBuffer
    {
        private MemoryPool _pool;

        private LinkedSegment _segment;
        private int _index;

        private LinkedSegment _head;
        private int _headIndex;

        internal WritableBuffer(MemoryPool pool, LinkedSegment segment)
        {
            _pool = pool;

            _segment = segment;
            _index = segment?.Start ?? 0;

            _head = segment;
            _headIndex = _index;
        }

        internal WritableBuffer(MemoryPool pool, LinkedSegment segment, int index)
        {
            _pool = pool;

            _segment = segment;
            _index = index;

            _head = segment;
            _headIndex = _index;
        }

        internal LinkedSegment Head => _head;

        internal int HeadIndex => _headIndex;

        internal LinkedSegment Segment => _segment;

        internal int Index => _index;

        internal bool IsDefault => _segment == null;

        public BufferSpan Memory => new BufferSpan(_segment.Block.DataArrayPtr, _segment.Block.Array, Segment.End, _segment.Block.Data.Offset + _segment.Block.Data.Count - Segment.End);

        public void Write(byte[] data, int offset, int count)
        {
            if (_segment == null)
            {
                _segment = new LinkedSegment(_pool.Lease());
                _index = _segment.End;
                _head = _segment;
                _headIndex = _segment.Start;
            }

            Debug.Assert(_segment.Block != null);
            Debug.Assert(_segment.Next == null);
            Debug.Assert(_segment.End == _index);

            var segment = _segment;
            var block = _segment.Block;
            var blockIndex = _index;
            var bufferIndex = offset;
            var remaining = count;
            var bytesLeftInBlock = block.Data.Offset + block.Data.Count - blockIndex;

            while (remaining > 0)
            {
                // Try the block empty if the segment is reaodnly
                if (bytesLeftInBlock == 0 || segment.ReadOnly)
                {
                    var nextBlock = _pool.Lease();
                    var nextSegment = new LinkedSegment(nextBlock);
                    segment.End = blockIndex;
                    Volatile.Write(ref segment.Next, nextSegment);
                    segment = nextSegment;
                    block = nextBlock;

                    blockIndex = block.Data.Offset;
                    bytesLeftInBlock = block.Data.Count;
                }

                var bytesToCopy = remaining < bytesLeftInBlock ? remaining : bytesLeftInBlock;

                Buffer.BlockCopy(data, bufferIndex, block.Array, blockIndex, bytesToCopy);

                blockIndex += bytesToCopy;
                bufferIndex += bytesToCopy;
                remaining -= bytesToCopy;
                bytesLeftInBlock -= bytesToCopy;
            }

            segment.End = blockIndex;
            segment.Block = block;
            _segment = segment;
            _index = blockIndex;
        }

        public void Append(ReadableBuffer begin, ReadableBuffer end)
        {
            var clonedBegin = LinkedSegment.Clone(begin, end);
            var clonedEnd = clonedBegin;
            while (clonedEnd.Next != null)
            {
                clonedEnd = clonedEnd.Next;
            }

            if (_segment == null)
            {
                _head = clonedBegin;
                _headIndex = clonedBegin.Start;
            }
            else
            {
                Debug.Assert(_segment.Block != null);
                Debug.Assert(_segment.Next == null);
                Debug.Assert(_segment.End == _index);

                _segment.Next = clonedBegin;
            }

            _segment = clonedEnd;
            _index = clonedEnd.End;
        }

        public void UpdateWritten(int bytesWritten)
        {
            Debug.Assert(_segment != null);
            Debug.Assert(!_segment.ReadOnly);
            Debug.Assert(_segment.Block != null);
            Debug.Assert(_segment.Next == null);
            Debug.Assert(_segment.End == _index);

            var block = _segment.Block;
            var blockIndex = _index + bytesWritten;

            Debug.Assert(blockIndex <= block.Data.Offset + block.Data.Count);

            _segment.End = blockIndex;
            _index = blockIndex;
        }
    }
}
