using System.Threading.Tasks;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser which returns the specified value without consuming any input
        /// </summary>
        /// <param name="value">The value to return</param>
        /// <typeparam name="T">The type of the value to return</typeparam>
        /// <returns>A parser which returns the specified value without consuming any input</returns>
        public static Parser<TToken, T> Return<T>(T value)
            => new ReturnParser<TToken, T>(value);
    }

    internal sealed class ReturnParser<TToken, T> : Parser<TToken, T>
    {
        private readonly T _value;

        public ReturnParser(T value)
        {
            _value = value;
        }

        internal sealed override ValueTask<InternalResult<T>> Parse(ParseState<TToken> state)
            => new ValueTask<InternalResult<T>>(InternalResult.Success<T>(_value, false));
    }
}
