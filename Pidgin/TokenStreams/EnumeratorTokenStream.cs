using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pidgin.TokenStreams
{
    internal class EnumeratorTokenStream<TToken> : ITokenStream<TToken>
    {
        public int ChunkSizeHint => 16;
        private readonly IEnumerator<TToken> _input;

        public EnumeratorTokenStream(IEnumerator<TToken> input)
        {
            _input = input;
        }


        public ValueTask<int> ReadInto(TToken[] buffer, int startIndex, int length)
        {
            for (var i = 0; i < length; i++)
            {
                var hasNext = _input.MoveNext();
                if (!hasNext)
                {
                    return new ValueTask<int>(i);
                }
                buffer[startIndex + i] = _input.Current;
            }
            return new ValueTask<int>(length);
        }

        public void Dispose() { }
    }
}