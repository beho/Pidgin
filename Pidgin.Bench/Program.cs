using BenchmarkDotNet.Running;
using System.Threading.Tasks;

namespace Pidgin.Bench
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            //var b = new JsonBench();
            //b.Setup();
            //await b.DeepJson_Pidgin();
        }
    }
}
