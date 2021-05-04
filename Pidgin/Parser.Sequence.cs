using Pidgin.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LExpression = System.Linq.Expressions.Expression;

namespace Pidgin
{
    public static partial class Parser<TToken>
    {
        /// <summary>
        /// Creates a parser that parses and returns a literal sequence of tokens
        /// </summary>
        /// <param name="tokens">A sequence of tokens</param>
        /// <returns>A parser that parses a literal sequence of tokens</returns>
        public static Parser<TToken, TToken[]> Sequence(params TToken[] tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }
            return Sequence<TToken[]>(tokens);
        }
        /// <summary>
        /// Creates a parser that parses and returns a literal sequence of tokens.
        /// The input enumerable is enumerated and copied to a list.
        /// </summary>
        /// <typeparam name="TEnumerable">The type of tokens to parse</typeparam>
        /// <param name="tokens">A sequence of tokens</param>
        /// <returns>A parser that parses a literal sequence of tokens</returns>
        public static Parser<TToken, TEnumerable> Sequence<TEnumerable>(TEnumerable tokens)
            where TEnumerable : IEnumerable<TToken>
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }
            return SequenceTokenParser<TToken, TEnumerable>.Create(tokens);
        }

        /// <summary>
        /// Creates a parser that applies a sequence of parsers and collects the results.
        /// This parser fails if any of its constituent parsers fail
        /// </summary>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers</param>
        /// <returns>A parser that applies a sequence of parsers and collects the results</returns>
        public static Parser<TToken, IEnumerable<T>> Sequence<T>(params Parser<TToken, T>[] parsers)
        {
            return Sequence(parsers.AsEnumerable());
        }

        /// <summary>
        /// Creates a parser that applies a sequence of parsers and collects the results.
        /// This parser fails if any of its constituent parsers fail
        /// </summary>
        /// <typeparam name="T">The return type of the parsers</typeparam>
        /// <param name="parsers">A sequence of parsers</param>
        /// <returns>A parser that applies a sequence of parsers and collects the results</returns>
        public static Parser<TToken, IEnumerable<T>> Sequence<T>(IEnumerable<Parser<TToken, T>> parsers)
        {
            if (parsers == null)
            {
                throw new ArgumentNullException(nameof(parsers));
            }
            var parsersArray = parsers.ToArray();
            if (parsersArray.Length == 1)
            {
                return parsersArray[0].Select(x => new[] { x }.AsEnumerable());
            }
            return new SequenceParser<TToken, T>(parsersArray);
        }
    }

    internal sealed class SequenceParser<TToken, T> : Parser<TToken, IEnumerable<T>>
    {
        private readonly Parser<TToken, T>[] _parsers;

        public SequenceParser(Parser<TToken, T>[] parsers)
        {
            _parsers = parsers;
        }

        internal sealed override async ValueTask<InternalResult<IEnumerable<T>>> Parse(ParseState<TToken> state, ExpectedCollector<TToken> expecteds)
        {
            var consumedInput = false;
            var ts = new T[_parsers.Length];

            for (var i = 0; i < _parsers.Length; i++)
            {
                var p = _parsers[i];
            
                var result = await p.Parse(state, expecteds);
                consumedInput = consumedInput || result.ConsumedInput;

                if (!result.Success)
                {
                    return InternalResult.Failure<IEnumerable<T>>(consumedInput);
                }

                ts[i] = result.Value;
            }

            return InternalResult.Success<IEnumerable<T>>(ts, consumedInput);
        }
    }

    internal static class SequenceTokenParser<TToken, TEnumerable>
        where TEnumerable : IEnumerable<TToken>
    {
        private static readonly Func<TEnumerable, Parser<TToken, TEnumerable>>? _createParser;

        public static Parser<TToken, TEnumerable> Create(TEnumerable tokens)
        {
            if (_createParser != null)
            {
                return _createParser(tokens);
            }
            return new SequenceTokenParserSlow<TToken, TEnumerable>(tokens);
        }

        static SequenceTokenParser()
        {
            var ttoken = typeof(TToken).GetTypeInfo();
            var equatable = typeof(IEquatable<TToken>).GetTypeInfo();
            if (ttoken.IsValueType && equatable.IsAssignableFrom(ttoken))
            {
                var ctor = typeof(SequenceTokenParserFast<,>)
                    .MakeGenericType(typeof(TToken), typeof(TEnumerable))
                    .GetTypeInfo()
                    .DeclaredConstructors
                    .Single();
                var param = LExpression.Parameter(typeof(TEnumerable));
                var create = LExpression.New(ctor, param);
                _createParser = LExpression.Lambda<Func<TEnumerable, Parser<TToken, TEnumerable>>>(create, param).Compile();
            }
        }
    }

    internal sealed class SequenceTokenParserFast<TToken, TEnumerable> : Parser<TToken, TEnumerable>
        where TToken : struct, IEquatable<TToken>
        where TEnumerable : IEnumerable<TToken>
    {
        private readonly TEnumerable _value;
        private readonly ImmutableArray<TToken> _valueTokens;

        public SequenceTokenParserFast(TEnumerable value)
        {
            _value = value;
            _valueTokens = value.ToImmutableArray();
        }

        internal sealed override async ValueTask<InternalResult<TEnumerable>> Parse(ParseState<TToken> state, ExpectedCollector<TToken> expecteds)
        {
            var memory = await state.LookAhead(_valueTokens.Length);  // span.Length <= _valueTokens.Length

            int errorPos = Compare(memory.Span);

            if (errorPos != -1)
            {
                // strings didn't match
                await state.Advance(errorPos);
                state.Error = new InternalError<TToken>(
                    Maybe.Just(memory.ValueAt(errorPos)),
                    false,
                    state.Location,
                    null
                );
                expecteds.Add(new Expected<TToken>(_valueTokens));
                return InternalResult.Failure<TEnumerable>(errorPos > 0);
            }

            if (memory.Length < _valueTokens.Length)
            {
                // strings matched but reached EOF
                await state.Advance(memory.Length);
                state.Error = new InternalError<TToken>(
                    Maybe.Nothing<TToken>(),
                    true,
                    state.Location,
                    null
                );
                expecteds.Add(new Expected<TToken>(_valueTokens));
                return InternalResult.Failure<TEnumerable>(memory.Length > 0);
            }

            // OK
            await state.Advance(_valueTokens.Length);
            return InternalResult.Success<TEnumerable>(_value, _valueTokens.Length > 0);
        }

        private int Compare(ReadOnlySpan<TToken> span)
        {
            var errorPos = -1;
            for (var i = 0; i < span.Length; i++)
            {
                if (!span[i].Equals(_valueTokens[i]))
                {
                    errorPos = i;
                    break;
                }
            }

            return errorPos;
        }
    }

    internal sealed class SequenceTokenParserSlow<TToken, TEnumerable> : Parser<TToken, TEnumerable>
        where TEnumerable : IEnumerable<TToken>
    {
        private readonly TEnumerable _value;
        private readonly ImmutableArray<TToken> _valueTokens;

        public SequenceTokenParserSlow(TEnumerable value)
        {
            _value = value;
            _valueTokens = value.ToImmutableArray();
        }

        internal sealed override async ValueTask<InternalResult<TEnumerable>> Parse(ParseState<TToken> state, ExpectedCollector<TToken> expecteds)
        {
            var memory = await state.LookAhead(_valueTokens.Length);  // span.Length <= _valueTokens.Length

            int errorPos = Compare(memory.Span);

            if (errorPos != -1)
            {
                // strings didn't match
                await state.Advance(errorPos);
                state.Error = new InternalError<TToken>(
                    Maybe.Just(memory.ValueAt(errorPos)),
                    false,
                    state.Location,
                    null
                );
                expecteds.Add(new Expected<TToken>(_valueTokens));
                return InternalResult.Failure<TEnumerable>(errorPos > 0);
            }

            if (memory.Length < _valueTokens.Length)
            {
                // strings matched but reached EOF
                await state.Advance(memory.Length);
                state.Error = new InternalError<TToken>(
                    Maybe.Nothing<TToken>(),
                    true,
                    state.Location,
                    null
                );
                expecteds.Add(new Expected<TToken>(_valueTokens));
                return InternalResult.Failure<TEnumerable>(memory.Length > 0);
            }

            // OK
            await state.Advance(_valueTokens.Length);
            return InternalResult.Success<TEnumerable>(_value, _valueTokens.Length > 0);
        }

        private int Compare(ReadOnlySpan<TToken> span)
        {
            var errorPos = -1;
            for (var i = 0; i < span.Length; i++)
            {
                if (!EqualityComparer<TToken>.Default.Equals(span[i], _valueTokens[i]))
                {
                    errorPos = i;
                    break;
                }
            }

            return errorPos;
        }
    }
}