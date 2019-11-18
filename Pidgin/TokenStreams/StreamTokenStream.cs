using System.IO;
using System.Threading.Tasks;

namespace Pidgin.TokenStreams
{
    internal class StreamTokenStream : ITokenStream<byte>
    {
        public int ChunkSizeHint => 4096;

        private readonly Stream _input;

        public StreamTokenStream(Stream input)
        {
            _input = input;
        }

        public ValueTask<int> ReadInto(byte[] buffer, int startIndex, int length)
            => new ValueTask<int>(_input.Read(buffer, startIndex, length));

        public void Dispose() { }
    }
}