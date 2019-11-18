using Xunit;
using Pidgin;
using Pidgin.Permutation;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;
using System.Linq;
using System.Threading.Tasks;

namespace Pidgin.Tests
{
    public class PermutationParserTests
    {
        [Fact]
        public async Task TestSimplePermutation()
        {
            var parser = PermutationParser
                .Create<char>()
                .Add(Char('a'))
                .Add(Char('b'))
                .Add(Char('c'))
                .Build()
                .Select(
                    tup =>
                    {
                        var (((_, a), b), c) = tup;
                        return string.Concat(a, b, c);
                    }
                );

            var results = await Task.WhenAll(new[] { "abc", "bac", "bca", "cba" }.Select(async x => await parser.ParseOrThrow(x)));


            Assert.All(results, x => Assert.Equal("abc", x));
        }

        [Fact]
        public async Task TestOptionalPermutation()
        {
            var parser = PermutationParser
                .Create<char>()
                .Add(Char('a'))
                .AddOptional(Char('b'), '_')
                .Add(Char('c'))
                .Build()
                .Select(
                    tup =>
                    {
                        var (((_, a), b), c) = tup;
                        return string.Concat(a, b, c);
                    }
                );

            var results1 = await Task.WhenAll(new[] { "abc", "bac", "bca", "cba" }.Select(async x => await parser.ParseOrThrow(x)));
            Assert.All(results1, x => Assert.Equal("abc", x));

            var results2 = await Task.WhenAll(new[] { "ac", "ca" }.Select(async x => await parser.ParseOrThrow(x)));
            Assert.All(results2, x => Assert.Equal("a_c", x));
        }
    }
}