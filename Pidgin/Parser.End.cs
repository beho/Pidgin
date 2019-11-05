using System.Threading.Tasks;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser which parses the end of the input stream
        /// </summary>
        /// <returns>A parser which parses the end of the input stream and returns <see cref="Unit.Value"/></returns>
        public static Parser<TToken, Unit> End { get; } = new EndParser();

        private sealed class EndParser : Parser<TToken, Unit>
        {
            internal sealed override ValueTask<InternalResult<Unit>> Parse(ParseState<TToken> state)
            {
                if (state.HasCurrent)
                {
                    state.Error = new InternalError<TToken>(
                        Maybe.Just(state.Current),
                        false,
                        state.Location,
                        null
                    );
                    state.AddExpected(new Expected<TToken>());
                    return new ValueTask<InternalResult<Unit>>(InternalResult.Failure<Unit>(false));
                }
                return new ValueTask<InternalResult<Unit>>(InternalResult.Success(Unit.Value, false));
            }
        }
    }
}