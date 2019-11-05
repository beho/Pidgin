using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Pidgin.TokenStreams;

namespace Pidgin
{
    /// <summary>
    /// Extension methods for running parsers
    /// </summary>
    public static class ParserExtensions
    {
        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input string</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<char, T>> Parse<T>(this Parser<char, T> parser, string input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => Parse(parser, input.AsMemory(), calculatePos ?? Parser.DefaultCharPosCalculator);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<TToken, T>> Parse<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new ListTokenStream<TToken>(input), calculatePos ?? Parser.GetDefaultPosCalculator<TToken>());

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<TToken, T>> ParseReadOnlyList<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new ReadOnlyListTokenStream<TToken>(input), calculatePos ?? Parser.GetDefaultPosCalculator<TToken>());

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerable</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<TToken, T>> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
        {
            using (var e = input.GetEnumerator())
            {
                return Parse(parser, e, calculatePos);
            }
        }

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerator</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<TToken, T>> Parse<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new EnumeratorTokenStream<TToken>(input), calculatePos ?? Parser.GetDefaultPosCalculator<TToken>());

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>.
        /// Note that more characters may be consumed from <paramref name="input"/> than were required for parsing.
        /// You may need to manually rewind <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input stream</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<byte, T>> Parse<T>(this Parser<byte, T> parser, Stream input, Func<byte, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new StreamTokenStream(input), calculatePos ?? Parser.DefaultBytePosCalculator);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input reader</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<char, T>> Parse<T>(this Parser<char, T> parser, TextReader input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => DoParse(parser, new ReaderTokenStream(input), calculatePos ?? Parser.DefaultCharPosCalculator);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input array</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<TToken, T>> Parse<TToken, T>(this Parser<TToken, T> parser, TToken[] input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => parser.Parse(input.AsMemory(), calculatePos);

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input span</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <returns>The result of parsing</returns>
        public static ValueTask<Result<TToken, T>> Parse<TToken, T>(this Parser<TToken, T> parser, ReadOnlyMemory<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
        {
            var state = new ParseState<TToken>(calculatePos ?? Parser.GetDefaultPosCalculator<TToken>(), input);
            var result = DoParse(parser, state);

            // TODO is it necessary with ReadOnlyMemory
            //KeepAlive(ref input); 
            return result;
        }
        //[MethodImpl(MethodImplOptions.NoInlining)]
        //private static void KeepAlive<TToken>(ref ReadOnlySpan<TToken> span) { }

        private static async ValueTask<Result<TToken, T>> DoParse<TToken, T>(Parser<TToken, T> parser, ITokenStream<TToken> stream, Func<TToken, SourcePos, SourcePos> calculatePos)
        {
            var state = new ParseState<TToken>(calculatePos, stream);
            await state.Initialize();
            return await DoParse(parser, state);
        }
        private static async ValueTask<Result<TToken, T>> DoParse<TToken, T>(Parser<TToken, T> parser, ParseState<TToken> state)
        {
            var internalResult = await parser.Parse(state);

            var result = internalResult.Success
                ? new Result<TToken, T>(internalResult.ConsumedInput, internalResult.Value)
                : new Result<TToken, T>(internalResult.ConsumedInput, state.BuildError());

            state.Dispose();  // ensure we return the state's buffers to the buffer pool

            return result;
        }


        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input string</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<T>(this Parser<char, T> parser, string input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input list</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseReadOnlyListOrThrow<TToken, T>(this Parser<TToken, T> parser, IReadOnlyList<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.ParseReadOnlyList(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerable</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerable<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input enumerator</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, IEnumerator<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input stream</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<T>(this Parser<byte, T> parser, Stream input, Func<byte, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input reader</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<T>(this Parser<char, T> parser, TextReader input, Func<char, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input array</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, TToken[] input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        /// <summary>
        /// Applies <paramref name="parser"/> to <paramref name="input"/>
        /// </summary>
        /// <param name="parser">A parser</param>
        /// <param name="input">An input span</param>
        /// <param name="calculatePos">A function to calculate the new position after consuming a token, or null to use the default</param>
        /// <exception cref="ParseException">Thrown when an error occurs during parsing</exception>
        /// <returns>The result of parsing</returns>
        public static async ValueTask<T> ParseOrThrow<TToken, T>(this Parser<TToken, T> parser, ReadOnlyMemory<TToken> input, Func<TToken, SourcePos, SourcePos>? calculatePos = null)
            => GetValueOrThrow(await parser.Parse(input, calculatePos));

        private static T GetValueOrThrow<TToken, T>(Result<TToken, T> result)
            => result.Success ? result.Value : throw new ParseException(result.Error!.RenderErrorMessage());
    }
}