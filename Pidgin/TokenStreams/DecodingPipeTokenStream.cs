using System;
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

        private readonly PipeReader _reader;
        private readonly Decoder _decoder;

        private bool _isCompleted = false;
        private bool _isAdvanceRequired = false;

        private ReadOnlySequence<byte> _currentSequence;
        private SequencePosition _examinedPosition;

        private ReadOnlyMemory<byte> _currentSegment;
        private SequencePosition _currentSegmentStart;


        public DecodingPipeTokenStream(PipeReader reader, Decoder decoder)
        {
            _reader = reader;
            _decoder = decoder;
        }


        public async ValueTask<int> ReadInto(char[] buffer, int startIndex, int length)
        {
            // initial read
            if (!_isCompleted && _currentSequence.IsEmpty)
            {
                await Read(resetExamined: true);
            }

            // try to obtain next segment if current one is empty
            if (_currentSegment.IsEmpty && !TryGetNextSequenceSegment())
            {
                if (_isAdvanceRequired)
                {
                    Advance();
                }

                // 1/ last read signaled IsCompleted
                // 2/ cannot obtain another segment by virtue of being here
                if (_isCompleted)
                {
                    return 0;
                }

                await Read();
            }

            int charsDecoded = 0;
            var chars = new Memory<char>(buffer, startIndex, length);
            do
            {
                ReadOnlyMemory<byte> bytes = _currentSegment.Slice(0, Math.Min(_currentSegment.Length, chars.Length));

                _decoder.Convert(bytes.Span, chars.Span, false, out int bytesUsed, out int segmentCharsDecoded, out bool _);
                charsDecoded += segmentCharsDecoded;
                chars = chars.Slice(segmentCharsDecoded);

                _currentSegment = _currentSegment.Slice(bytesUsed);
                _examinedPosition = _currentSequence.GetPosition(bytesUsed, _examinedPosition);
            } while (!chars.IsEmpty && (!_currentSegment.IsEmpty || TryGetNextSequenceSegment()));

            // we could not decode any character even though spanLength > 0
            // so read again
            if (charsDecoded == 0)
            {
                return await ReadInto(buffer, startIndex, length);
            }

            return charsDecoded;
        }

        private async ValueTask Read(bool resetExamined = false)
        {
            ReadResult result = await _reader.ReadAsync();

            _currentSequence = result.Buffer;
            _isCompleted = result.IsCompleted;

            if (resetExamined)
            {
                _examinedPosition = _currentSequence.Start;
            }

            // we are storing _examinedPosition between Read calls
            // this presupposes that no one else is reading from the pipe
            // otherwise segment in _examinedPosition might reference already consumed one
            _currentSegmentStart = _examinedPosition;
            TryGetNextSequenceSegment();

            _isAdvanceRequired = true;
        }

        private void Advance()
        {
            _reader.AdvanceTo(_currentSequence.Start, _currentSequence.End);
            _isAdvanceRequired = false;
        }

        private bool TryGetNextSequenceSegment()
            => _currentSequence.TryGet(ref _currentSegmentStart, out _currentSegment, advance: true);

        public void Dispose()
        {
            _currentSequence = default;
            _examinedPosition = default;
            _currentSegment = default;
            _currentSegmentStart = default;

            _decoder.Reset();
        }
    }
#endif
}
