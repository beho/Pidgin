﻿using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
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

        private StateFlags _state;

        private ReadOnlySequence<byte> _currentSequence;

        private ReadOnlyMemory<byte> _currentSegment;
        private SequencePosition _currentSegmentStart;

        private bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state.HasFlag(StateFlags.Completed);
        }

        private bool IsAdvanceRequired
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _state.HasFlag(StateFlags.AdvanceRequired);
        }


        public DecodingPipeTokenStream(PipeReader reader, Decoder decoder)
        {
            _reader = reader;
            _decoder = decoder;
        }


        public int ReadInto(char[] buffer, int startIndex, int length)
        {
            // initial read
            if (!IsCompleted && _currentSequence.IsEmpty)
            {
                Read(initialRead: true);
            }

            // try to obtain next segment if current one is empty
            if (_currentSegment.IsEmpty && !TryGetNextSequenceSegment())
            {
                if (IsAdvanceRequired)
                {
                    Advance();
                }

                // 1/ last read signaled IsCompleted
                // 2/ cannot obtain another segment by virtue of being here
                if (IsCompleted)
                {
                    return 0;
                }

                Read();
            }

            int charsDecoded = 0;
            var chars = new Span<char>(buffer, startIndex, length);
            do
            {
                ReadOnlySpan<byte> bytes = _currentSegment.Span.Slice(0, Math.Min(_currentSegment.Length, chars.Length));

                _decoder.Convert(bytes, chars, false, out int bytesUsed, out int segmentCharsDecoded, out bool _);
                charsDecoded += segmentCharsDecoded;
                chars = chars.Slice(segmentCharsDecoded);

                _currentSegment = _currentSegment.Slice(bytesUsed);
            } while (!chars.IsEmpty && (!_currentSegment.IsEmpty || TryGetNextSequenceSegment()));

            // we could not decode any character even though spanLength > 0
            // so read again
            if (charsDecoded == 0)
            {
                return ReadInto(buffer, startIndex, length);
            }

            return charsDecoded;
        }

        private void Read(bool initialRead = false)
        {
            // we are storing SequencePosition from previous Read call (unless this is initial read)
            // this presupposes that no one else is reading from the pipe
            // otherwise segment in _examinedPosition might reference already consumed one
            SequencePosition examined = _currentSequence.End;
            ValueTask<ReadResult> task = _reader.ReadAsync();

            ReadResult result = task.IsCompleted
                ? task.Result
                : task.AsTask().GetAwaiter().GetResult();

            _currentSequence = result.Buffer;
            if (result.IsCompleted)
            {
                _state |= StateFlags.Completed;
            }

            _currentSegmentStart = initialRead ? _currentSequence.Start : examined;
            TryGetNextSequenceSegment();

            _state |= StateFlags.AdvanceRequired;
        }

        private void Advance()
        {
            _reader.AdvanceTo(_currentSequence.Start, _currentSequence.End);
            _state &= ~StateFlags.AdvanceRequired;
        }

        private bool TryGetNextSequenceSegment()
            => _currentSequence.TryGet(ref _currentSegmentStart, out _currentSegment, advance: true);

        public void Dispose()
        {
            _currentSequence = default;
            _currentSegment = default;
            _currentSegmentStart = default;

            _decoder.Reset();
        }

        [Flags]
        private enum StateFlags : byte
        {
            Completed = 0x01,
            AdvanceRequired = 0x02
        }
    }
#endif
}
