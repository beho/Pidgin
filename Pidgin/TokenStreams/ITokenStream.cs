using System;
using System.Threading.Tasks;

namespace Pidgin.TokenStreams
{
    internal interface ITokenStream<TToken> : IDisposable
    {
        int ChunkSizeHint { get; }

        ValueTask<int> ReadInto(TToken[] buffer, int startIndex, int length);
    }
}
