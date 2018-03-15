using System.IO;

namespace StreamSlicer
{
    public class StreamSlicer
    {
        private const int DefaultBufferSize = 1024 * 16;

        private readonly byte[] _buffer;
        private readonly Stream _stream;
        private readonly ulong _marker;
        private readonly ulong _markerMask;
        private readonly int _bufferSize;
        private readonly long _length;

        private int _offset;
        private ArraySlice<byte> _slice;
        private int _prevMarkerPos;

        public StreamSlicer(Stream stream, ulong marker, int bufferSize = DefaultBufferSize)
        {
            _stream = stream;
            _marker = marker;
            _markerMask = GetMaskFor(marker);
            _bufferSize = bufferSize;
            _buffer = new byte[bufferSize];
            _length = stream.Length;
            _prevMarkerPos = 0;
        }

        private ulong GetMaskFor(ulong marker)
        {
            ulong mask = 0;
            for (int i = 0; i < 8; i++)
            {
                var test = ((ulong)0xFF << (i * 8));
                if ((marker & test) == 0)
                {
                    return mask;
                }

                mask = mask | test;
            }

            return 0;
        }

        public bool Next(ref ArraySlice<byte> outSlice)
        {
            bool result = false;
            var bytesToRead = _bufferSize - _offset;
            var bytesRead = _stream.Read(_buffer, _offset, bytesToRead);
            _slice.Reset(bytesRead + _offset);

            var nextMarkerPos = MoveToNextMarker(ref _slice, _prevMarkerPos);

            if (nextMarkerPos > 0)
            {
                outSlice.Crop(_prevMarkerPos, nextMarkerPos);
                result = true;
            }
            else if (nextMarkerPos == 0)
            {
                var endOfStream = _length == _stream.Position;
                if (endOfStream)
                {
                    nextMarkerPos = bytesRead + _offset;
                    outSlice.Crop(_prevMarkerPos, nextMarkerPos);
                    result = true;
                }
            }

            _prevMarkerPos = nextMarkerPos;
            _offset = ShiftTail(_buffer, _prevMarkerPos);

            return result;
        }

        private int MoveToNextMarker(ref ArraySlice<byte> slice, int prevPos)
        {
            ulong match = 0;
            int position = 0;

            for (int i = 0; i < slice.Length; i++)
            {
                match = match << 8 | slice[i];
                position++;
                if ((match & _markerMask) != _marker)
                {
                    continue;
                }

                var result = position + prevPos;

                if (result == prevPos)
                {
                    continue;
                }

                slice.TrimFront(position);

                return result;
            }

            return 0;
        }

        private int ShiftTail(byte[] buffer, int offset)
        {
            for (int i = 0; i < buffer.Length - offset; i++)
            {
                buffer[i] = buffer[i + offset];
            }

            return buffer.Length - offset;
        }
    }
}
