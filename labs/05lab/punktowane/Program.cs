using ASD.Graphs;
using ASD.Graphs.Testing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASD
{
    public abstract class Lab10TestCase : TestCase
    {
        protected readonly Graph graph;
        protected readonly int[] cost;
        protected readonly int maxBudget;
        protected readonly int expectedCost;

        protected int result;
        protected int[] answer;

        protected Lab10TestCase(
            Graph graph,
            int[] cost,
            int maxBudget,
            int expectedCost,
            double timeLimit,
            string description
        ) : base(timeLimit, null, description)
        {
            this.graph = (Graph)graph.Clone();
            this.cost = cost == null ? null : (int[])cost.Clone();
            this.maxBudget = maxBudget;
            this.expectedCost = expectedCost;
        }

        protected (Result resultCode, string message) CheckReturnedValue(string valueName)
        {
            if (result != expectedCost)
                return (Result.WrongResult, $"Zwrócono {valueName} {result}, oczekiwano {expectedCost} [{Description}]");

            return (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success, $"OK {PerformanceTime:0.00}s [{Description}]");
        }

        protected (Result resultCode, string message) CheckReturnedSet(int[] usedCost, string valueName)
        {
            if (answer == null)
                return (Result.WrongResult, $"Zwrócono S == null [{Description}]");

            if (answer.Any(v => v < 0 || v >= graph.VertexCount))
                return (Result.WrongResult, $"Zbiór S zawiera wierzchołek spoza grafu [{Description}]");

            if (answer.Distinct().Count() != answer.Length)
                return (Result.WrongResult, $"Zbiór S zawiera powtórzenia [{Description}]");

            if (!IsForest(graph, answer))
                return (Result.WrongResult, $"Graf G - S nie jest lasem [{Description}]");

            int actualCost = answer.Sum(v => usedCost[v]);
            if (actualCost != expectedCost)
                return (Result.WrongResult, $"{valueName} zbioru S wynosi {actualCost}, oczekiwano {expectedCost} [{Description}]");

            return (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success, $"OK {PerformanceTime:0.00}s [{Description}]");
        }

        private static bool IsForest(Graph G, int[] removed)
        {
            Graph G2 = (Graph)G.Clone();
            foreach (int v in removed)
                foreach (int u in G.OutNeighbors(v))
                    G2.RemoveEdge(v, u);

            int edges = 0;
            int cc = 0;
            bool[] visited = new bool[G2.VertexCount];
            foreach (Edge e in G2.DFS().SearchAll())
            {
                edges++;
                if (!visited[e.From])
                    cc++;
                visited[e.From] = visited[e.To] = true;

            }
            for (int i = 0; i < G2.VertexCount; i++)
                if (!visited[i]) cc++;


            edges /= 2;
            return edges + cc == G2.VertexCount;
        }

        protected static int[] UnitCosts(int n)
        {
            int[] result = new int[n];

            for (int i = 0; i < n; i++)
                result[i] = 1;

            return result;
        }
    }

    public class Stage1ValueTestCase : Lab10TestCase
    {
        public Stage1ValueTestCase(
            Graph graph,
            int maxBudget,
            int expectedCost,
            double timeLimit,
            string description
        ) : base(graph, null, maxBudget, expectedCost, timeLimit, description)
        { }

        protected override void PerformTestCase(object prototypeObject)
        {
            var solution = (Lab10)prototypeObject;

            result = solution.Stage1((Graph)graph.Clone(), maxBudget, out answer);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            return CheckReturnedValue("rozmiar");
        }
    }

    public class Stage1SetTestCase : Lab10TestCase
    {
        public Stage1SetTestCase(
            Graph graph,
            int maxBudget,
            int expectedCost,
            double timeLimit,
            string description
        ) : base(graph, null, maxBudget, expectedCost, timeLimit, description)
        { }

        protected override void PerformTestCase(object prototypeObject)
        {
            var solution = (Lab10)prototypeObject;

            result = solution.Stage1((Graph)graph.Clone(), maxBudget, out answer);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            return CheckReturnedSet(UnitCosts(graph.VertexCount), "Rozmiar");
        }
    }

    public class Stage2ValueTestCase : Lab10TestCase
    {
        public Stage2ValueTestCase(
            Graph graph,
            int[] cost,
            int maxBudget,
            int expectedCost,
            double timeLimit,
            string description
        ) : base(graph, cost, maxBudget, expectedCost, timeLimit, description)
        { }

        protected override void PerformTestCase(object prototypeObject)
        {
            var solution = (Lab10)prototypeObject;

            result = solution.Stage2((Graph)graph.Clone(), (int[])cost.Clone(), maxBudget, out answer);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            return CheckReturnedValue("koszt");
        }
    }

    public class Stage2SetTestCase : Lab10TestCase
    {
        public Stage2SetTestCase(
            Graph graph,
            int[] cost,
            int maxBudget,
            int expectedCost,
            double timeLimit,
            string description
        ) : base(graph, cost, maxBudget, expectedCost, timeLimit, description)
        { }

        protected override void PerformTestCase(object prototypeObject)
        {
            var solution = (Lab10)prototypeObject;

            result = solution.Stage2((Graph)graph.Clone(), (int[])cost.Clone(), maxBudget, out answer);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            return CheckReturnedSet(cost, "Koszt");
        }
    }

    public class Lab10Tests : TestModule
    {
        private readonly TestSet Stage1a = new TestSet(new Lab10(), "1A - poprawność rozmiaru");
        private readonly TestSet Stage1b = new TestSet(new Lab10(), "1B - poprawność zbioru");
        private readonly TestSet Stage2a = new TestSet(new Lab10(), "2A - poprawność kosztu");
        private readonly TestSet Stage2b = new TestSet(new Lab10(), "2B - poprawność zbioru");

        public override void PrepareTestSets()
        {
            TestSets["Stage1a"] = Stage1a;
            TestSets["Stage1b"] = Stage1b;
            TestSets["Stage2a"] = Stage2a;
            TestSets["Stage2b"] = Stage2b;

            PrepareStage1Tests();
            PrepareStage2Tests();
        }

        // --- helpers do generowania tablic kosztów ----------------------------

        private static int[] GenerateUniformArray(int n, int value)
        {
            int[] arr = new int[n];
            for (int i = 0; i < n; i++)
                arr[i] = value;
            return arr;
        }

        private static int[] GenerateRandomArray(int n, int minVal, int maxVal, int seed)
        {
            var rnd = new Random(seed);
            int[] arr = new int[n];
            for (int i = 0; i < n; i++)
                arr[i] = rnd.Next(minVal, maxVal + 1);
            return arr;
        }

        // --- konstruktory testów ----------------------------------------------

        private void AddStage1Test(Graph graph, int maxBudget, int expectedCost, double timeLimit, string description)
        {
            Stage1a.TestCases.Add(new Stage1ValueTestCase(graph, maxBudget, expectedCost, timeLimit, description));
            Stage1b.TestCases.Add(new Stage1SetTestCase(graph, maxBudget, expectedCost, timeLimit, description));
        }

        private void AddStage2Test(Graph graph, int[] cost, int maxBudget, int expectedCost, double timeLimit, string description)
        {
            Stage2a.TestCases.Add(new Stage2ValueTestCase(graph, cost, maxBudget, expectedCost, timeLimit, description));
            Stage2b.TestCases.Add(new Stage2SetTestCase(graph, cost, maxBudget, expectedCost, timeLimit, description));
        }

        // --- ETAP 1 -----------------------------------------------------------

        private void PrepareStage1Tests()
        {
            {
                var G = new Graph(5);

                AddStage1Test(G, 10, 0, 1, "Graf bez krawędzi");
            }

            {
                var G = new Graph(6);

                G.AddEdge(0, 1);
                G.AddEdge(0, 2);
                G.AddEdge(1, 3);
                G.AddEdge(1, 4);
                G.AddEdge(2, 5);

                AddStage1Test(G, 10, 0, 1, "Drzewo");
            }

            {
                var G = new Graph(3);

                G.AddEdge(0, 1);
                G.AddEdge(1, 2);
                G.AddEdge(0, 2);

                AddStage1Test(G, 10, 1, 1, "Trójkąt");
            }

            {
                var G = new Graph(30);

                for (int i = 0; i < 30; i++)
                    G.AddEdge(i, (i + 1) % 30);

                AddStage1Test(G, 10, 1, 1, "Długi cykl");
            }

            {
                var G = new Graph(5);

                for (int i = 0; i < 5; i++)
                    for (int j = i + 1; j < 5; j++)
                        G.AddEdge(i, j);

                AddStage1Test(G, 10, 3, 1, "Klika K5");
            }

            {
                var G = MakeExampleGraph();

                AddStage1Test(G, 10, 1, 1, "Przykład z treści zadania");
            }

            {
                var G = MakeDisjointTriangles(5);

                AddStage1Test(G, 10, 5, 1, "Rozłączne trójkąty");
            }

            {
                var G = MakeBouquetOfTriangles(4);

                AddStage1Test(G, 10, 1, 1, "Trójkąty o wspólnym wierzchołku");
            }

            {
                var G = MakePetersen();

                AddStage1Test(G, 10, 3, 2, "Graf Petersena");
            }

            {
                var G = new Graph(6);

                G.AddEdge(0, 1);
                G.AddEdge(0, 3);
                G.AddEdge(0, 4);
                G.AddEdge(0, 5);
                G.AddEdge(1, 2);
                G.AddEdge(1, 5);
                G.AddEdge(2, 3);
                G.AddEdge(2, 4);
                G.AddEdge(3, 4);

                AddStage1Test(G, 10, 2, 1, "Test Adama: siatka");
            }

            {
                var G = new Graph(7);

                G.AddEdge(0, 4);
                G.AddEdge(4, 5);
                G.AddEdge(4, 6);
                G.AddEdge(5, 6);
                G.AddEdge(1, 2);
                G.AddEdge(1, 3);
                G.AddEdge(2, 3);

                AddStage1Test(G, 10, 2, 1, "Test Adama: dwa cykle");
            }

            {
                var gen = new RandomGraphGenerator(5050);
                var G = gen.Graph(18, 0.7);

                AddStage1Test(G, 15, 10, 10, "'Mały' graf losowy");
            }

            {
                var gen = new RandomGraphGenerator(2718);
                var G = gen.Graph(24, 0.18);

                AddStage1Test(G, 15, 8, 10, "Rzadki graf losowy");
            }

            {
                var gen = new RandomGraphGenerator(5207);
                var G = gen.Graph(22, 0.4);

                AddStage1Test(G, 20, 10, 10, "Graf losowy");
            }

            {
                var gen = new RandomGraphGenerator(31337);
                var G = gen.Graph(20, 0.7);

                AddStage1Test(G, 25, 14, 10, "Gęsty graf losowy");
            }

            // --- Stress test ---
            //{
            //    var G = MakeSubdividedKnWithTails(5, 30, 4);

            //    AddStage1Test(G, 3, 3, 10, "Test domowy: podpodzielone K5 + ogony, V=325");
            //}
        }

        // --- ETAP 2 -----------------------------------------------------------

        private void PrepareStage2Tests()
        {
            {
                var G = new Graph(5);

                AddStage2Test(G, GenerateUniformArray(5, 7), 0, 0, 1, "Graf bez krawędzi");
            }

            {
                var G = new Graph(7);

                G.AddEdge(0, 1);
                G.AddEdge(0, 2);
                G.AddEdge(1, 3);
                G.AddEdge(1, 4);
                G.AddEdge(2, 5);
                G.AddEdge(2, 6);

                AddStage2Test(G, new[] { 100, 50, 50, 10, 10, 10, 10 }, 10, 0, 1, "Drzewo");
            }

            {
                var G = new Graph(3);

                G.AddEdge(0, 1);
                G.AddEdge(1, 2);
                G.AddEdge(0, 2);

                AddStage2Test(G, new[] { 10, 1, 10 }, 10, 1, 1, "Trójkąt");
            }

            {
                var G = new Graph(30);

                for (int i = 0; i < 30; i++)
                    G.AddEdge(i, (i + 1) % 30);

                int[] costs = GenerateUniformArray(30, 100);
                costs[17] = 1;

                AddStage2Test(G, costs, 10, 1, 1, "Długi cykl z jednym tanim wierzchołkiem");
            }

            {
                var G = new Graph(5);

                for (int i = 0; i < 5; i++)
                    for (int j = i + 1; j < 5; j++)
                        G.AddEdge(i, j);

                AddStage2Test(G, new[] { 5, 1, 2, 8, 3 }, 10, 6, 1, "Klika K5");
            }

            {
                var G = MakeExampleGraph();

                AddStage2Test(G, new[] { 1, 1, 10, 1, 1 }, 10, 2, 1, "Przykład z treści zadania");
            }

            {
                var G = MakeExampleGraph();

                AddStage2Test(G, new[] { 0, 2, 100, 1, 5 }, 10, 1, 1, "Przykład z treści zadania ze zmodyfikowanymi wagami");
            }

            {
                var G = MakeDisjointTriangles(5);

                int[] costs = new int[15];
                for (int i = 0; i < 15; i++)
                    costs[i] = (i % 3 == 1) ? 1 : 100;

                AddStage2Test(G, costs, 10, 5, 1, "Rozłączne trójkąty");
            }

            {
                var G = MakeBouquetOfTriangles(4);

                int[] costs = new int[9];
                costs[0] = 100;
                for (int i = 1; i < 9; i++)
                    costs[i] = 1;

                AddStage2Test(G, costs, 10, 4, 1, "Trójkąty o wspólnym wierzchołku");
            }

            {
                var G = MakePetersen();

                AddStage2Test(G, new[] { 3, 1, 4, 1, 5, 9, 2, 6, 5, 3 }, 10, 5, 2, "Graf Petersena");
            }

            {
                var gen = new RandomGraphGenerator(5052);
                var G = gen.Graph(18, 0.7);
                int[] costs = GenerateRandomArray(18, 1, 15, 5052);

                AddStage2Test(G, costs, 100, 63, 10, "'Mały' graf losowy");
            }

            {
                var gen = new RandomGraphGenerator(2718);
                var G = gen.Graph(24, 0.18);
                int[] costs = GenerateRandomArray(24, 1, 15, 2718);

                AddStage2Test(G, costs, 100, 56, 10, "Rzadki graf losowy");
            }

            {
                var gen = new RandomGraphGenerator(5207);
                var G = gen.Graph(22, 0.4);
                int[] costs = GenerateRandomArray(22, 1, 20, 5207);

                AddStage2Test(G, costs, 150, 104, 10, "Graf losowy");
            }

            {
                var gen = new RandomGraphGenerator(31337);
                var G = gen.Graph(20, 0.7);
                int[] costs = GenerateRandomArray(20, 1, 10, 31337);

                AddStage2Test(G, costs, 125, 85, 10, "Gęsty graf losowy");
            }

            // --- Stress test ---
            {
                var G = MakeSubdividedKnWithTails(5, 30, 4);
                int[] costs = MakeSubdividedKnCosts(5, 30, 4, cornerCost: 100, internalCost: 1, tailCost: 50);
            
                AddStage2Test(G, costs, 6, 6, 10, "Test domowy: podpodzielone K5 + ogony, V=325");
            }
        }

        // --- konstruktory grafów ----------------------------------------------

        private static Graph MakeExampleGraph()
        {
            var G = new Graph(5);

            G.AddEdge(0, 1);
            G.AddEdge(0, 2);
            G.AddEdge(1, 2);

            G.AddEdge(2, 3);
            G.AddEdge(2, 4);
            G.AddEdge(3, 4);

            return G;
        }

        private static Graph MakeDisjointTriangles(int k)
        {
            var G = new Graph(3 * k);

            for (int i = 0; i < k; i++)
            {
                int a = 3 * i, b = 3 * i + 1, c = 3 * i + 2;

                G.AddEdge(a, b);
                G.AddEdge(b, c);
                G.AddEdge(a, c);
            }

            return G;
        }

        private static Graph MakeBouquetOfTriangles(int k)
        {
            var G = new Graph(2 * k + 1);

            for (int i = 0; i < k; i++)
            {
                int a = 2 * i + 1, b = 2 * i + 2;

                G.AddEdge(0, a);
                G.AddEdge(0, b);
                G.AddEdge(a, b);
            }

            return G;
        }

        private static Graph MakePetersen()
        {
            var G = new Graph(10);

            for (int i = 0; i < 5; i++)
                G.AddEdge(i, (i + 1) % 5);

            G.AddEdge(5, 7);
            G.AddEdge(7, 9);
            G.AddEdge(9, 6);
            G.AddEdge(6, 8);
            G.AddEdge(8, 5);

            for (int i = 0; i < 5; i++)
                G.AddEdge(i, i + 5);

            return G;
        }
        private static Graph MakeSubdividedKnWithTails(int n, int internalsPerEdge, int tailLength)
        {
            int kEdges = n * (n - 1) / 2;
            int totalV = n + kEdges * internalsPerEdge + n * tailLength;
            var G = new Graph(totalV);
            int next = n;

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    int prev = i;

                    for (int k = 0; k < internalsPerEdge; k++)
                    {
                        G.AddEdge(prev, next);
                        prev = next;
                        next++;
                    }

                    G.AddEdge(prev, j);
                }
            }

            for (int corner = 0; corner < n; corner++)
            {
                int prev = corner;

                for (int k = 0; k < tailLength; k++)
                {
                    G.AddEdge(prev, next);
                    prev = next;
                    next++;
                }
            }

            return G;
        }

        private static int[] MakeSubdividedKnCosts(int n, int internalsPerEdge, int tailLength,
            int cornerCost, int internalCost, int tailCost)
        {
            int kEdges = n * (n - 1) / 2;
            int totalV = n + kEdges * internalsPerEdge + n * tailLength;
            int internalsEnd = n + kEdges * internalsPerEdge;
            int[] costs = new int[totalV];

            for (int i = 0; i < n; i++)
                costs[i] = cornerCost;
            for (int i = n; i < internalsEnd; i++)
                costs[i] = internalCost + (internalsPerEdge - 1 - (i - n) % internalsPerEdge) / 3;
            for (int i = internalsEnd; i < totalV; i++)
                costs[i] = tailCost;

            return costs;
        }

        // --- punktacja --------------------------------------------------------

        public override double ScoreResult()
        {
            double score = 0.0;

            if (Stage1a.PassedCount == Stage1a.TestCases.Count)
                score += 1.0;

            if (Stage1b.PassedCount == Stage1b.TestCases.Count)
                score += 0.5;

            if (Stage2a.PassedCount == Stage2a.TestCases.Count)
                score += 0.5;

            if (Stage2b.PassedCount == Stage2b.TestCases.Count)
                score += 0.5;

            return score;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tests = new Lab10Tests();
            tests.PrepareTestSets();

            foreach (var ts in tests.TestSets)
            {
                ts.Value.PerformTests(verbose: true, checkTimeLimit: true);
                Console.WriteLine();
            }
        }
    }
}
