using Pidgin.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Pidgin.Parser<char>;

namespace Pidgin
{
    public static partial class Parser
    {
        /// <summary>
        /// A parser that parses and returns a single whitespace character
        /// </summary>
        /// <returns>A parser that parses and returns a single whitespace character</returns>
        public static Parser<char, char> Whitespace { get; }
            = Token(char.IsWhiteSpace).Labelled("whitespace");
        /// <summary>
        /// A parser that parses and returns a sequence of whitespace characters
        /// </summary>
        /// <returns>A parser that parses and returns a sequence of whitespace characters</returns>
        public static Parser<char, IEnumerable<char>> Whitespaces { get; }
            = Whitespace.Many().Labelled("whitespace");
        /// <summary>
        /// A parser that parses and returns a sequence of whitespace characters packed into a string
        /// </summary>
        /// <returns>A parser that parses and returns a sequence of whitespace characters packed into a string</returns>
        public static Parser<char, string> WhitespaceString { get; }
            = Whitespace.ManyString().Labelled("whitespace");
        /// <summary>
        /// A parser that discards a sequence of whitespace characters
        /// </summary>
        /// <returns>A parser that discards a sequence of whitespace characters</returns>
        public static Parser<char, Unit> SkipWhitespaces { get; }
            = new SkipWhitespacesParser();
    }

    internal class SkipWhitespacesParser : Parser<char, Unit>
    {
        internal override async ValueTask<InternalResult<Unit>> Parse(ParseState<char> state)
        {
            var startingLoc = state.Location;
            var chunk = await state.LookAhead(32);
            while (chunk.Length > 0)
            {
                for (var i = 0; i < chunk.Length; i++)
                {
                    if (!char.IsWhiteSpace(chunk.ValueAt(i)))
                    {
                        await state.Advance(i);
                        return InternalResult.Success(Unit.Value, state.Location > startingLoc);
                    }
                }
                await state.Advance(chunk.Length);
                chunk = await state.LookAhead(32);
            }
            return InternalResult.Success(Unit.Value, state.Location > startingLoc);
        }
    }
}