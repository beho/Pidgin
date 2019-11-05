using System.IO;
using System.Threading.Tasks;

namespace Pidgin.TokenStreams
{
    internal class ReaderTokenStream : ITokenStream<char>
    {
        public int ChunkSizeHint => 4096;

        private readonly TextReader _input;

        public ReaderTokenStream(TextReader input)
        {
            _input = input;
        }

        public ValueTask<int> ReadInto(char[] buffer, int startIndex, int length)
            => new ValueTask<int>(_input.Read(buffer, startIndex, length));

        public void Dispose() { }
    }
}