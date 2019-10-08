﻿using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;

namespace Pidgin.TokenStreams
{
#if NETCOREAPP3_0
    class DecodingPipeTokenStream : ITokenStream<char>
    {
        public int ChunkSizeHint => 4096;

        private PipeReader _reader;
        private bool _completed = false;
        private bool _read = false;
        private ReadOnlySequence<byte> _currentSequence;
        private ReadOnlyMemory<byte> _currentSegment;
        private SequencePosition _initialPosition;
        private SequencePosition _position;
        private long _examined;
        private int _currentSegmentExamined;

        private Decoder _decoder;

        public DecodingPipeTokenStream(PipeReader reader, Decoder decoder)
        {
            _reader = reader;
            _decoder = decoder;
        }


        public async ValueTask<int> ReadInto(char[] buffer, int startIndex, int length)
        {
            // initial read
            if (_currentSequence.IsEmpty)
            {
                await Read();
            }

            SequencePosition origin = _position;
            // if previous _reader.ReadAsync signaled completion and all data are processed
            while (_currentSegment.IsEmpty && !_currentSequence.TryGet(ref _position, out _currentSegment, advance: true))
            {
                if (_read)
                {
                    Advance(_currentSegmentExamined, origin);
                }

                if (_completed)
                {
                    return 0;
                }

                // TryGet with advance=true modifies _position to next segment but any reading is relative to _position before call
                origin = _position;
                _currentSegmentExamined = 0;

                await Read();
            }

            int spanLength = Math.Min(_currentSegment.Length, length);

            // reader is not completed but we consumed all we read 
            // and we cannot return 0 - it would mean end of token stream
            // so read again
            if (spanLength == 0)
            {
                if (_read)
                {
                    Advance(_examined, _initialPosition);
                }

                return await ReadInto(buffer, startIndex, length);
            }

            int bytesUsed, charsUsed;
            Decode(buffer, spanLength, out bytesUsed, out charsUsed);

            _currentSegment = _currentSegment.Slice(bytesUsed);
            _examined += spanLength;
            _currentSegmentExamined += spanLength;

            // we could not decode any character even though spanLength > 0
            // so read again
            if (charsUsed == 0)
            {
                return await ReadInto(buffer, startIndex, length);
            }

            // TODO ensure that reader is ready to be read before return?
            //if (_read)
            //{
            //    Advance(_currentSegmentExamined, origin);
            //}

            return charsUsed;
        }

        private void Decode(char[] buffer, int spanLength, out int bytesUsed, out int charsUsed)
        {
            ReadOnlySpan<byte> source = _currentSegment.Span.Slice(0, spanLength);
            _decoder.Convert(source, buffer, false, out bytesUsed, out charsUsed, out bool _);
        }

        private async ValueTask Read()
        {
            ReadResult result = await _reader.ReadAsync();

            _currentSequence = result.Buffer;
            _initialPosition = _currentSequence.Start;

            // jump to already examined position
            _position = _currentSequence.GetPosition(_examined);
            _completed = result.IsCompleted;

            _read = true;
        }

        private void Advance(long offset, SequencePosition origin)
        {
            SequencePosition examinedPosition = origin.Equals(default)
                ? _currentSequence.GetPosition(_examined, _initialPosition)
                : _currentSequence.GetPosition(offset, origin);

            _reader.AdvanceTo(_initialPosition, examinedPosition);
            _read = false;
        }

        public void Dispose()
        {
            _reader = null;
            _currentSequence = default;
            _currentSegment = default;
            _position = default;

            _decoder.Reset();
            _decoder = null;
        }
    }
#endif
}
