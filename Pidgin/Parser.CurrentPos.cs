using System.Threading.Tasks;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// A parser which returns the current source position
        /// </summary>
        /// <returns>A parser which returns the current source position</returns>
        public static Parser<TToken, SourcePos> CurrentPos { get; }
            = new CurrentPosParser();

        private sealed class CurrentPosParser : Parser<TToken, SourcePos>
        {
            internal override ValueTask<InternalResult<SourcePos>> Parse(ParseState<TToken> state)
                => new ValueTask<InternalResult<SourcePos>>(InternalResult.Success(state.ComputeSourcePos(), false));
        }
    }
}