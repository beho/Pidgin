using System;
using System.IO;
using System.Threading.Tasks;
using Pidgin.TokenStreams;
using Xunit;

namespace Pidgin.Tests
{
    public class ParseStateTests
    {
        [Fact]
        public async Task TestEmptyInput()
        {
            var input = "";
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));
            await state.Initialize();

            Assert.Equal(new SourcePos(1, 1), state.ComputeSourcePos());
            Assert.False(state.HasCurrent);
        }

        [Fact]
        public async Task TestAdvance()
        {
            var input = "foo";
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));
            await state.Initialize();

            await Consume('f', state);
            await Consume('o', state);
            await Consume('o', state);

            Assert.False(state.HasCurrent);
        }

        [Fact]
        public async Task TestDiscardChunk()
        {
            var input = ('f' + new string('o', ChunkSize));  // Length == ChunkSize + 1
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));
            await state.Initialize();

            await Consume('f', state);
            await Consume(new string('o', ChunkSize), state);
            Assert.False(state.HasCurrent);
            Assert.Equal(new SourcePos(1, input.Length + 1 /* because Col is 1-indexed */), state.ComputeSourcePos());
        }

        [Fact]
        public async Task TestSaveWholeChunkAligned()
        {
            // grows buffer on final iteration of loop
            //
            // |----|----|
            // foooo
            // ^----
            await AlignedChunkTest(ChunkSize);
        }
        [Fact]
        public async Task TestSaveWholeChunkUnaligned()
        {
            // grows buffer on final iteration of loop
            //
            // |----|----|
            // faoooo
            //  ^----
            await UnalignedChunkTest(ChunkSize);
        }
        [Fact]
        public async Task TestSaveMoreThanWholeChunkAligned()
        {
            // grows buffer on penultimate iteration of loop
            //
            // |----|----|
            // fooooo
            // ^-----
            await AlignedChunkTest(ChunkSize + 1);
        }
        [Fact]
        public async Task TestSaveMoreThanWholeChunkUnaligned()
        {
            // grows buffer on penultimate iteration of loop
            //
            // |----|----|
            // faoooo
            //  ^----
            await UnalignedChunkTest(ChunkSize + 1);
        }
        [Fact]
        public async Task TestSaveLessThanWholeChunkAligned()
        {
            // does not grow buffer
            //
            // |----|----|
            // fooooo
            // ^-----
            await AlignedChunkTest(ChunkSize - 1);
        }
        [Fact]
        public async Task TestSaveLessThanWholeChunkUnaligned()
        {
            // does not grow buffer
            //
            // |----|----|
            // fooooo
            // ^-----
            await UnalignedChunkTest(ChunkSize - 1);
        }

        private static async Task AlignedChunkTest(int inputLength)
        {
            var input = ('f' + new string('o', inputLength - 1));
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));
            await state.Initialize();

            state.PushBookmark();

            await Consume('f', state);
            await Consume(new string('o', inputLength - 1), state);
            Assert.False(state.HasCurrent);
            Assert.Equal(new SourcePos(1, inputLength + 1), state.ComputeSourcePos());

            state.Rewind();
            Assert.Equal(new SourcePos(1, 1), state.ComputeSourcePos());
            await Consume('f', state);
        }

        private static async Task UnalignedChunkTest(int inputLength)
        {
            var input = ("fa" + new string('o', inputLength - 2));
            var state = new ParseState<char>((_, x) => x.IncrementCol(), ToStream(input));
            await state.Initialize();

            await Consume('f', state);

            state.PushBookmark();
            await Consume('a' + new string('o', inputLength - 2), state);
            Assert.False(state.HasCurrent);
            Assert.Equal(new SourcePos(1, inputLength + 1), state.ComputeSourcePos());

            state.Rewind();
            Assert.Equal(new SourcePos(1, 2), state.ComputeSourcePos());
            await Consume('a', state);
        }

        private static async Task Consume(char expected, ParseState<char> state)
        {
            var oldCol = state.ComputeSourcePos().Col;
            Assert.True(state.HasCurrent);
            Assert.Equal(expected, state.Current);
            await state.Advance();
            Assert.Equal(oldCol + 1, state.ComputeSourcePos().Col);
        }

        private static async Task Consume(string expected, ParseState<char> state)
        {
            var oldCol = state.ComputeSourcePos().Col;
            var lookAhead = await state.LookAhead(expected.Length);
            AssertEqual(expected.AsSpan(), lookAhead.Span);
            await state.Advance(expected.Length);
            Assert.Equal(oldCol + expected.Length, state.ComputeSourcePos().Col);
        }

        private static void AssertEqual(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], actual[i]);
            }
        }

        private static ITokenStream<char> ToStream(string input)
            => new ReaderTokenStream(new StringReader(input));

        private static int ChunkSize => ToStream("").ChunkSizeHint;
    }
}