using System;
using System.Linq;
using System.Text;
using ASD;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace ASD
{
    public abstract class Lab02TestCase : TestCase
    {
        protected readonly int[] Z;
        protected readonly (int radius, int cost)[] umbrellaTypes;
        protected readonly int expectedProfit;
        protected readonly int maxUmbrellas;
        protected (int profit, (int position, int model)[] umbrellas) result;

        protected Lab02TestCase(int[] Z, (int radius, int cost)[] umbrellaTypes, int maxUmbrellas, int expectedProfit, double timeLimit, string description)
            : base(timeLimit, null, description)
        {
            this.Z = (int[])Z.Clone();
            this.expectedProfit = expectedProfit;
            this.umbrellaTypes = ((int, int)[])umbrellaTypes.Clone();
            this.maxUmbrellas = maxUmbrellas;
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            var (code, msg) = CheckDeclaredCost();
            return (code, $"{msg} [{this.Description}]");
        }

        protected (Result resultCode, string message) CheckDeclaredCost()
        {
            if (result.profit != this.expectedProfit)
                return (Result.WrongResult, $"Zwrócono wartość zysku {result.profit}, a powinno być {this.expectedProfit}");

            if (result.umbrellas == null)
                return (Result.WrongResult, "Zysk OK, brak rozmieszczenia parasolek");

            if (result.umbrellas.Length > maxUmbrellas)
                return (Result.WrongResult, $"Rozmieszczono za dużo parasolek ({result.umbrellas.Length} na {maxUmbrellas} dostępne)");

            int[] umbrellaRadius = new int[Z.Length];
            for (int i = 0; i < Z.Length; i++)
                umbrellaRadius[i] = -1;
            int totalCost = 0;

            foreach ((int position, int model) in result.umbrellas)
            {
                if (position < 0 || position > Z.Length)
                    return (Result.WrongResult, $"Błędnie umieszczona parasolka na pozycji {position} (dostępny zakres od 0 do {Z.Length})");
                if (model < 0 || model > umbrellaTypes.Length)
                    return (Result.WrongResult, $"Błędny model parasolki {model} (dostępny zakres od 0 do {umbrellaTypes.Length})");
                umbrellaRadius[position] = Math.Max(umbrellaTypes[model].radius, umbrellaRadius[position]);
                totalCost += umbrellaTypes[model].cost;
            }

            bool[] covered = new bool[Z.Length];
            int maxUmbrellaEnd = -1;
            int profit = 0;
            for (int i = 0; i < Z.Length; i++)
            {
                maxUmbrellaEnd = Math.Max(i + umbrellaRadius[i], maxUmbrellaEnd);
                if (maxUmbrellaEnd >= i && !covered[i])
                {
                    covered[i] = true;
                    profit += Z[i];
                }
            }
            maxUmbrellaEnd = Z.Length;
            for (int i = Z.Length - 1; i >= 0; i--)
            {
                maxUmbrellaEnd = Math.Min(i - umbrellaRadius[i], maxUmbrellaEnd);
                if (maxUmbrellaEnd <= i && !covered[i])
                {
                    covered[i] = true;
                    profit += Z[i];
                }
            }

            if (profit - totalCost != result.profit)
                return (Result.WrongResult, $"Rozmieszczenie parasolek daje zysk {profit - totalCost} zamiast zadeklarowanego {result.profit}");


            return OkResult("OK");
        }

        public (Result resultCode, string message) OkResult(string message) =>
            (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success,
            $"{message} {PerformanceTime.ToString("#0.00")}s");
    }

    public class Stage1TestCase : Lab02TestCase
    {
        public Stage1TestCase(int[] Z, int umbrellaRadius, int maxUmbrellaNumber, int expectedProfit, double timeLimit, string description)
            : base(Z, new (int radius, int cost)[] { (umbrellaRadius, 0) }, maxUmbrellaNumber, expectedProfit, timeLimit, description)
        { }

        protected override void PerformTestCase(object prototypeObject)
        {
            (int profit, int[] umbrellas) c = ((Lab02)prototypeObject).Stage1(Z, maxUmbrellas, umbrellaTypes[0].radius);
            if (c.umbrellas == null)
            {
                result = (c.profit, null);
                return;
            }
            (int position, int model)[] umbrellas2 = new (int position, int model)[c.umbrellas.Length];
            for (int i = 0; i < umbrellas2.Length; i++)
                umbrellas2[i] = (c.umbrellas[i], 0);
            result = (c.profit, umbrellas2);
        }
    }

    public class Stage2TestCase : Lab02TestCase
    {
        private readonly int K;
        public Stage2TestCase(int[] Z, (int radius, int cost)[] umbrellaTypes, int expectedProfit, double timeLimit, string description)
            : base(Z, umbrellaTypes, int.MaxValue, expectedProfit, timeLimit, description)
        {
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            result = ((Lab02)prototypeObject).Stage2(Z, umbrellaTypes);
        }
    }

    public class Lab02Tests : TestModule
    {
        TestSet Stage1 = new TestSet(prototypeObject: new Lab02(), description: "Etap 1", settings: true);
        TestSet Stage2 = new TestSet(prototypeObject: new Lab02(), description: "Etap 2", settings: true);

        public override void PrepareTestSets()
        {
            TestSets["Stage1"] = Stage1;
            TestSets["Stage2"] = Stage2;
            Prepare();
        }

        private void addStage1(Stage1TestCase testCase)
        {
            Stage1.TestCases.Add(testCase);
        }

        private void addStage2(Stage2TestCase testCase)
        {
            Stage2.TestCases.Add(testCase);
        }

        private Stage1TestCase makeUniform1(int citySize, int radius, double timeLimit, string description)
        {
            int n = citySize;
            int r = radius;
            int[] Z = new int[n];
            for (int i = 0; i < n; i++)
                Z[i] = 1;
            int p = n / (2 * r + 1);
            int z = p * (2 * r + 1);
            return new Stage1TestCase(Z, r, p, z, timeLimit, description);
        }

        private Stage2TestCase makeUniform2(int citySize, int minRadius, int maxRadius, int optimalSolution, double timeLimit, string description)
        {
            int[] Z = new int[citySize];
            for (int i = 0; i < citySize; i++)
                Z[i] = maxRadius + 1 - minRadius;
            (int radius, int cost)[] models = new (int radius, int cost)[maxRadius - minRadius + 1];
            for (int m = minRadius; m <= maxRadius; m++)
                models[m - minRadius] = (m, (maxRadius + 1 - minRadius - (m - minRadius)) * (2 * m + 1));
            return new Stage2TestCase(Z, models, optimalSolution, timeLimit, description);

        }
        

        private void Prepare()
        {
            addStage1(new Stage1TestCase(new int[] { 0, 1, 0, 1, 0 }, 1, 1, 2, 1, "Jedna parasolka + dwa punkty z zyskiem"));
            addStage1(new Stage1TestCase(new int[] { 1, 6, 5, 0, 8, 2, 1, 9, 1, 3, 4, 1, 6, 4, 2 }, 2, 2, 39, 1, "Przykład z treści"));
            addStage1(new Stage1TestCase(new int[] { 1, 1, 2, 4, 3, 2, 0, 1, 4, 1, 3, 10, 0, 10, 0, 3, 3, 3 }, 2, 3, 50, 1, "Zyski jak w przykładzie z etapu 2, trzy parasolki o promieniu 2"));
            addStage1(new Stage1TestCase(new int[] { 3, 4, 3, 2, 5, 3, 1, 6, 3}, 3, 2, 30, 1, "Dwie duże parasolki"));
            addStage1(new Stage1TestCase(new int[] { 0, 3, 4, 0, 0, 0, 0, 3, 1, 3, 2, 3, 4, 0, 0, 0, 0, 0, 0, 0, 5, 3, 8, 2, 1, 14, 3, 1, 4, 4, 0, 0, 0, 0, 0}, 3, 3, 61, 1, "Trochę podstępny przykład"));
            addStage1(makeUniform1(5000, 5, 2, "Dużo małych parasolek"));
            addStage1(makeUniform1(200000, 10000, 2, "Mało dużych parasolek"));
            // addStage1(new Stage1TestCase(new int[] { 0, 1, 0, 1, 0 }, 0, 0, 0, 1, "zero parasolek"));


            addStage2(new Stage2TestCase(new int[] { 0, 1, 0, 1, 0 }, new (int, int)[] { (1, 1) }, 1, 1, "Parasolka za 1 + dwa punkty z zyskiem"));
            addStage2(new Stage2TestCase(new int[] { 1, 1, 2, 4, 3, 2, 0, 1, 4, 1, 3, 10, 0, 10, 0, 3, 3, 3 }, new (int, int)[] { (0, 2), (1, 5), (2, 7) }, 29, 1, "Przykład z treści"));
            addStage2(new Stage2TestCase(new int[] { 1, 6, 5, 0, 8, 2, 1, 9, 1, 3, 4, 1, 6, 4, 2 }, new (int, int)[] { (2, 10) }, 23, 1, "Przykład z etapu 1 z parasolką za 10"));
            addStage2(new Stage2TestCase(new int[] { 1, 6, 5, 0, 8, 2, 1, 9, 1, 3, 4, 1, 6, 4, 2 }, new (int, int)[] { (2, 17) }, 5, 1, "Przykład z etapu 1 z parasolką za 17"));
            addStage2(new Stage2TestCase(new int[] { 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, new (int, int)[] { (3, 14), (5, 22), (7, 30) }, 18, 1, "Równe zyski (3)"));
            addStage2(new Stage2TestCase(new int[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, new (int, int)[] { (3, 7), (5, 11), (7, 15) }, 19, 1, "Równe zyski (2)"));
            addStage2(makeUniform2(500000, 13, 23, 4999967, 2, "Dużo małych parasolek")); 
            addStage2(makeUniform2(500000, 12000, 12010, 4995559, 2, "Mało dużych parasolek"));
            addStage2(makeUniform2(10000, 10, 3010, 29997958, 2, "Dużo modeli parasolek"));
            // addStage2(new Stage2TestCase(new int[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }, new (int, int)[] { (3, 7), (5, 11), (7, 15) }, 19, 1, "Równe zyski (2)"));

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tests = new Lab02Tests();
            tests.PrepareTestSets();
            foreach (var ts in tests.TestSets)
            {
                ts.Value.PerformTests(verbose: true, checkTimeLimit: false);
            }
        }
    }
}
