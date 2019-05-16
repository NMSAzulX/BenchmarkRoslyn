using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Diagnostics;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var summary = BenchmarkRunner.Run<Watcher>(DefaultConfig.Instance.With(Job.Core));
           
            Console.ReadKey();
        }
    }
}
