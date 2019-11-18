using Pidgin.Examples.Json;
using System.Threading.Tasks;
using Xunit;

namespace Pidgin.Tests
{
    public class JsonTest
    {
        [Fact]
        public async Task TestJsonObject()
        {
            var input = "[ { \"foo\" : \"bar\" } , [ \"baz\" ] ]";

            var result = await JsonParser.Parse(input);

            Assert.Equal("[{\"foo\":\"bar\"},[\"baz\"]]", result.Value.ToString());
        }
    }
}