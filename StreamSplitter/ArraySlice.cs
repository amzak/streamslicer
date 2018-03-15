using System;

namespace StreamSlicer
{
    public struct ArraySlice<T>
    {
        private readonly T[] _array;
        private int _from;
        private int _to;

        public ArraySlice(T[] array)
        {
            _from = 0;
            _to = array.Length - 1;
            _array = array;
        }

        public void TrimFront(int count)
        {
            _from += count;
        }

        public void Reset(int length = 0)
        {
            _from = 0;
            _to = length > 0 ? length : _array.Length - 1;
        }

        public void Crop(int from, int to)
        {
            _from = from;
            _to = to;
        }

        public ArraySegment<T> ToSegment(int count, int offset = 0)
        {
            return new ArraySegment<T>(_array, _from + offset, count);
        }

        public int Length => _to - _from;

        public T this[int index] => _array[index + _from];

    }
}