using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace Benchmarks;

class Program
{
    static void Main(string[] args)
    {

        var config = 
            DefaultConfig.Instance
            // IP2Location is not optimized.
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        BenchmarkRunner.Run<LookupBench>(config);
    }
}
