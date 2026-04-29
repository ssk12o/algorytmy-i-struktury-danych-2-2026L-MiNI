using ASD;
using ASD.Graphs;
using ASD.Graphs.Testing;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using static ASD.TestCase;

namespace ASD2.Tests
{
    public abstract class TreasureTrackersTestCase : TestCase
    {
        protected readonly DiGraph graph;
        protected readonly int start;
        protected readonly int end;
        protected readonly int[] durability;
        protected readonly int[] opensOn;

        protected readonly int? expectedResult;
        protected int? result;

        protected TreasureTrackersTestCase(
            DiGraph graph,
            int start,
            int end,
            int[] durability,
            int[] opensOn,
            int? expectedResult,
            double timeLimit,
            string description
        ) : base(timeLimit, null, description)
        {
            this.graph = graph;
            this.start = start;
            this.end = end;
            this.durability = (int[])durability.Clone();
            this.opensOn = opensOn != null ? (int[])opensOn.Clone() : null;
            this.expectedResult = expectedResult;
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            if (result != expectedResult)
                return (Result.WrongResult,
                    $"Zwrócono {(result == null ? "null" : result.ToString())}, oczekiwano {(expectedResult == null ? "null" : expectedResult.ToString())} [{Description}]");

            return (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success,
                $"OK {PerformanceTime:0.00}s [{Description}]");
        }
    }

    public class Stage1TestCase : TreasureTrackersTestCase
    {
        private readonly int expeditionSize;

        public Stage1TestCase(
            DiGraph graph,
            int start,
            int end,
            int[] durability,
            int[] opensOn,
            int expeditionSize,
            int? expectedResult,
            double timeLimit,
            string description
        ) : base(graph, start, end, durability, opensOn, expectedResult, timeLimit, description)
        {
            this.expeditionSize = expeditionSize;
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            var sol = (TreasureTrackers)prototypeObject;

            result = sol.Stage1(
                graph,
                start,
                end,
                durability,
                opensOn,
                expeditionSize
            );
        }
    }

    public class Stage2TestCase : TreasureTrackersTestCase
    {
        public Stage2TestCase(
            DiGraph graph,
            int start,
            int end,
            int[] durability,
            int? expectedResult,
            double timeLimit,
            string description
        ) : base(graph, start, end, durability, null, expectedResult, timeLimit, description)
        { }

        protected override void PerformTestCase(object prototypeObject)
        {
            var sol = (TreasureTrackers)prototypeObject;

            result = sol.Stage2(
                graph,
                start,
                end,
                durability
            );
        }
    }

    public class TreasureTests : TestModule
    {
        private readonly TestSet Stage1 = new TestSet(new TreasureTrackers(), "Etap 1");
        private readonly TestSet Stage2 = new TestSet(new TreasureTrackers(), "Etap 2");

        public override void PrepareTestSets()
        {
            TestSets["Stage1"] = Stage1;
            TestSets["Stage2"] = Stage2;

            Prepare();
        }

        private static int[] GenerateRandomArray(int n, int minVal, int maxVal, int seed)
        {
            var rnd = new Random(seed);
            int[] arr = new int[n];
            for (int i = 0; i < n; i++)
                arr[i] = rnd.Next(minVal, maxVal + 1);
            return arr;
        }

        private static int[] GenerateUniformArray(int n, int value)
        {
            int[] arr = new int[n];
            for (int i = 0; i < n; i++)
                arr[i] = value;
            return arr;
        }

        private static int[] GenerateIncrementalArray(int n)
        {
            int[] arr = new int[n];
            for (int i = 0; i < n; i++)
                arr[i] = i;
            return arr;
        }

        static DiGraph GenerateStage2DAG(int n, int extraEdges, int seed)
        {
            var rnd = new Random(seed);

            var g = new DiGraph(n);

            for (int i = 0; i < n - 1; i++)
            {
                int to = rnd.Next(i + 1, n);

                g.AddEdge(i, to);
            }

            for (int v = 1; v < n; v++)
            {
                if (g.InDegree(v) == 0)
                {
                    int from = rnd.Next(0, v);

                    g.AddEdge(from, v);
                }
            }

            for(int i = 0; i < extraEdges; i++)
            {
                int a = rnd.Next(n);
                int b = rnd.Next(n);
                
                if(a == b)
                    continue;
                if (a >= b)
                    (a, b) = (b, a);

                g.AddEdge(a, b);
            }

            return g;
        }

        private void Prepare()
        {
            var gen = new RandomGraphGenerator(123);

            // ETAP 1
            {
                var g = new DiGraph(9);
                g.AddEdge(3, 0);
                g.AddEdge(0, 1);
                g.AddEdge(1, 2);
                g.AddEdge(2, 6);
                g.AddEdge(3, 4);
                g.AddEdge(4, 5);
                g.AddEdge(5, 6);
                g.AddEdge(3, 7);
                g.AddEdge(7, 8);
                g.AddEdge(8, 6);
                g.AddEdge(4, 1);
                g.AddEdge(7, 5);

                int[] durability = { 4, 5, 7, 8, 2, 2, 8, 5, 1 };
                int[] opensOn = { 0, 1, 1, 0, 1, 2, 0, 2, 3 };

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, 3, 6,
                    durability: durability,
                    opensOn: opensOn,
                    expeditionSize: 6,
                    expectedResult: 2,
                    timeLimit: 2,
                    description: "Przykład z treści zadania"
                ));
            }

            {
                var g = new DiGraph(5);
                g.AddEdge(0, 1);
                g.AddEdge(1, 2);
                g.AddEdge(2, 3);
                g.AddEdge(3, 4);

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, 0, 4,
                    new[] { 5, 5, 5, 5, 5 },
                    new[] { 0, 1, 2, 3, 4 },
                    5,
                    4,
                    1,
                    "Prosta ścieżka"
                ));
            }

            {
                var g = new DiGraph(5);
                g.AddEdge(0, 1);
                g.AddEdge(0, 2);
                g.AddEdge(0, 3);
                g.AddEdge(1, 4);
                g.AddEdge(2, 4);
                g.AddEdge(3, 4);

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, 0, 2,
                    new[] { 5, 1, 1, 1, 5 },
                    new[] { 0, 5, 10, 5, 0 },
                    4,
                    null,
                    1,
                    "Prosty przykład bez rozwiązania"
                ));
            }

            {
                var g = new DiGraph(9);
                g.AddEdge(3, 0);
                g.AddEdge(0, 1);
                g.AddEdge(1, 2);
                g.AddEdge(2, 6);
                g.AddEdge(3, 4);
                g.AddEdge(4, 5);
                g.AddEdge(5, 6);
                g.AddEdge(3, 7);
                g.AddEdge(7, 8);
                g.AddEdge(8, 6);
                g.AddEdge(4, 1);
                g.AddEdge(7, 5);

                int[] durability = { 4, 5, 7, 8, 2, 2, 8, 5, 1 };
                int[] opensOn = GenerateRandomArray(9, (int)1e8, (int)1e9, 1);
                opensOn[8] = (int)(1e9 + 1);

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, 3, 6,
                    durability: durability,
                    opensOn: opensOn,
                    expeditionSize: 6,
                    expectedResult: 794443710,
                    timeLimit: 1,
                    description: "Wysokie czasy otwarcia komnat"
                ));
            }

            {
                int n = 1000;

                var g = new DiGraph(n + 1);

                int[] durability = GenerateUniformArray(n + 1, 3);
                int[] opensOn = new int[n + 1];
                durability[0] = durability[n] = 5 * n;
                opensOn[0] = opensOn[n] = 0;

                for (int i = 1; i < n; i++)
                {
                    g.AddEdge(0, i);
                    g.AddEdge(i, n);
                    opensOn[i] = i;
                }

                Stage1.TestCases.Add(new Stage1TestCase(
                   g, 0, n,
                   durability: durability,
                   opensOn: opensOn,
                   expeditionSize: (int)Math.Ceiling(n * 1.7),
                   expectedResult: (int)Math.Ceiling((int)Math.Ceiling(n * 1.7) / 3.0),
                   timeLimit: 10,
                   description: "Wiele otwierających się po kolei krótkich ścieżek"
               ));
            }

            {
                int l = 10000;

                var g = new DiGraph(l + 2);

                int start = l, end = l + 1;

                g.AddEdge(start, 0);
                g.AddEdge(start, 1);
                g.AddEdge(start, 2);
                g.AddEdge(l - 3, end);
                g.AddEdge(l - 2, end);
                g.AddEdge(l - 1, end);

                for(int i = 0; i < l - 3; i++)
                {
                    g.AddEdge(i, i + 3);
                }

                int[] durability = GenerateUniformArray(l + 2, 1);
                int[] opensOn = GenerateIncrementalArray(l + 2);
                durability[start] = durability[end] = 3;

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, start, end,
                    durability: durability,
                    opensOn: opensOn,
                    expeditionSize: 3,
                    expectedResult: l + 1,
                    timeLimit: 5,
                    description: "Trzy długie ścieżki połączone końcami, każda komnata otwiera się w innym dniu"
                ));
            }

            {
                var g = gen.DiGraph(100, 0.25);

                int[] durability = GenerateRandomArray(100, 1, 100, 1234);
                int[] opensOn = GenerateRandomArray(100, 0, 1000, 1234);
                durability[0] = durability[99] = 500;

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, 0, 99,
                    durability: durability,
                    opensOn: opensOn,
                    expeditionSize: 500,
                    expectedResult: 596,
                    timeLimit: 10,
                    description: "Średni test losowy"
                ));
            }

            {
                var g = gen.DiGraph(250, 0.5);

                int[] durability = GenerateRandomArray(250, 1, 30, 11);
                int[] opensOn = GenerateRandomArray(250, 0, 30, 11);
                durability[0] = durability[249] = 60;

                Stage1.TestCases.Add(new Stage1TestCase(
                    g, 0, 249,
                    durability: durability,
                    opensOn: opensOn,
                    expeditionSize: 60,
                    expectedResult: 20,
                    timeLimit: 10,
                    description: "Duży test losowy"
                ));
            }

            // ETAP 2
            {
                var g = new DiGraph(8);
                g.AddEdge(2, 0);
                g.AddEdge(0, 1);
                g.AddEdge(1, 5);
                g.AddEdge(2, 3);
                g.AddEdge(3, 4);
                g.AddEdge(4, 5);
                g.AddEdge(2, 6);
                g.AddEdge(7, 5);
                g.AddEdge(6, 4);
                g.AddEdge(3, 7);

                var durability = new int[] { 1, 1, 4, 2, 2, 4, 2, 2 };

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 2, 5,
                    durability: durability,
                    expectedResult: 3,
                    timeLimit: 1,
                    description: "Przykład z treści zadania"
                ));
            }

            {
                var g = new DiGraph(5);
                g.AddEdge(0, 1);
                g.AddEdge(1, 2);
                g.AddEdge(2, 3);
                g.AddEdge(3, 4);

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 0, 4,
                    durability: new[] {5, 5, 5, 5, 5},
                    expectedResult: 1,
                    timeLimit: 1,
                    description: "Prosta ścieżka"
                ));
            }

            {
                var g = new DiGraph(7);
                g.AddEdge(0, 1);
                g.AddEdge(0, 2);
                g.AddEdge(1, 3);
                g.AddEdge(2, 3);
                g.AddEdge(3, 4);
                g.AddEdge(3, 5);
                g.AddEdge(4, 6);
                g.AddEdge(5, 6);

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 0, 6,
                    durability: new[] { 1, 1, 1, 1, 1, 1, 1 },
                    expectedResult: null,
                    timeLimit: 1,
                    description: "Brak rozwiązania - zbyt niska wytrzymałość komnat"
                ));
            }

            {
                var g = new DiGraph(6);
                g.AddEdge(0, 1);
                g.AddEdge(1, 2);
                g.AddEdge(2, 3);
                g.AddEdge(2, 5);
                g.AddEdge(3, 4);

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 0, 4,
                    durability: new[] { 5, 5, 5, 5, 5, 5 },
                    expectedResult: null,
                    timeLimit: 1,
                    description: "Brak rozwiązania - nie istnieje żadna ścieżka przez wierzchołek"
                ));
            }

            {
                int b = 64;

                var g = new DiGraph(2 * b);

                for (int i = 1; i < b; i++)
                {
                    g.AddEdge(i, i * 2);
                    g.AddEdge(i, i * 2 + 1);
                }
                for(int i = b; i < 2 * b; i++)
                {
                    g.AddEdge(i, 0);
                }

                var durability = GenerateUniformArray(2 * b, 4 * b);

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 1, 0,
                    durability: durability,
                    expectedResult: b,
                    timeLimit: 5,
                    description: "Drzewo binarne, którego liście łączą się z wyjściem"
                ));
            }

            {
                int n = 10;

                var g = new DiGraph(n * n);

                for(int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if(i != n - 1)
                            g.AddEdge(i * n + j, (i + 1) * n + j);

                        if(j != n - 1)
                            g.AddEdge(i * n + j, i * n + j + 1);
                    }
                }

                var durability = GenerateUniformArray(n * n, n * n);

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 0, n * n - 1,
                    durability: durability,
                    expectedResult: n,
                    timeLimit: 10,
                    description: "Siatka, w której możemy poruszać się w lewo i w dół"
                ));
            }

            {
                var g = GenerateStage2DAG(50, 400, 123);

                var durability = GenerateUniformArray(50, 1);
                durability[0] = durability[49] = 50;

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 0, 49,
                    durability: durability,
                    expectedResult: 4,
                    timeLimit: 5,
                    description: "Średni test losowy"
                ));
            }

            {
                var g = GenerateStage2DAG(200, 2500, 123);

                var durability = GenerateUniformArray(200, 25);

                Stage2.TestCases.Add(new Stage2TestCase(
                    g, 0, 199,
                    durability: durability,
                    expectedResult: 15,
                    timeLimit: 15,
                    description: "Duży test losowy"
                ));
            }
        }

        public override double ScoreResult()
        {
            double stage1score = 0, stage2score = 0;

            if (Stage1.PassedCount != Stage1.TestCases.Count)
                stage1score = -1.5;
            else
                stage1score = 1.5;

            if (Stage2.PassedCount != Stage2.TestCases.Count)
                stage2score = -1;
            else
                stage2score = 1;

            return stage1score + stage2score;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tests = new TreasureTests();
            tests.PrepareTestSets();

            foreach (var ts in tests.TestSets)
                ts.Value.PerformTests(verbose: true, checkTimeLimit: false);
        }
    }
}
