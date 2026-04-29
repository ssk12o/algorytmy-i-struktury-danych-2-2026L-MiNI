using System;
using System.Linq;
using ASD;
using ASD.Graphs;
using static ASD.TestCase;

namespace ASD2
{
    public class CoinFlowStage1TestCase : TestCase
    {
        protected readonly DiGraph<double> G;
        protected readonly int n;
        protected readonly bool expectedArbitrage;
        protected bool answer;

        public CoinFlowStage1TestCase(int n, DiGraph<double> G, bool expectedArbitrage, int timeLimit, string description)
            : base(timeLimit, null, description)
        {
            this.n = n;
            this.G = G;
            this.expectedArbitrage = expectedArbitrage;
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            answer = ((CoinFlow)prototypeObject).Stage1(G);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            if (answer == expectedArbitrage)
                return OkResult("OK");

            return (Result.WrongResult, $"Zwrocono {answer}, oczekiwano {expectedArbitrage}");
        }

        public (Result resultCode, string message) OkResult(string message) =>
            (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success, $"{message} {PerformanceTime.ToString("#0.00")}s");
    }

    public class CoinFlowStage2TestCase : TestCase
    {
        protected readonly DiGraph<double> G;
        protected readonly bool[] isStrong;
        protected readonly int k, startNode, endNode;
        protected readonly double? expectedMultiplier;
        protected int[] answer;

        public CoinFlowStage2TestCase(DiGraph<double> G, bool[] isStrong, int k, int startNode, int endNode, double? expectedMultiplier, int timeLimit, string description)
            : base(timeLimit, null, description)
        {
            this.G = G;
            this.isStrong = isStrong;
            this.k = k;
            this.startNode = startNode;
            this.endNode = endNode;
            this.expectedMultiplier = expectedMultiplier;
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            answer = ((CoinFlow)prototypeObject).Stage2(G, isStrong, k, startNode, endNode);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            if (answer == null && expectedMultiplier != null)
                return (Result.WrongResult, "Zwrocono brak sciezki, a sciezka istnieje.");
            if (answer != null && expectedMultiplier == null)
                return (Result.WrongResult, "Zwrocono sciezke, podczas gdy nie powinna istniec (lub przekracza limit).");
            if (answer == null && expectedMultiplier == null)
                return OkResult("OK (Brak sciezki - zgodnie z oczekiwaniami)");

            if (answer[0] != startNode)
                return (Result.WrongResult, $"Sciezka nie zaczyna sie w walucie startowej {startNode}");
            if (answer[answer.Length - 1] != endNode)
                return (Result.WrongResult, $"Sciezka nie konczy sie w walucie docelowej {endNode}");

            int weakCount = 0;
            double currentMultiplier = 1.0;

            for (int i = 0; i < answer.Length; i++)
            {
                if (!isStrong[answer[i]]) weakCount++;

                if (i < answer.Length - 1)
                {
                    int u = answer[i];
                    int v = answer[i + 1];
                    if (double.IsNaN(G.GetEdgeWeight(u, v)))
                        return (Result.WrongResult, $"Brak krawedzi w grafie miedzy {u} a {v}");

                    currentMultiplier *= G.GetEdgeWeight(u, v);
                }
            }

            if (weakCount > k)
                return (Result.WrongResult, $"Przekroczono limit slabych walut! Zuzyto {weakCount}, limit to {k}");

            if (Math.Abs(currentMultiplier - expectedMultiplier.Value) > 1e-4)
                return (Result.WrongResult, $"Mnoznik kapitalu wynosi {currentMultiplier}, oczekiwano {expectedMultiplier.Value}");

            return OkResult("OK");
        }

        public (Result resultCode, string message) OkResult(string message) =>
            (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success, $"{message} {PerformanceTime.ToString("#0.00")}s");
    }

    public class CoinFlowStage3TestCase : TestCase
    {
        protected readonly int n;
        protected readonly DiGraph<double> G;
        protected readonly (int currencyIdx, double rate)[] diamondToCurrency;
        protected readonly (int currencyIdx, double goldRate)[] currencyToGold;
        protected readonly double? expectedGold;
        protected int[] answer;

        public CoinFlowStage3TestCase(int n, DiGraph<double> G, (int, double)[] d2c, (int, double)[] c2g, double? expectedGold, int timeLimit, string description)
            : base(timeLimit, null, description)
        {
            this.n = n;
            this.G = G;
            this.diamondToCurrency = d2c;
            this.currencyToGold = c2g;
            this.expectedGold = expectedGold;
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            answer = ((CoinFlow)prototypeObject).Stage3(G, diamondToCurrency, currencyToGold);
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            if (answer == null && expectedGold != null)
                return (Result.WrongResult, "Zwrocono null, a istnieje droga wymiany na zloto.");
            if (answer != null && expectedGold == null)
                return (Result.WrongResult, "Zwrocono sciezke, a wymiana na zloto nie jest mozliwa.");
            if (answer == null && expectedGold == null)
                return OkResult("OK (Brak sciezki - zgodnie z oczekiwaniami)");

            int firstCurrency = answer[0];
            int lastCurrency = answer[answer.Length - 1];

            var startOffer = diamondToCurrency.FirstOrDefault(x => x.currencyIdx == firstCurrency);
            if (startOffer.rate == 0)
                return (Result.WrongResult, $"Waluta poczatkowa {firstCurrency} nie jest dostepna za diament.");

            var endOffer = currencyToGold.FirstOrDefault(x => x.currencyIdx == lastCurrency);
            if (endOffer.goldRate == 0)
                return (Result.WrongResult, $"Waluta koncowa {lastCurrency} nie ma bezposredniej wymiany na zloto.");

            double currentAmount = startOffer.rate;

            for (int i = 0; i < answer.Length - 1; i++)
            {
                int u = answer[i];
                int v = answer[i + 1];
                if (double.IsNaN(G.GetEdgeWeight(u, v)))
                    return (Result.WrongResult, $"Brak krawedzi w grafie miedzy {u} a {v}");

                currentAmount *= G.GetEdgeWeight(u, v);
            }

            currentAmount *= endOffer.goldRate;

            if (Math.Abs(currentAmount - expectedGold.Value) > 1e-4)
                return (Result.WrongResult, $"Uzyskano {currentAmount} zlota, oczekiwano {expectedGold.Value}");

            return OkResult("OK");
        }

        public (Result resultCode, string message) OkResult(string message) =>
            (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success, $"{message} {PerformanceTime.ToString("#0.00")}s");
    }

    public class CoinFlowTests : TestModule
    {
        TestSet Stage1 = new TestSet(prototypeObject: new CoinFlow(), description: "Etap I - Arbitraz", settings: true);
        TestSet Stage2 = new TestSet(prototypeObject: new CoinFlow(), description: "Etap II - Ograniczone Ryzyko", settings: true);
        TestSet Stage3 = new TestSet(prototypeObject: new CoinFlow(), description: "Etap III - Diamentowa Droga", settings: true);

        public override void PrepareTestSets()
        {
            TestSets["Etap I"] = Stage1;
            TestSets["Etap II"] = Stage2;
            TestSets["Etap III"] = Stage3;

            PrepareTests();
        }

        void PrepareTestsStage1()
        {
            DiGraph<double> G1 = new DiGraph<double>(3);
            G1.AddEdge(0, 1, 1.2f);
            G1.AddEdge(1, 2, 0.8f);
            G1.AddEdge(2, 0, 1.0f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(3, G1, false, 30, "T01: Prosty rynek stabilny"));

            DiGraph<double> G2 = new DiGraph<double>(3);
            G2.AddEdge(0, 1, 1.5f);
            G2.AddEdge(1, 2, 0.8f);
            G2.AddEdge(2, 0, 1.0f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(3, G2, true, 1, "T02: Prosty cykl arbitrazowy"));

            DiGraph<double> G3 = new DiGraph<double>(4);
            G3.AddEdge(0, 1, 0.5f);
            G3.AddEdge(1, 0, 0.5f);
            G3.AddEdge(2, 3, 2.0f);
            G3.AddEdge(3, 2, 2.0f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(4, G3, true, 1, "T03: Arbitraz w odizolowanej skladowej grafu niespojnego"));

            DiGraph<double> G5 = new DiGraph<double>(2);
            G5.AddEdge(0, 1, 1.5f);
            G5.AddEdge(1, 0, 1.5f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(2, G5, true, 1, "T04: Bardzo krotki cykl zyskowny (2 waluty)"));

            DiGraph<double> G6 = new DiGraph<double>(2);
            G6.AddEdge(0, 1, 0.9f);
            G6.AddEdge(1, 0, 0.9f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(2, G6, false, 1, "T05: Bardzo krotki cykl stratny"));

            DiGraph<double> G7 = new DiGraph<double>(4);
            G7.AddEdge(0, 1, 1000f);
            G7.AddEdge(1, 2, 1000f);
            G7.AddEdge(2, 3, 1000f);
            G7.AddEdge(3, 1, 0.0000001f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(4, G7, false, 1, "T06: Bardzo zyskowna sciezka otwarta, ale cykl stratny"));

            DiGraph<double> G8 = new DiGraph<double>(2);
            G8.AddEdge(0, 1, 1.0005f);
            G8.AddEdge(1, 0, 1.0005f);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(2, G8, true, 1, "T07: Bardzo delikatny mikro-arbitraz"));

            int n9 = 1000;
            var (G9, expected9) = GenerateRandomGraph(n9, edgesPerVertex: 5, hasArbitrage: true, seed: 12345, arbitrageCycleLength: 50);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n9, G9, expected9, 60, "T08: Zlozonosc - Duzy graf rzadki z dluuuugim cyklem (50 walut)"));

            int n10 = 500;
            var (G10, expected10) = GenerateRandomGraph(n10, edgesPerVertex: 250, hasArbitrage: false, seed: 98765, arbitrageCycleLength: 0);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n10, G10, expected10, 10, "T09: Zlozonosc - Duzy graf gesty bez arbitrazu"));

            int n11 = 800;
            var (G11, expected11) = GenerateRandomGraph(n11, edgesPerVertex: 10, hasArbitrage: false, seed: 11111, arbitrageCycleLength: 0);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n11, G11, expected11, 2, "T10: Zlozonosc - Sredni graf rzadki bez arbitrazu"));

            int n12 = 200;
            var (G12, expected12) = GenerateRandomGraph(n12, edgesPerVertex: 30, hasArbitrage: true, seed: 22222, arbitrageCycleLength: 50);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n12, G12, expected12, 20, "T11: Zlozonosc - Sredni graf gesty z arbitrazem (cykl: 50)"));

            int n13 = 200;
            var (G13, expected13) = GenerateRandomGraph(n13, edgesPerVertex: 150, hasArbitrage: false, seed: 33333, arbitrageCycleLength: 0);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n13, G13, expected13, 5, "T12: Zlozonosc - Mniejszy graf, bardzo gesty bez arbitrazu"));

            int n14 = 100;
            var (G14, expected14) = GenerateRandomGraph(n14, edgesPerVertex: 80, hasArbitrage: true, seed: 44444, arbitrageCycleLength: 5);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n14, G14, expected14, 30, "T13: Zlozonosc - Mniejszy graf, bardzo gesty z krotkim arbitrazem"));

            int n15 = 5000;
            var (G15, expected15) = GenerateRandomGraph(n15, edgesPerVertex: 2, hasArbitrage: false, seed: 55555, arbitrageCycleLength: 0);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n15, G15, expected15, 3, "T14: Zlozonosc - Ogromny graf (5000), ekstremalnie rzadki bez arbitrazu"));

            int n16 = 1000;
            var (G16, expected16) = GenerateRandomGraph(n16, edgesPerVertex: 3, hasArbitrage: true, seed: 66666, arbitrageCycleLength: 200);
            Stage1.TestCases.Add(new CoinFlowStage1TestCase(n16, G16, expected16, 60, "T15: Zlozonosc - Duzy graf rzadki z gigantycznym cyklem (200 walut)"));
        }

        (DiGraph<double> graph, bool expectedArbitrage) GenerateRandomGraph(int n, int edgesPerVertex, bool hasArbitrage, int seed, int arbitrageCycleLength)
        {
            DiGraph<double> G = new DiGraph<double>(n);
            Random random = new Random(seed);

            double[] potentials = new double[n];
            for (int i = 0; i < n; i++)
            {
                potentials[i] = (double)(random.NextDouble() * 100.0 + 1.0);
            }

            if (hasArbitrage && arbitrageCycleLength > 1)
            {
                int start = Math.Max(0, n - arbitrageCycleLength);
                for (int i = start; i < n; i++)
                {
                    int u = i;
                    int v = (i + 1 == n) ? start : i + 1;

                    double gainFactor = 1.01f;
                    double weight = (potentials[v] / potentials[u]) * gainFactor;

                    G.AddEdge(u, v, weight);
                }
            }

            for (int u = 0; u < n; u++)
            {
                int attempts = 0;
                int added = 0;

                while (added < edgesPerVertex && attempts < n * 2)
                {
                    attempts++;
                    int v = random.Next(n);

                    if (u == v || G.HasEdge(u, v)) continue;

                    double lossFactor = (double)(0.5 + random.NextDouble() * 0.45);
                    double weight = (potentials[v] / potentials[u]) * lossFactor;

                    G.AddEdge(u, v, weight);
                    added++;
                }
            }

            return (G, hasArbitrage);
        }

        (DiGraph<double> G, bool[] isStrong, double expectedMult) GeneratePlantedDagStage2(int n, int extraEdges, int k_limit, int seed)
        {
            DiGraph<double> G = new DiGraph<double>(n);
            Random rng = new Random(seed);
            bool[] isStrong = new bool[n];

            for (int i = 0; i < n; i++) isStrong[i] = true;

            int weakPlanted = 0;
            while (weakPlanted < k_limit && weakPlanted < n)
            {
                int idx = rng.Next(n);
                if (isStrong[idx])
                {
                    isStrong[idx] = false;
                    weakPlanted++;
                }
            }

            double expectedMult = 1.0;

            for (int i = 0; i < n - 1; i++)
            {
                double weight = 1.01f + (double)(rng.NextDouble() * 0.04);
                G.AddEdge(i, i + 1, weight);
                expectedMult *= weight;
            }

            int added = 0;
            int attempts = 0;
            while (added < extraEdges && attempts < extraEdges * 3)
            {
                attempts++;
                int u = rng.Next(n - 2);

                int v = rng.Next(u + 2, n);

                if (u < 0 || u >= n || v < 0 || v >= n || u >= v) continue;

                if (G.HasEdge(u, v)) continue;

                double lossWeight = (double)(0.2 + rng.NextDouble() * 0.6);
                G.AddEdge(u, v, lossWeight);
                added++;
            }

            return (G, isStrong, expectedMult);
        }

        void PrepareTestsStage3()
        {

            DiGraph<double> G1 = new DiGraph<double>(3);
            G1.AddEdge(0, 1, 0.5f);
            G1.AddEdge(1, 2, 0.5f);
            var d2c1 = new (int, double)[] { (0, 10.0), (1, 5.0) };
            var c2g1 = new (int, double)[] { (1, 10.0), (2, 2.0) };
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(3, G1, d2c1, c2g1, 50.0, 1, "T01: Brak wymian wewnetrznych (Sciezka 1-elementowa)"));

            DiGraph<double> G2 = new DiGraph<double>(4);
            G2.AddEdge(0, 1, 2.0f);
            G2.AddEdge(1, 3, 2.0f);
            G2.AddEdge(2, 3, 10.0f);
            var d2c2 = new (int, double)[] { (0, 100.0), (2, 5.0) };
            var c2g2 = new (int, double)[] { (3, 0.5) };
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(4, G2, d2c2, c2g2, 200.0, 1, "T02: Wybor dluzszego lancucha dzieki lepszej ofercie startowej"));

            DiGraph<double> G3 = new DiGraph<double>(4);
            G3.AddEdge(0, 1, 2.0f);
            G3.AddEdge(2, 3, 2.0f);
            var d2c3 = new (int, double)[] { (0, 10.0) };
            var c2g3 = new (int, double)[] { (3, 10.0) };
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(4, G3, d2c3, c2g3, null, 1, "T03: Brak sciezki (zwraca null)"));

            int n04 = 500;
            var (G04, d2c04, c2g04, expected04) = GeneratePlantedDagStage3(n04, extraEdges: 2000, seed: 111);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n04, G04, d2c04, c2g04, expected04, 2, $"T04: Zlozonosc - Sredni graf (N={n04})"));

            int n05 = 800;
            var (G05, d2c05, c2g05, expected05) = GeneratePlantedDagStage3(n05, extraEdges: 10000, seed: 222);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n05, G05, d2c05, c2g05, expected05, 3, $"T05: Zlozonosc - Duzy i gesty graf (N={n05}, E=~10000)"));

            int n06 = 4000;
            var (G06, d2c06, c2g06, expected06) = GeneratePlantedDagStage3(n06, extraEdges: 1000, seed: 333);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n06, G06, d2c06, c2g06, expected06, 4, $"T06: Zlozonosc - Ogromny dlugi lancuch (N={n06})"));

            int n07 = 5000;
            var (G07, d2c07, c2g07, expected07) = GeneratePlantedDagStage3(n07, extraEdges: 1000, seed: 444);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n07, G07, d2c07, c2g07, expected07, 4, $"T07: Zlozonosc - Ekstremalnie rzadki gigant (N={n07})"));

            int n08 = 200;
            var (G08, d2c08, c2g08, expected08) = GeneratePlantedDagStage3(n08, extraEdges: 15000, seed: 555);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n08, G08, d2c08, c2g08, expected08, 2, $"T08: Zlozonosc - Maly, ale ekstremalnie gesty graf (N={n08}, E=~15000)"));

            int n09 = 1000;
            var (G09, d2c09, c2g09, expected09) = GeneratePlantedDagStage3(n09, extraEdges: 5000, seed: 666);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n09, G09, d2c09, c2g09, expected09, 3, $"T09: Zlozonosc - Duzy graf z mnostwem falszywych ofert (N={n09})"));

            int n10 = 800;
            var (G10, d2c10, c2g10, expected10) = GeneratePlantedDagStage3(n10, extraEdges: 3000, seed: 777);
            var emptyC2G = new (int, double)[0];
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n10, G10, d2c10, emptyC2G, null, 2, $"T10: Zlozonosc - Duzy graf, uciety koniec (Brak ofert zlota, zwrot null)"));

            var emptyD2C = new (int, double)[0];
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n10, G10, emptyD2C, c2g10, null, 2, $"T11: Zlozonosc - Duzy graf, uciety start (Brak ofert za diament, zwrot null)"));

            int n12 = 1500;
            var (G12, d2c12, c2g12, expected12) = GeneratePlantedDagStage3(n12, extraEdges: 8000, seed: 888);
            Stage3.TestCases.Add(new CoinFlowStage3TestCase(n12, G12, d2c12, c2g12, expected12, 3, $"T12: Zlozonosc - Duzy i zbalansowany graf (N={n12}, E=~8000)"));
        }

        (DiGraph<double> G, (int, double)[] d2c, (int, double)[] c2g, double expectedGold) GeneratePlantedDagStage3(int n, int extraEdges, int seed)
        {
            DiGraph<double> G = new DiGraph<double>(n);
            Random rng = new Random(seed);

            double expectedGold = 100.0;

            for (int i = 0; i < n - 1; i++)
            {
                double weight = 1.01f + (double)(rng.NextDouble() * 0.04);
                G.AddEdge(i, i + 1, weight);
                expectedGold *= weight;
            }

            expectedGold *= 2.0;

            int added = 0;
            int attempts = 0;
            while (added < extraEdges && attempts < extraEdges * 3)
            {
                attempts++;
                int u = rng.Next(n - 2);
                int v = rng.Next(u + 2, n);

                if (u < 0 || u >= n || v < 0 || v >= n || u >= v) continue;
                if (G.HasEdge(u, v)) continue;

                double lossWeight = (double)(0.1 + rng.NextDouble() * 0.4);
                G.AddEdge(u, v, lossWeight);
                added++;
            }

            var d2cList = new System.Collections.Generic.List<(int, double)> { (0, 100.0) };
            var c2gList = new System.Collections.Generic.List<(int, double)> { (n - 1, 2.0) };

            int fakeOffers = n / 10;
            for (int i = 0; i < fakeOffers; i++)
            {
                int randomStart = rng.Next(1, n - 2);
                d2cList.Add((randomStart, rng.NextDouble() * 5.0));

                int randomEnd = rng.Next(1, n - 2);
                c2gList.Add((randomEnd, rng.NextDouble() * 0.1));
            }

            return (G, d2cList.ToArray(), c2gList.ToArray(), expectedGold);
        }

        void PrepareTestsStage2()
        {
            DiGraph<double> G1 = new DiGraph<double>(4);
            G1.AddEdge(0, 1, 2.0f);
            G1.AddEdge(1, 3, 2.0f);
            G1.AddEdge(0, 2, 1.5f);
            G1.AddEdge(2, 3, 1.5f);
            bool[] strong1 = { true, false, true, true };

            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G1, strong1, 1, 0, 3, 4.0, 1, "T01: Limit pozwala na 1 slaba (Wybor zyskownej sciezki)"));
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G1, strong1, 0, 0, 3, 2.25, 1, "T02: Limit 0 wymusza wybor slabszej, ale stabilnej sciezki"));

            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G1, strong1, 0, 1, 3, null, 1, "T03: Brak rozwiazania - startowa waluta jest slaba, a limit to 0"));

            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G1, strong1, 0, 0, 0, 1.0, 1, "T04: Start == Cel (mocna waluta, limit 0)"));
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G1, strong1, 1, 1, 1, 1.0, 1, "T05: Start == Cel (slaba waluta, limit 1)"));
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G1, strong1, 0, 1, 1, null, 1, "T06: Start == Cel (slaba waluta, limit 0 -> brak rozwiazania)"));

            DiGraph<double> G2 = new DiGraph<double>(3);
            G2.AddEdge(0, 1, 5.0f);
            bool[] strong2 = { true, true, true };
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G2, strong2, 10, 0, 2, null, 1, "T07: Cel fizycznie nieosiagalny"));

            DiGraph<double> G3 = new DiGraph<double>(3);
            G3.AddEdge(0, 1, 2.0f);
            G3.AddEdge(1, 2, 2.0f);
            bool[] strong3 = { true, false, false };
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G3, strong3, 1, 0, 2, null, 1, "T08: Cel dostepny, ale przekracza limit slabych walut"));

            int n09 = 150;
            int k09 = 50;
            var (G09, isStrong09, expected09) = GeneratePlantedDagStage2(n09, extraEdges: 500, k_limit: k09, seed: 123);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G09, isStrong09, k09, 0, n09 - 1, expected09, 2, $"T09: Zlozonosc - Sredni graf warstwowy (N={n09}, k={k09})"));

            int n10 = 300;
            int k10 = 10;
            var (G10, isStrong10, expected10) = GeneratePlantedDagStage2(n10, extraEdges: 1000, k_limit: k10, seed: 321);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G10, isStrong10, k10, 0, n10 - 1, expected10, 3, $"T10: Zlozonosc - Duzy graf (N={n10}, restrykcyjne k={k10})"));

            int n11 = 1000;
            int k11 = 2;
            var (G11, isStrong11, expected11) = GeneratePlantedDagStage2(n11, extraEdges: 2000, k_limit: k11, seed: 999);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G11, isStrong11, k11, 0, n11 - 1, expected11, 3, $"T11: Zlozonosc - Ogromny graf, malo warstw (N={n11}, k={k11})"));

            int n12 = 200;
            int k12 = 15;
            var (G12, isStrong12, expected12) = GeneratePlantedDagStage2(n12, extraEdges: 5000, k_limit: k12, seed: 444);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G12, isStrong12, k12, 0, n12 - 1, expected12, 4, $"T12: Zlozonosc - Sredni graf, baaardzo gesty (N={n12}, E=~5000, k={k12})"));

            int n13 = 1500;
            int k13 = 1;
            var (G13, isStrong13, expected13) = GeneratePlantedDagStage2(n13, extraEdges: 3000, k_limit: k13, seed: 555);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G13, isStrong13, k13, 0, n13 - 1, expected13, 2, $"T13: Zlozonosc - Duzy graf, limit k=1 (N={n13}, E=~4500)"));

            int n14 = 250;
            int k14 = 50;
            var (G14, isStrong14, expected14) = GeneratePlantedDagStage2(n14, extraEdges: 1500, k_limit: k14, seed: 666);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G14, isStrong14, k14, 0, n14 - 1, expected14, 6, $"T14: Zlozonosc - Test maksymalnego obciazenia k (N={n14}, k={k14}, E=~1750)"));

            int n15 = 800;
            int k15 = 20;
            var (G15, isStrong15, expected15) = GeneratePlantedDagStage2(n15, extraEdges: 2500, k_limit: k15, seed: 777);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G15, isStrong15, k15, 0, n15 - 1, expected15, 3, $"T15: Zlozonosc - Dlugi graf (N={n15}, k={k15})"));

            int n16 = 500;
            DiGraph<double> G16 = new DiGraph<double>(n16);
            bool[] isStrong16 = new bool[n16];

            for (int i = 0; i < n16 - 1; i++)
            {
                G16.AddEdge(i, i + 1, 1.1f);
                isStrong16[i] = false;
            }
            isStrong16[n16 - 1] = false;

            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G16, isStrong16, 400, 0, n16 - 1, null, 2, "T16: Brak rozwiazania - za dlugi lancuch slabych walut w duzym grafie"));

            int n17 = 3000;
            int k17 = 5;
            var (G17, isStrong17, expected17) = GeneratePlantedDagStage2(n17, extraEdges: 0, k_limit: k17, seed: 888);
            Stage2.TestCases.Add(new CoinFlowStage2TestCase(G17, isStrong17, k17, 0, n17 - 1, expected17, 2, $"T17: Zlozonosc - Czysta linia bez skrotow (N={n17}, k={k17})"));
        }

        void PrepareTests()
        {
            PrepareTestsStage1();
            PrepareTestsStage2();
            PrepareTestsStage3();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tests = new CoinFlowTests();
            tests.PrepareTestSets();
            foreach (var ts in tests.TestSets)
            {
                ts.Value.PerformTests(verbose: true, checkTimeLimit: false);
            }
        }
    }
}