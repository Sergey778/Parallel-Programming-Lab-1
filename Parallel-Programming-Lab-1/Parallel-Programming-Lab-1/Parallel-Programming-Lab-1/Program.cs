using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace Parallel_Programming_Lab_1
{
    public class Program
    {

        private static IEnumerable<int> BaseTask(IEnumerable<int> source, int[] result, int from, int to, Func<int, int> action)
        {
            var end = to < result.Length ? to : result.Length;
            for (var i = from; i < end; i++)
            {
                result[i] = action(source.ElementAt(i));
            }
            return result;
        }

        private static IEnumerable<int> HardTask(IEnumerable<int> source, int[] result, int from, int to, Func<int, int> action) 
        {
            var end = to < result.Length ? to : result.Length;
            for (var i = from; i < end; i++) 
            {
                var limit = (int)(Math.Log10(i) * 100);
                for (var j = 0; j < limit; j++)
                {
                    result[i] += action(source.ElementAt(i));
                }
            }
            return result;
        }

        private static IEnumerable<int> HardRingTask(IEnumerable<int> source, int[] result, int from, int to, int step, Func<int, int> action) 
        {
            var end = to < result.Length ? to : result.Length;
            for (var i = from; i < end; i += step) 
            {
                var limit = (int)(Math.Log10(i) * 100);
                for (var j = 0; j < limit; j++) 
                {
                    result[i] += action(source.ElementAt(i));
                }
            }
            return result;
        }

        public static IEnumerable<int> SeqTask(IEnumerable<int> list, Func<int, int> action) =>
            BaseTask(list, new int[list.Count()], 0, list.Count(), action);

        public static IEnumerable<int> SeqHardTask(IEnumerable<int> list, Func<int, int> action) =>
            HardTask(list, new int[list.Count()], 0, list.Count(), action);

        public static IEnumerable<int> ParTask(IEnumerable<int> list, Func<int, int> action, int threadCount = -1)
        {
            var result = new int[list.Count()];
            var processorCount = threadCount < 0 ? Environment.ProcessorCount : threadCount;
            var threads = new Thread[processorCount];
            var objectsPerThread = (int) Math.Ceiling(list.Count() / (double) processorCount);
            for (var i = 0; i < processorCount; i++)
            {
                threads[i] = new Thread((object a) =>
                {
                    var threadNumber = (int) a;
                    BaseTask(list, result, threadNumber * objectsPerThread, (threadNumber + 1) * objectsPerThread, action);
                });
                threads[i].Start(i);
            }
            foreach (var thread in threads)
                thread.Join();
            return result;
        }

        public static IEnumerable<int> ParHardTask(IEnumerable<int> list, Func<int, int> action, int threadCount = -1) 
        {
            var result = new int[list.Count()];
            var processorCount = threadCount < 0 ? Environment.ProcessorCount : threadCount;
            var threads = new Thread[processorCount];
            var objectsPerThread = (int) Math.Ceiling(list.Count() / (double) processorCount);
            for (var i = 0; i < processorCount; i++) 
            {
                threads[i] = new Thread((object a) => 
                {
                    var threadNumber = (int) a;
                    HardTask(list, result, threadNumber * objectsPerThread, (threadNumber + 1) * objectsPerThread, action);
                });
                threads[i].Start(i);
            }
            foreach (var thread in threads) 
                thread.Join();
            return result;
        }

        public static IEnumerable<int> ParHardRingTask(IEnumerable<int> list, Func<int, int> action, int threadCount = -1)
        {
            var result = new int[list.Count()];
            var processorCount = threadCount < 0 ? Environment.ProcessorCount : threadCount;
            var threads = new Thread[processorCount];
            var objectsPerThread = (int) Math.Ceiling(list.Count() / (double) processorCount);
            for (var i = 0; i < processorCount; i++) 
            {
                threads[i] = new Thread((object a) => {
                    var threadNumber = (int) a;
                    HardRingTask(list, result, threadNumber, list.Count(), processorCount, action);
                });
                threads[i].Start(i);
            }
            foreach (var thread in threads) 
                thread.Join();
            return result;
        }

        public class SimpleBenchmark
        {
            [Params(10, 100, 1000, 100000)]
            public int Length { get; set; }
            public IEnumerable<int> Data { get; set; }

            public int MaxValue = 100000;

            [Params(-1, 2, 3, 4, 5, 10)]
            public int ThreadCount { get; set; }

            public int Multiplier { get; } = 42;

            public Func<int, int> Action { get; } = x => (int)Math.Pow(x, 1.71);

            public Random Random { get; } = new Random();

            public SimpleBenchmark()
            {
            }

            [Setup]
            public void Setup() 
            {
                Data = new int[Length];
                Data = Data.Select(x => Random.Next(MaxValue));
            }

            [Benchmark]
            public IEnumerable<int> SeqBenchmark()
            {
                return SeqTask(Data, Action);
            }

            [Benchmark]
            public IEnumerable<int> ParBenchmark()
            {
                return ParTask(Data, Action, ThreadCount);
            }
        }

        public class HardBenchmark 
        {
            [Params(10, 100, 1000, 100000)]
            public int Length { get; set; }
            public IEnumerable<int> Data { get; set; }

            public int MaxValue = 100000;

            [Params(-1, 2, 3, 4, 5, 10)]
            public int ThreadCount { get; set; }

            public int Multiplier { get; } = 42;

            public Func<int, int> Action { get; } = x => (int)Math.Pow(x, 1.71);

            public Random Random { get; } = new Random();

            public HardBenchmark()
            {
            }

            [Setup]
            public void Setup() 
            {
                Data = new int[Length];
                Data = Data.Select(x => Random.Next(MaxValue));
            }

            [Benchmark]
            public IEnumerable<int> SeqBenchmark()
            {
                return SeqHardTask(Data, Action);
            }

            [Benchmark]
            public IEnumerable<int> ParBenchmark()
            {
                return ParHardTask(Data, Action, ThreadCount);
            }

            [Benchmark]
            public IEnumerable<int> ParRingBenchmark()
            {
                return ParHardRingTask(Data, Action, ThreadCount);
            }
        }

        static void Main(string[] args)
        {
            var simpleBenchmarkResult = BenchmarkRunner.Run<SimpleBenchmark>();
            var hardBenchmarkResult = BenchmarkRunner.Run<HardBenchmark>();
        }
    }
}
