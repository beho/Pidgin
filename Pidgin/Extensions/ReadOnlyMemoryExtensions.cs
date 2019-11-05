using System;
using System.Runtime.CompilerServices;

namespace Pidgin.Extensions
{
    internal static class ReadOnlyMemoryExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T ValueAt<T>(this ReadOnlyMemory<T> memory, int pos)
            => memory.Span[pos];
    }
}
