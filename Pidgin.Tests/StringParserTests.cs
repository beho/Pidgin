using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace Pidgin.Tests
{
    public class StringParserTests : ParserTestBase
    {
        [Fact]
        public async Task TestReturn()
        {
            {
                var parser = Return('a');
                AssertSuccess(await parser.Parse(""), 'a', false);
                AssertSuccess(await parser.Parse("foobar"), 'a', false);
            }
            {
                var parser = FromResult('a');
                AssertSuccess(await parser.Parse(""), 'a', false);
                AssertSuccess(await parser.Parse("foobar"), 'a', false);
            }
        }

        [Fact]
        public async Task TestFail()
        {
            {
                var parser = Fail<Unit>("message");
                var expectedError = new ParseError<char>(
                    Maybe.Nothing<char>(),
                    false,
                    new[] { new Expected<char>(ImmutableArray.Create<char>()) },
                    new SourcePos(1, 1),
                    "message"
                );
                AssertFailure(await parser.Parse(""), expectedError, false);
                AssertFailure(await parser.Parse("foobar"), expectedError, false);
            }
        }

        [Fact]
        public async Task TestToken()
        {
            {
                var parser = Char('a');
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("ab"), 'a', true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create('a')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('a')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = AnyCharExcept('a', 'b', 'c');
                AssertSuccess(await parser.Parse("e"), 'e', true);
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = Token('a'.Equals);
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("ab"), 'a', true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new Expected<char>[] { },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = Any;
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("b"), 'b', true);
                AssertSuccess(await parser.Parse("ab"), 'a', true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("any character") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = Whitespace;
                AssertSuccess(await parser.Parse("\r"), '\r', true);
                AssertSuccess(await parser.Parse("\n"), '\n', true);
                AssertSuccess(await parser.Parse("\t"), '\t', true);
                AssertSuccess(await parser.Parse(" "), ' ', true);
                AssertSuccess(await parser.Parse(" abc"), ' ', true);
                AssertFailure(
                    await parser.Parse("abc"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new[] { new Expected<char>("whitespace") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("whitespace") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestCIChar()
        {
            {
                var parser = CIChar('a');
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("ab"), 'a', true);
                AssertSuccess(await parser.Parse("A"), 'A', true);
                AssertSuccess(await parser.Parse("AB"), 'A', true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create('A')), new Expected<char>(ImmutableArray.Create('a')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('A')), new Expected<char>(ImmutableArray.Create('a')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestEnd()
        {
            {
                var parser = End;
                AssertSuccess(await parser.Parse(""), Unit.Value, false);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new[] { new Expected<char>() },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestNumber()
        {
            {
                var parser = Num;
                AssertSuccess(await parser.Parse("0"), 0, true);
                AssertSuccess(await parser.Parse("+0"), +0, true);
                AssertSuccess(await parser.Parse("-0"), -0, true);
                AssertSuccess(await parser.Parse("1"), 1, true);
                AssertSuccess(await parser.Parse("+1"), +1, true);
                AssertSuccess(await parser.Parse("-1"), -1, true);
                AssertSuccess(await parser.Parse("12345"), 12345, true);
                AssertSuccess(await parser.Parse("1a"), 1, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("+"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("-"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
            {
                var parser = HexNum;
                AssertSuccess(await parser.Parse("ab"), 0xab, true);
                AssertSuccess(await parser.Parse("cd"), 0xcd, true);
                AssertSuccess(await parser.Parse("ef"), 0xef, true);
                AssertSuccess(await parser.Parse("AB"), 0xAB, true);
                AssertSuccess(await parser.Parse("CD"), 0xCD, true);
                AssertSuccess(await parser.Parse("EF"), 0xEF, true);
                AssertFailure(
                    await parser.Parse("g"),
                    new ParseError<char>(
                        Maybe.Just('g'),
                        false,
                        new[] { new Expected<char>("hexadecimal number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = OctalNum;
                AssertSuccess(await parser.Parse("7"), 7, true);
                AssertFailure(
                    await parser.Parse("8"),
                    new ParseError<char>(
                        Maybe.Just('8'),
                        false,
                        new[] { new Expected<char>("octal number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = LongNum;
                AssertSuccess(await parser.Parse("0"), 0L, true);
                AssertSuccess(await parser.Parse("+0"), +0L, true);
                AssertSuccess(await parser.Parse("-0"), -0L, true);
                AssertSuccess(await parser.Parse("1"), 1L, true);
                AssertSuccess(await parser.Parse("+1"), +1L, true);
                AssertSuccess(await parser.Parse("-1"), -1L, true);
                AssertSuccess(await parser.Parse("12345"), 12345L, true);
                var tooBigForInt = ((long)int.MaxValue) + 1;
                AssertSuccess(await parser.Parse(tooBigForInt.ToString()), tooBigForInt, true);
                AssertSuccess(await parser.Parse("1a"), 1, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("+"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("-"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("number") },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Real;
                AssertSuccess(await parser.Parse("0"), 0d, true);
                AssertSuccess(await parser.Parse("+0"), +0d, true);
                AssertSuccess(await parser.Parse("-0"), -0d, true);
                AssertSuccess(await parser.Parse("1"), 1d, true);
                AssertSuccess(await parser.Parse("+1"), +1d, true);
                AssertSuccess(await parser.Parse("-1"), -1d, true);

                AssertSuccess(await parser.Parse("12345"), 12345d, true);
                AssertSuccess(await parser.Parse("+12345"), +12345d, true);
                AssertSuccess(await parser.Parse("-12345"), -12345d, true);

                AssertSuccess(await parser.Parse("12.345"), 12.345d, true);
                AssertSuccess(await parser.Parse("+12.345"), +12.345d, true);
                AssertSuccess(await parser.Parse("-12.345"), -12.345d, true);

                AssertSuccess(await parser.Parse(".12345"), .12345d, true);
                AssertSuccess(await parser.Parse("+.12345"), +.12345d, true);
                AssertSuccess(await parser.Parse("-.12345"), -.12345d, true);

                AssertSuccess(await parser.Parse("12345e10"), 12345e10d, true);
                AssertSuccess(await parser.Parse("+12345e10"), +12345e10d, true);
                AssertSuccess(await parser.Parse("-12345e10"), -12345e10d, true);
                AssertSuccess(await parser.Parse("12345e+10"), 12345e+10d, true);
                AssertSuccess(await parser.Parse("+12345e+10"), +12345e+10d, true);
                AssertSuccess(await parser.Parse("-12345e+10"), -12345e+10d, true);
                AssertSuccess(await parser.Parse("12345e-10"), 12345e-10d, true);
                AssertSuccess(await parser.Parse("+12345e-10"), +12345e-10d, true);
                AssertSuccess(await parser.Parse("-12345e-10"), -12345e-10d, true);

                AssertSuccess(await parser.Parse("12.345e10"), 12.345e10d, true);
                AssertSuccess(await parser.Parse("+12.345e10"), +12.345e10d, true);
                AssertSuccess(await parser.Parse("-12.345e10"), -12.345e10d, true);
                AssertSuccess(await parser.Parse("12.345e+10"), 12.345e+10d, true);
                AssertSuccess(await parser.Parse("+12.345e+10"), +12.345e+10d, true);
                AssertSuccess(await parser.Parse("-12.345e+10"), -12.345e+10d, true);
                AssertSuccess(await parser.Parse("12.345e-10"), 12.345e-10d, true);
                AssertSuccess(await parser.Parse("+12.345e-10"), +12.345e-10d, true);
                AssertSuccess(await parser.Parse("-12.345e-10"), -12.345e-10d, true);

                AssertSuccess(await parser.Parse(".12345e10"), .12345e10d, true);
                AssertSuccess(await parser.Parse("+.12345e10"), +.12345e10d, true);
                AssertSuccess(await parser.Parse("-.12345e10"), -.12345e10d, true);
                AssertSuccess(await parser.Parse(".12345e+10"), .12345e+10d, true);
                AssertSuccess(await parser.Parse("+.12345e+10"), +.12345e+10d, true);
                AssertSuccess(await parser.Parse("-.12345e+10"), -.12345e+10d, true);
                AssertSuccess(await parser.Parse(".12345e-10"), .12345e-10d, true);
                AssertSuccess(await parser.Parse("+.12345e-10"), +.12345e-10d, true);
                AssertSuccess(await parser.Parse("-.12345e-10"), -.12345e-10d, true);


                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("+"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("-"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("12345."),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 7),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("12345e"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 7),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("12345e+"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 8),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("12345.e"),
                    new ParseError<char>(
                        Maybe.Just('e'),
                        false,
                        new[] { new Expected<char>("real number") },
                        new SourcePos(1, 7),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSequence()
        {
            {
                var parser = String("foo");
                AssertSuccess(await parser.Parse("foo"), "foo", true);
                AssertSuccess(await parser.Parse("food"), "foo", true);
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = Sequence(Char('f'), Char('o'), Char('o'));
                AssertSuccess(await parser.Parse("foo"), "foo", true);
                AssertSuccess(await parser.Parse("food"), "foo", true);
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("f")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("o")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("f")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestCIString()
        {
            {
                var parser = CIString("foo");
                AssertSuccess(await parser.Parse("foo"), "foo", true);
                AssertSuccess(await parser.Parse("food"), "foo", true);
                AssertSuccess(await parser.Parse("FOO"), "FOO", true);
                AssertSuccess(await parser.Parse("FOOD"), "FOO", true);
                AssertSuccess(await parser.Parse("fOo"), "fOo", true);
                AssertSuccess(await parser.Parse("Food"), "Foo", true);
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("FOul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestBind()
        {
            {
                // any two equal characters
                var parser = Any.Then(c => Token(c.Equals));
                AssertSuccess(await parser.Parse("aa"), 'a', true);
                AssertFailure(
                    await parser.Parse("ab"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Any.Bind(c => Token(c.Equals), (x, y) => new { x, y });
                AssertSuccess(await parser.Parse("aa"), new { x = 'a', y = 'a' }, true);
                AssertFailure(
                    await parser.Parse("ab"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Any.Then(c => Token(c.Equals), (x, y) => new { x, y });
                AssertSuccess(await parser.Parse("aa"), new { x = 'a', y = 'a' }, true);
                AssertFailure(
                    await parser.Parse("ab"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
            {
                var parser =
                    from x in Any
                    from y in Token(x.Equals)
                    select new { x, y };
                AssertSuccess(await parser.Parse("aa"), new { x = 'a', y = 'a' }, true);
                AssertFailure(
                    await parser.Parse("ab"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Char('x').Then(c => Char('y'));
                AssertSuccess(await parser.Parse("xy"), 'y', true);
                AssertFailure(
                    await parser.Parse("yy"),
                    new ParseError<char>(
                        Maybe.Just('y'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('x')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("xx"),
                    new ParseError<char>(
                        Maybe.Just('x'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('y')) },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestThen()
        {
            {
                var parser = Char('a').Then(Char('b'));
                AssertSuccess(await parser.Parse("ab"), 'b', true);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("b")) },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("a")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = Char('a').Then(Char('b'), (a, b) => new { a, b });
                AssertSuccess(await parser.Parse("ab"), new { a = 'a', b = 'b' }, true);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("b")) },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("a")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = Char('a').Before(Char('b'));
                AssertSuccess(await parser.Parse("ab"), 'a', true);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("b")) },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("a")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestMap()
        {
            {
                var parser = Map((x, y, z) => new { x, y, z }, Char('a'), Char('b'), Char('c'));
                AssertSuccess(await parser.Parse("abc"), new { x = 'a', y = 'b', z = 'c' }, true);
                AssertFailure(
                    await parser.Parse("abd"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("c")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Char('a').Select(a => new { a });
                AssertSuccess(await parser.Parse("a"), new { a = 'a' }, true);
            }
            {
                var parser = Char('a').Map(a => new { a });
                AssertSuccess(await parser.Parse("a"), new { a = 'a' }, true);
            }
            {
                var parser =
                    from a in Char('a')
                    select new { a };
                AssertSuccess(await parser.Parse("a"), new { a = 'a' }, true);
            }
        }

        [Fact]
        public async Task TestOr()
        {
            {
                var parser = Fail<char>("test").Or(Char('a'));
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create<char>()), new Expected<char>(ImmutableArray.Create('a')) },
                        new SourcePos(1, 1),
                        "test"
                    ),
                    false
                );
            }
            {
                var parser = Char('a').Or(Char('b'));
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("b"), 'b', true);
                AssertFailure(
                    await parser.Parse("c"),
                    new ParseError<char>(
                        Maybe.Just('c'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('a')), new Expected<char>(ImmutableArray.Create('b')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = String("foo").Or(String("bar"));
                AssertSuccess(await parser.Parse("foo"), "foo", true);
                AssertSuccess(await parser.Parse("bar"), "bar", true);
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
            {
                var parser = String("foo").Or(String("foul"));
                // because the first parser consumed input
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Try(String("foo")).Or(String("foul"));
                AssertSuccess(await parser.Parse("foul"), "foul", true);
            }
        }

        [Fact]
        public async Task TestOneOf()
        {
            {
                var parser = OneOf(Char('a'), Char('b'), Char('c'));
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("b"), 'b', true);
                AssertSuccess(await parser.Parse("c"), 'c', true);
                AssertFailure(
                    await parser.Parse("d"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('a')), new Expected<char>(ImmutableArray.Create('b')), new Expected<char>(ImmutableArray.Create('c')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = OneOf("abc");
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("b"), 'b', true);
                AssertSuccess(await parser.Parse("c"), 'c', true);
                AssertFailure(
                    await parser.Parse("d"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('a')), new Expected<char>(ImmutableArray.Create('b')), new Expected<char>(ImmutableArray.Create('c')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = OneOf(String("foo"), String("bar"));
                AssertSuccess(await parser.Parse("foo"), "foo", true);
                AssertSuccess(await parser.Parse("bar"), "bar", true);
                AssertFailure(
                    await parser.Parse("quux"),
                    new ParseError<char>(
                        Maybe.Just('q'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")), new Expected<char>(ImmutableArray.CreateRange("bar")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestCIOneOf()
        {
            {
                var parser = CIOneOf('a', 'b', 'c');
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("b"), 'b', true);
                AssertSuccess(await parser.Parse("c"), 'c', true);
                AssertSuccess(await parser.Parse("A"), 'A', true);
                AssertSuccess(await parser.Parse("B"), 'B', true);
                AssertSuccess(await parser.Parse("C"), 'C', true);
                AssertFailure(
                    await parser.Parse("d"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[]
                        {
                            new Expected<char>(ImmutableArray.Create('a')),
                            new Expected<char>(ImmutableArray.Create('A')),
                            new Expected<char>(ImmutableArray.Create('b')),
                            new Expected<char>(ImmutableArray.Create('B')),
                            new Expected<char>(ImmutableArray.Create('c')),
                            new Expected<char>(ImmutableArray.Create('C'))
                        },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                var parser = CIOneOf("abc");
                AssertSuccess(await parser.Parse("a"), 'a', true);
                AssertSuccess(await parser.Parse("b"), 'b', true);
                AssertSuccess(await parser.Parse("c"), 'c', true);
                AssertSuccess(await parser.Parse("A"), 'A', true);
                AssertSuccess(await parser.Parse("B"), 'B', true);
                AssertSuccess(await parser.Parse("C"), 'C', true);
                AssertFailure(
                    await parser.Parse("d"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[]
                        {
                            new Expected<char>(ImmutableArray.Create('a')),
                            new Expected<char>(ImmutableArray.Create('A')),
                            new Expected<char>(ImmutableArray.Create('b')),
                            new Expected<char>(ImmutableArray.Create('B')),
                            new Expected<char>(ImmutableArray.Create('c')),
                            new Expected<char>(ImmutableArray.Create('C'))
                        },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestNot()
        {
            {
                var parser = Not(String("food")).Then(String("bar"));
                AssertSuccess(await parser.Parse("foobar"), "bar", true);
            }
            {
                var parser = Not(OneOf(Char('a'), Char('b'), Char('c')));
                AssertSuccess(await parser.Parse("e"), Unit.Value, false);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 1),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Not(Return('f'));
                AssertFailure(
                    await parser.Parse("foobar"),
                    new ParseError<char>(
                        Maybe.Just('f'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
            {
                // test to make sure it doesn't throw out the buffer, for the purposes of computing error position
                var str = new string('a', 10000);
                var parser = Not(String(str));
                AssertFailure(
                    await parser.Parse(str),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 1),
                        null
                    ),
                    true
                );
            }
            {
                // test error pos calculation
                var parser = Char('a').Then(Not(Char('b')));
                AssertFailure(
                    await parser.Parse("ab"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new Expected<char>[] { },
                        new SourcePos(1, 2),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestLookahead()
        {
            {
                var parser = Lookahead(String("foo"));
                AssertSuccess(await parser.Parse("foo"), "foo", false);
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foe"),
                    new ParseError<char>(
                        Maybe.Just('e'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
            {
                // should backtrack on success
                var parser = Lookahead(String("foo")).Then(String("food"));
                AssertSuccess(await parser.Parse("food"), "food", true);
            }
        }

        [Fact]
        public async Task TestRecoverWith()
        {
            {
                var parser = String("foo").ThenReturn((ParseError<char>?)null)
                    .RecoverWith(err => String("bar").ThenReturn(err)!);

                AssertSuccess(
                    await parser.Parse("fobar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
            {
                var parser = String("nabble").ThenReturn((ParseError<char>?)null)
                    .Or(
                        String("foo").ThenReturn((ParseError<char>?)null)
                            .RecoverWith(err => String("bar").ThenReturn(err)!)
                    );

                // shouldn't get the expected from nabble
                AssertSuccess(
                    await parser.Parse("fobar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestTryUsingStaticExample()
        {
            {
                string MkString(char first, IEnumerable<char> rest)
                {
                    var sb = new StringBuilder();
                    sb.Append(first);
                    sb.Append(string.Concat(rest));
                    return sb.ToString();
                }

                var pUsing = String("using");
                var pStatic = String("static");
                var identifier = Token(char.IsLetter)
                    .Then(Token(char.IsLetterOrDigit).Many(), MkString)
                    .Labelled("identifier");
                var usingStatic =
                    from kws in Try(
                        from u in pUsing.Before(Whitespace.AtLeastOnce())
                        from s in pStatic.Before(Whitespace.AtLeastOnce())
                        select new { }
                    )
                    from id in identifier
                    select new { isStatic = true, id };
                var notStatic =
                    from u in pUsing
                    from ws in Whitespace.AtLeastOnce()
                    from id in identifier
                    select new { isStatic = false, id };
                var parser = usingStatic.Or(notStatic);

                AssertSuccess(await parser.Parse("using static Console"), new { isStatic = true, id = "Console" }, true);
                AssertSuccess(await parser.Parse("using System"), new { isStatic = false, id = "System" }, true);
                AssertFailure(
                    await parser.Parse("usine"),
                    new ParseError<char>(
                        Maybe.Just('e'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("using")) },
                        new SourcePos(1, 5),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("using 123"),
                    new ParseError<char>(
                        Maybe.Just('1'),
                        false,
                        new[] { new Expected<char>("identifier") },
                        new SourcePos(1, 7),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestAssert()
        {
            {
                var parser = Char('a').Assert('a'.Equals);
                AssertSuccess(await parser.Parse("a"), 'a', true);
            }
            {
                var parser = Char('a').Assert('b'.Equals);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        false,
                        new[] { new Expected<char>("result satisfying assertion") },
                        new SourcePos(1, 2),
                        "Assertion failed"
                    ),
                    true
                );
            }
            {
                var parser = Char('a').Where('a'.Equals);
                AssertSuccess(await parser.Parse("a"), 'a', true);
            }
            {
                var parser = Char('a').Where('b'.Equals);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        false,
                        new[] { new Expected<char>("result satisfying assertion") },
                        new SourcePos(1, 2),
                        "Assertion failed"
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestMany()
        {
            {
                var parser = String("foo").Many();
                AssertSuccess(await parser.Parse(""), Enumerable.Empty<string>(), false);
                AssertSuccess(await parser.Parse("bar"), Enumerable.Empty<string>(), false);
                AssertSuccess(await parser.Parse("foo"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foofoo"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("food"), new[] { "foo" }, true);
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Whitespaces;
                AssertSuccess(await parser.Parse("    "), new[] { ' ', ' ', ' ', ' ' }, true);
                AssertSuccess(await parser.Parse("\r\n"), new[] { '\r', '\n' }, true);
                AssertSuccess(await parser.Parse(" abc"), new[] { ' ' }, true);
                AssertSuccess(await parser.Parse("abc"), Enumerable.Empty<char>(), false);
                AssertSuccess(await parser.Parse(""), Enumerable.Empty<char>(), false);
            }
            {
                var parser = Return(1).Many();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestManyString()
        {
            {
                var parser = Char('f').ManyString();
                AssertSuccess(await parser.Parse(""), "", false);
                AssertSuccess(await parser.Parse("bar"), "", false);
                AssertSuccess(await parser.Parse("f"), "f", true);
                AssertSuccess(await parser.Parse("ff"), "ff", true);
                AssertSuccess(await parser.Parse("fo"), "f", true);
            }
            {
                var parser = String("f").ManyString();
                AssertSuccess(await parser.Parse(""), "", false);
                AssertSuccess(await parser.Parse("bar"), "", false);
                AssertSuccess(await parser.Parse("f"), "f", true);
                AssertSuccess(await parser.Parse("ff"), "ff", true);
                AssertSuccess(await parser.Parse("fo"), "f", true);
            }
            {
                var parser = Return('f').ManyString();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestSkipMany()
        {
            {
                var parser = String("foo").SkipMany();
                AssertSuccess(await parser.Parse(""), Unit.Value, false);
                AssertSuccess(await parser.Parse("bar"), Unit.Value, false);
                AssertSuccess(await parser.Parse("foo"), Unit.Value, true);
                AssertSuccess(await parser.Parse("foofoo"), Unit.Value, true);
                AssertSuccess(await parser.Parse("food"), Unit.Value, true);
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = SkipWhitespaces.Then(End);
                AssertSuccess(await parser.Parse("    "), Unit.Value, true);
                AssertSuccess(await parser.Parse("\r\n\t"), Unit.Value, true);
                AssertSuccess(await parser.Parse(""), Unit.Value, false);
                AssertSuccess(await parser.Parse(new string(' ', 32)), Unit.Value, true);
                AssertSuccess(await parser.Parse(new string(' ', 33)), Unit.Value, true);
                AssertSuccess(await parser.Parse(new string(' ', 64)), Unit.Value, true);
                AssertSuccess(await parser.Parse(new string(' ', 65)), Unit.Value, true);
            }
            {
                var parser = Return(1).SkipMany();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestAtLeastOnce()
        {
            {
                var parser = String("foo").AtLeastOnce();
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertSuccess(await parser.Parse("foo"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foofoo"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("food"), new[] { "foo" }, true);
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Return(1).AtLeastOnce();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestAtLeastOnceString()
        {
            {
                var parser = Char('f').AtLeastOnceString();
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create('f')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('f')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertSuccess(await parser.Parse("f"), "f", true);
                AssertSuccess(await parser.Parse("ff"), "ff", true);
                AssertSuccess(await parser.Parse("fg"), "f", true);
            }
            {
                var parser = String("f").AtLeastOnceString();
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create('f')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("b"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create('f')) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertSuccess(await parser.Parse("f"), "f", true);
                AssertSuccess(await parser.Parse("ff"), "ff", true);
                AssertSuccess(await parser.Parse("fg"), "f", true);
            }
            {
                var parser = Return('f').AtLeastOnceString();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestSkipAtLeastOnce()
        {
            {
                var parser = String("foo").SkipAtLeastOnce();
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertSuccess(await parser.Parse("foo"), Unit.Value, true);
                AssertSuccess(await parser.Parse("foofoo"), Unit.Value, true);
                AssertSuccess(await parser.Parse("food"), Unit.Value, true);
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Return(1).SkipAtLeastOnce();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestUntil()
        {
            {
                var parser = String("foo").Until(Char(' '));
                AssertSuccess(await parser.Parse(" "), Enumerable.Empty<string>(), true);
                AssertSuccess(await parser.Parse(" bar"), Enumerable.Empty<string>(), true);
                AssertSuccess(await parser.Parse("foo "), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foofoo "), new[] { "foo", "foo" }, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("food"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Return(1).Until(Char(' '));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestSkipUntil()
        {
            {
                var parser = String("foo").SkipUntil(Char(' '));
                AssertSuccess(await parser.Parse(" "), Unit.Value, true);
                AssertSuccess(await parser.Parse(" bar"), Unit.Value, true);
                AssertSuccess(await parser.Parse("foo "), Unit.Value, true);
                AssertSuccess(await parser.Parse("foofoo "), Unit.Value, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("food"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Return(1).SkipUntil(Char(' '));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestAtLeastOnceUntil()
        {
            {
                var parser = String("foo").AtLeastOnceUntil(Char(' '));
                AssertSuccess(await parser.Parse("foo "), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foofoo "), new[] { "foo", "foo" }, true);
                AssertFailure(
                    await parser.Parse(" "),
                    new ParseError<char>(
                        Maybe.Just(' '),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse(" bar"),
                    new ParseError<char>(
                        Maybe.Just(' '),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("food"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Return(1).AtLeastOnceUntil(Char(' '));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestSkipAtLeastOnceUntil()
        {
            {
                var parser = String("foo").SkipAtLeastOnceUntil(Char(' '));
                AssertSuccess(await parser.Parse("foo "), Unit.Value, true);
                AssertSuccess(await parser.Parse("foofoo "), Unit.Value, true);
                AssertFailure(
                    await parser.Parse(" "),
                    new ParseError<char>(
                        Maybe.Just(' '),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse(" bar"),
                    new ParseError<char>(
                        Maybe.Just(' '),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("food"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')), new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foofoul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 6),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Return(1).SkipAtLeastOnceUntil(Char(' '));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await parser.Parse(""));
            }
        }

        [Fact]
        public async Task TestRepeat()
        {
            {
                var parser = String("foo").Repeat(3);
                AssertSuccess(await parser.Parse("foofoofoo"), new[] { "foo", "foo", "foo" }, true);
                AssertFailure(
                    await parser.Parse("foofoo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 7),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSeparated()
        {
            {
                var parser = String("foo").Separated(Char(' '));
                AssertSuccess(await parser.Parse(""), Enumerable.Empty<string>(), false);
                AssertSuccess(await parser.Parse("foo"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foobar"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("bar"), Enumerable.Empty<string>(), false);
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foo bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 5),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSeparatedAtLeastOnce()
        {
            {
                var parser = String("foo").SeparatedAtLeastOnce(Char(' '));
                AssertSuccess(await parser.Parse("foo"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foobar"), new[] { "foo" }, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foo bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 5),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSeparatedAndTerminated()
        {
            {
                var parser = String("foo").SeparatedAndTerminated(Char(' '));
                AssertSuccess(await parser.Parse("foo "), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo "), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo bar"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse(""), new string[] { }, false);
                AssertSuccess(await parser.Parse("bar"), new string[] { }, false);
                AssertFailure(
                    await parser.Parse("foo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange(" ")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foobar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange(" ")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foo foobar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange(" ")) },
                        new SourcePos(1, 8),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSeparatedAndTerminatedAtLeastOnce()
        {
            {
                var parser = String("foo").SeparatedAndTerminatedAtLeastOnce(Char(' '));
                AssertSuccess(await parser.Parse("foo "), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo "), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo bar"), new[] { "foo" }, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("foo"),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.Create(' ')) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foobar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange(" ")) },
                        new SourcePos(1, 4),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foo foobar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange(" ")) },
                        new SourcePos(1, 8),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSeparatedAndOptionallyTerminated()
        {
            {
                var parser = String("foo").SeparatedAndOptionallyTerminated(Char(' '));
                AssertSuccess(await parser.Parse("foo "), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo "), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo foobar"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo bar"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo bar"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foobar"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse(""), new string[] { }, false);
                AssertSuccess(await parser.Parse("bar"), new string[] { }, false);
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
                AssertFailure(
                    await parser.Parse("foo four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 7),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestSeparatedAndOptionallyTerminatedAtLeastOnce()
        {
            {
                var parser = String("foo").SeparatedAndOptionallyTerminatedAtLeastOnce(Char(' '));
                AssertSuccess(await parser.Parse("foo "), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo "), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo foobar"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo foo bar"), new[] { "foo", "foo" }, true);
                AssertSuccess(await parser.Parse("foo bar"), new[] { "foo" }, true);
                AssertSuccess(await parser.Parse("foobar"), new[] { "foo" }, true);
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        true,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("bar"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestBetween()
        {
            {
                var parser = String("foo").Between(Char('{'), Char('}'));
                AssertSuccess(await parser.Parse("{foo}"), "foo", true);
            }
        }

        [Fact]
        public async Task TestOptional()
        {
            {
                var parser = String("foo").Optional();
                AssertSuccess(await parser.Parse("foo"), Maybe.Just("foo"), true);
                AssertSuccess(await parser.Parse("food"), Maybe.Just("foo"), true);
                AssertSuccess(await parser.Parse("bar"), Maybe.Nothing<string>(), false);
                AssertSuccess(await parser.Parse(""), Maybe.Nothing<string>(), false);
                AssertFailure(
                    await parser.Parse("four"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("foo")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
            {
                var parser = Try(String("foo")).Optional();
                AssertSuccess(await parser.Parse("foo"), Maybe.Just("foo"), true);
                AssertSuccess(await parser.Parse("food"), Maybe.Just("foo"), true);
                AssertSuccess(await parser.Parse("bar"), Maybe.Nothing<string>(), false);
                AssertSuccess(await parser.Parse(""), Maybe.Nothing<string>(), false);
                AssertSuccess(await parser.Parse("four"), Maybe.Nothing<string>(), false);
            }
            {
                var parser = Char('+').Optional().Then(Digit).Select(char.GetNumericValue);
                AssertSuccess(await parser.Parse("1"), 1, true);
                AssertSuccess(await parser.Parse("+1"), 1, true);
                AssertFailure(
                    await parser.Parse("a"),
                    new ParseError<char>(
                        Maybe.Just('a'),
                        false,
                        new[] { new Expected<char>("digit") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestMapWithInput()
        {
            {
                var parser = String("abc").Many().MapWithInput((input, result) => (input.ToString(), result.Count()));
                AssertSuccess(await parser.Parse("abc"), ("abc", 1), true);
                AssertSuccess(await parser.Parse("abcabc"), ("abcabc", 2), true);
                AssertSuccess(  // long input, to check that it doesn't discard the buffer
                    await parser.Parse(string.Concat(Enumerable.Repeat("abc", 5000))),
                    (string.Concat(Enumerable.Repeat("abc", 5000)), 5000),
                    true
                );

                AssertFailure(
                    await parser.Parse("abd"),
                    new ParseError<char>(
                        Maybe.Just('d'),
                        false,
                        new[] { new Expected<char>(ImmutableArray.CreateRange("abc")) },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
        }

        [Fact]
        public async Task TestRec()
        {
            // roughly equivalent to String("foo").Separated(Char(' '))
            Parser<char, string>? p2 = null;
            var p1 = String("foo").Then(
                Rec(() => p2!).Optional(),
                (x, y) => y.HasValue ? x + y.Value : x
            );
            p2 = Char(' ').Then(Rec(() => p1));

            AssertSuccess(await p1.Parse("foo foo"), "foofoo", true);
        }

        [Fact]
        public async Task TestLabelled()
        {
            {
                var p = String("foo").Labelled("bar");
                AssertFailure(
                    await p.Parse("baz"),
                    new ParseError<char>(
                        Maybe.Just('b'),
                        false,
                        new[] { new Expected<char>("bar") },
                        new SourcePos(1, 1),
                        null
                    ),
                    false
                );
                AssertFailure(
                    await p.Parse("foul"),
                    new ParseError<char>(
                        Maybe.Just('u'),
                        false,
                        new[] { new Expected<char>("bar") },
                        new SourcePos(1, 3),
                        null
                    ),
                    true
                );
            }
        }

        private class TestCast1 { }
        private class TestCast2 : TestCast1
        {
            public override bool Equals(object? other) => other is TestCast2;
            public override int GetHashCode() => 1;
        }
        [Fact]
        public async Task TestCast()
        {
            {
                var parser = Return(new TestCast2()).Cast<TestCast1>();
                AssertSuccess(await parser.Parse(""), new TestCast2(), false);
            }
            {
                var parser = Return(new TestCast1()).OfType<TestCast2>();
                AssertFailure(
                    await parser.Parse(""),
                    new ParseError<char>(
                        Maybe.Nothing<char>(),
                        false,
                        new[] { new Expected<char>("result of type TestCast2") },
                        new SourcePos(1, 1),
                        "Expected a TestCast2 but got a TestCast1"
                    ),
                    false
                );
            }
        }

        [Fact]
        public async Task TestCurrentPos()
        {
            {
                var parser = CurrentPos;
                AssertSuccess(await parser.Parse(""), new SourcePos(1, 1), false);
            }
            {
                var parser = String("foo").Then(CurrentPos);
                AssertSuccess(await parser.Parse("foo"), new SourcePos(1, 4), true);
            }
            {
                var parser = Try(String("foo")).Or(Return("")).Then(CurrentPos);
                AssertSuccess(await parser.Parse("f"), new SourcePos(1, 1), false);  // it should backtrack
            }
        }
    }
}
