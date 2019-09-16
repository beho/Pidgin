using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Pidgin.Parser;

namespace Pidgin.Tests
{
    public class DecodingPipeTokenStreamTests
    {
        private const int stringLength = 10000;
        private readonly Parser<char, (string, SourcePos)> Parser = Map((r, p) => (r, p), Char('ž').ManyString(), Parser<char>.CurrentPos);

        [Theory]
        [MemberData(nameof(PipeTestArgs))]
        public async Task SyncPipeTest(int writeSize, int minimumSegmentSize)
        {
            Encoding encoding = Encoding.UTF8;

            // ž is two bytes - it will be split in half due to odd segmentSize
            string s = CreateString(stringLength, 'ž');
            ReadOnlyMemory<byte> m = encoding.GetBytes(s);

            var pipe = new Pipe(new PipeOptions(minimumSegmentSize: minimumSegmentSize));
            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            int rounds = m.Length / writeSize + (m.Length % writeSize > 0 ? 1 : 0);

            for (int i = 0; i < rounds; i++)
            {
                int sliceLength = Math.Min(m.Length, writeSize);

                await writer.WriteAsync(m.Slice(0, sliceLength));

                m = m.Slice(sliceLength);
            }

            writer.Complete();

            Result<char, (string Value, SourcePos CurrentPos)> result = Parser.Parse(reader, encoding);

            Assert.True(result.Success);
            Assert.Equal(s, result.Value.Value);
            Assert.Equal(encoding.GetByteCount(s), result.Value.CurrentPos.Col - 1);
        }

        [Theory]
        [MemberData(nameof(PipeTestArgs))]
        public async Task AsyncPipeTest(int writeSize, int minimumSegmentSize)
        {
            Encoding encoding = Encoding.UTF8;

            // ž is two bytes - it will be split in half due to odd segmentSize
            string s = CreateString(stringLength, 'ž');
            ReadOnlyMemory<byte> m = encoding.GetBytes(s);

            var pipe = new Pipe(new PipeOptions(minimumSegmentSize: minimumSegmentSize));
            PipeWriter writer = pipe.Writer;
            PipeReader reader = pipe.Reader;

            await Task.WhenAll(Write(), Read());

            async Task Write()
            {
                int rounds = m.Length / writeSize + (m.Length % writeSize > 0 ? 1 : 0);

                for (int i = 0; i < rounds; i++)
                {
                    int sliceLength = Math.Min(m.Length, writeSize);

                    await writer.WriteAsync(m.Slice(0, sliceLength));

                    m = m.Slice(sliceLength);

                    await Task.Delay(30);
                }

                writer.Complete();
            }

            async Task Read()
            {
                Result<char, (string Value, SourcePos CurrentPos)> result = Parser.Parse(reader, encoding);

                Assert.True(result.Success);
                Assert.Equal(s, result.Value.Value);
                Assert.Equal(encoding.GetByteCount(s), result.Value.CurrentPos.Col - 1);
            }
        }

        private static int[] WriteSizes = new[]
        {
            111, // very short length odd
            112, // very short length even
            3333, // multiple segments, each larger than DecodingPipeTokenStream.ChunkSizeHint, odd length
            3334, // multiple segments, each larger than DecodingPipeTokenStream.ChunkSizeHint, even length
            10000, // multiple segments, each larger than DecodingPipeTokenStream.ChunkSizeHint
            20000 // whole data at once
        };
        private static int[] MinimumSegmentSizes = new[] { 32, 128, 1024, 2048, 65536 };

        public static IEnumerable<object[]> PipeTestArgs = WriteSizes.Join(MinimumSegmentSizes, ws => true, mss => true, (ws, mss) => new object[] { ws, mss });

        private string CreateString(int length, char c) => string.Create(length, c, (chars, fillChar) =>
        {
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = fillChar;
            }
        });
    }
}
