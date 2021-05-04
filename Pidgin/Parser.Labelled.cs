
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Pidgin
{
    public partial class Parser<TToken, T>
    {
        /// <summary>
        /// Creates a parser equivalent to the current parser, with a custom label.
        /// The label will be reported in an error message if the parser fails, instead of the default error message.
        /// <seealso cref="ParseError{TToken}.Expected"/>
        /// <seealso cref="Expected{TToken}.Label"/>
        /// </summary>
        /// <param name="label">The custom label to apply to the current parser</param>
        /// <returns>A parser equivalent to the current parser, with a custom label</returns>
        public Parser<TToken, T> Labelled(string label)
        {
            if (label == null)
            {
                throw new ArgumentNullException(nameof(label));
            }
            return WithExpected(ImmutableArray.Create(new Expected<TToken>(label)));
        }

        internal Parser<TToken, T> WithExpected(ImmutableArray<Expected<TToken>> expected)
            => new WithExpectedParser<TToken, T>(this, expected);
    }

    internal sealed class WithExpectedParser<TToken, T> : Parser<TToken, T>
    {
        private readonly Parser<TToken, T> _parser;
        private readonly ImmutableArray<Expected<TToken>> _expected;

        public WithExpectedParser(Parser<TToken, T> parser, ImmutableArray<Expected<TToken>> expected)
        {
            _parser = parser;
            _expected = expected;
        }

        internal override async ValueTask<InternalResult<T>> Parse(ParseState<TToken> state, ExpectedCollector<TToken> expecteds)
        {
            var childExpecteds = new ExpectedCollector<TToken>(true);
            var result = await _parser.Parse(state, childExpecteds);
            childExpecteds.Dispose();

            if (!result.Success)
            {
                expecteds.Add(_expected);
            }
            return result;
        }
    }
}