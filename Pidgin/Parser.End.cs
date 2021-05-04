using System.Threading.Tasks;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser which parses the end of the input stream
        /// </summary>
        /// <returns>A parser which parses the end of the input stream and returns <see cref="Unit.Value"/></returns>
        public static Parser<TToken, Unit> End { get; } = new EndParser<TToken>();
    }

    internal sealed class EndParser<TToken> : Parser<TToken, Unit>
    {
        internal sealed override ValueTask<InternalResult<Unit>> Parse(ParseState<TToken> state, ExpectedCollector<TToken> expecteds)
        {
            if (state.HasCurrent)
            {
                state.Error = new InternalError<TToken>(
                    Maybe.Just(state.Current),
                    false,
                    state.Location,
                    null
                );

                expecteds.Add(new Expected<TToken>());
                return new ValueTask<InternalResult<Unit>>(InternalResult.Failure<Unit>(false));
            }
            return new ValueTask<InternalResult<Unit>>(InternalResult.Success(Unit.Value, false));
        }
    }
}