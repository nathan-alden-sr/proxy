using System;
using System.Collections.Generic;
using System.Text;
using NathanAlden.Proxy.Http;

namespace NathanAlden.Proxy.HttpClient
{
    public class ArraySegmentsToHttpHeaderLines
    {
        private byte[] _buffer = new byte[0];
        private int _carriageReturnIndex = -1;
        private int _findIndex;
        private bool _requestLineRead;

        public IEnumerable<(HttpHeaderLineType lineType, string line)> Add(ArraySegment<byte> arraySegment)
        {
            if (arraySegment.Count == 0)
            {
                yield break;
            }

            int copyIndex = _buffer.Length;

            Array.Resize(ref _buffer, _buffer.Length + arraySegment.Count);
            Array.Copy(arraySegment.Array, 0, _buffer, copyIndex, arraySegment.Count);

            var arrayShiftCount = 0;

            if (_carriageReturnIndex == -1)
            {
                FindNextCarriageReturn();
            }

            do
            {
                if (_carriageReturnIndex == -1 || _carriageReturnIndex == _buffer.Length - 1)
                {
                    break;
                }
                if (_buffer[_carriageReturnIndex + 1] != HttpConstants.LineFeed)
                {
                    continue;
                }

                string line = Encoding.ASCII.GetString(_buffer, arrayShiftCount, _carriageReturnIndex - arrayShiftCount);

                arrayShiftCount = _findIndex = _carriageReturnIndex + 2;

                if (line != "")
                {
                    if (!_requestLineRead)
                    {
                        _requestLineRead = true;
                        yield return (HttpHeaderLineType.RequestLineOrResponseStatusLine, line);
                    }
                    else
                    {
                        yield return (HttpHeaderLineType.Header, line);
                    }

                    FindNextCarriageReturn();
                }
                else
                {
                    yield return (HttpHeaderLineType.NewLine, line);

                    _carriageReturnIndex = -1;
                    _findIndex = -1;
                }
            } while (_carriageReturnIndex > -1);

            if (arrayShiftCount == 0)
            {
                yield break;
            }

            int newSize = _buffer.Length - arrayShiftCount;

            Array.Copy(_buffer, arrayShiftCount, _buffer, 0, newSize);
            Array.Resize(ref _buffer, newSize);

            _carriageReturnIndex = -1;
            _findIndex = 0;
        }

        public byte[] GetBuffer()
        {
            return (byte[])_buffer.Clone();
        }

        private void FindNextCarriageReturn()
        {
            _carriageReturnIndex = Array.IndexOf(_buffer, HttpConstants.CarriageReturn, _findIndex);
            _findIndex = _carriageReturnIndex + 1;
        }
    }
}