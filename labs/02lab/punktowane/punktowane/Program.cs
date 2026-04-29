using System;
using ASD.Graphs;
using ASD.Graphs.Testing;
using System.Collections.Generic;

namespace ASD
{

    public abstract class Lab04TestCase : TestCase
    {
        protected readonly DiGraph<int> graph;
        protected readonly int daysNumber;
        private readonly bool checkRoute;
        protected readonly bool expectedResult;

        protected (bool res, int[] route) result;

        public Lab04TestCase(DiGraph<int> g, int n, bool expectedRes, double timeLimit, string description, bool checkR) : base(timeLimit, null, description)
        {
            this.graph = g;
            this.daysNumber = n;
            this.expectedResult = expectedRes;
            this.checkRoute = checkR;
        }

        protected override (Result resultCode, string message) VerifyTestCase(object settings)
        {
            var (code, msg) = checkSolution(result.res, result.route);
            return (code, $"{msg} [{this.Description}]");
        }

        private (Result resultCode, string message) checkSolution(bool returnedResult, int[] returnedRoute)
        {
            if (expectedResult != returnedResult)
            {
                return (Result.WrongResult, $"Zwrócono {returnedResult}, powinno być {expectedResult}");
            }

            if (!checkRoute)
            {
                return OkResult("OK");
            }

            if (!returnedResult)
            {
                if (returnedRoute != null)
                    return (Result.WrongResult, "Nie ma trasy, należało zwrócić null");
                return OkResult("OK");
            }

            if (returnedRoute == null)
            {
                return (Result.WrongResult, "Zwrócono null zamiast trasy");
            }
            if (returnedRoute.Length == 0)
            {
                return (Result.WrongResult, "Zwrócono pustą trasę");
            }
            if (returnedRoute.Length == 1)
            {
                return (Result.WrongResult, "Zwrócono trasę o 1 wierzchołku");
            }

            // sprawdzenie czy wszystkie miasta są na mapie
            foreach (int i in returnedRoute)
                if (i < 0 || i > graph.VertexCount)
                    return (Result.WrongResult, "Na trasie znajduje się miasto spoza mapy");

            // sprawdzenie czy trasa jest realizowalna
            if (!graph.HasEdge(returnedRoute[0], returnedRoute[1]))
                return (Result.WrongResult, $"Nie istnieje połączenie z miasta {returnedRoute[0]} do {returnedRoute[1]}");
            int act_day = graph.GetEdgeWeight(returnedRoute[0], returnedRoute[1]);
            for (int i = 0; i < returnedRoute.Length - 1; ++i)
            {
                // sprawdzenie czy istnieje trasa pomiedzy kolejnymi miastami
                if (!graph.HasEdge(returnedRoute[i], returnedRoute[i + 1]))
                    return (Result.WrongResult, $"Nie istnieje połączenie z miasta {returnedRoute[i]} do {returnedRoute[i + 1]}");

                // sprawdzenie czy dzien sie zgadza
                int edge_weight = graph.GetEdgeWeight(returnedRoute[i], returnedRoute[i + 1]);
                if (edge_weight != act_day)
                    return (Result.WrongResult, $"Nie można w dniu {act_day} wyjechać z miasta {returnedRoute[i]} do {returnedRoute[i + 1]} (pociąg jeździ tylko w dniu {edge_weight}");
                act_day = (act_day + 1) % daysNumber;
            }
            return checkRouteEnds(returnedRoute);

        }

        protected abstract (Result resultCode, string message) checkRouteEnds(int[] returnedRoute);
        public (Result resultCode, string message) OkResult(string message) => (TimeLimit < PerformanceTime ? Result.LowEfficiency : Result.Success, $"{message} {PerformanceTime.ToString("#0.00")}s");

    }

    public class Stage1TestCase : Lab04TestCase
    {
        private readonly int start;
        private readonly int end;
        private readonly int day;
        public Stage1TestCase(DiGraph<int> g, int startV, int endV, int day, int n, bool expectedRes, double timeLimit, string description, bool checkR) : base(g, n, expectedRes, timeLimit, description, checkR)
        {
            this.start = startV;
            this.end = endV;
            this.day = day;
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            result = ((Lab04)prototypeObject).Lab04_FindRoute(graph, start, end, day, daysNumber);
        }

        protected override (Result resultCode, string message) checkRouteEnds(int[] returnedRoute)
        {
            // sprawdzenie startu i konca trasy
            if (returnedRoute[0] != start)
                return (Result.WrongResult, "Trasa nie zaczyna się w mieście startowym");
            if (returnedRoute[returnedRoute.Length - 1] != end)
                return (Result.WrongResult, "Trasa nie kończy się w mieście docelowym");
            // sprawdzenie czy startujemy w dobrym dniu
            if (graph.GetEdgeWeight(returnedRoute[0], returnedRoute[1]) != day)
                return (Result.WrongResult, $"Błędny dzień startu (jest {graph.GetEdgeWeight(returnedRoute[0], returnedRoute[1])}, a powinno być {day}");
            return OkResult("OK");
        }
    }

    public class Stage2TestCase : Lab04TestCase
    {
        private readonly int[] startSet;
        private readonly int[] endSet;
        public Stage2TestCase(DiGraph<int> g, int[] startV, int[] endV, int n, bool expectedRes, double timeLimit, string description, bool checkR) : base(g, n, expectedRes, timeLimit, description, checkR)
        {
            this.startSet = startV;
            this.endSet = endV;
        }

        protected override void PerformTestCase(object prototypeObject)
        {
            result = ((Lab04)prototypeObject).Lab04_FindRouteSets(graph, startSet, endSet, daysNumber);
        }

        protected override (Result resultCode, string message) checkRouteEnds(int[] returnedRoute)
        {
            // sprawdzenie startu i konca trasy
            if (!Array.Exists<int>(startSet, x => x == returnedRoute[0]))
                return (Result.WrongResult, "Trasa nie zaczyna się w żadnym z miast startowych");
            if (!Array.Exists<int>(endSet, x => x == returnedRoute[returnedRoute.Length - 1]))
                return (Result.WrongResult, "Trasa nie kończy się w żadnym z miast docelowych");
            return OkResult("OK");
        }
    }
    public class Lab04Tests : TestModule
    {
        TestSet Stage1a = new TestSet(prototypeObject: new Lab04(), description: "Etap 1, odpowiedź czy trasa istnieje", settings: true);
        TestSet Stage1b = new TestSet(prototypeObject: new Lab04(), description: "Etap 1, zwrócenie trasy", settings: true);
        TestSet Stage2a = new TestSet(prototypeObject: new Lab04(), description: "Etap 2, odpowiedź czy trasa istnieje", settings: true);
        TestSet Stage2b = new TestSet(prototypeObject: new Lab04(), description: "Etap 2, zwrócenie trasy", settings: true);

        public override void PrepareTestSets()
        {
            TestSets["Stage1a"] = Stage1a;
            TestSets["Stage1b"] = Stage1b;
            TestSets["Stage2a"] = Stage2a;
            TestSets["Stage2b"] = Stage2b;

            prepare();
        }

        private void addStage1a(Lab04TestCase s1aTestCase)
        {
            Stage1a.TestCases.Add(s1aTestCase);
        }

        private void addStage1b(Lab04TestCase s1bTestCase)
        {
            Stage1b.TestCases.Add(s1bTestCase);
        }

        private void addStage2a(Lab04TestCase s1aTestCase)
        {
            Stage2a.TestCases.Add(s1aTestCase);
        }

        private void addStage2b(Lab04TestCase s1bTestCase)
        {
            Stage2b.TestCases.Add(s1bTestCase);
        }

        private void prepare()
        {
            int days_number;
            int start_day;
            int vertices_number;

            DiGraph<int> g1 = new DiGraph<int>(7);
            days_number = 4;
            start_day = 1;
            g1.AddEdge(0, 1, 2);
            g1.AddEdge(1, 5, 3);
            g1.AddEdge(2, 1, 1);
            g1.AddEdge(2, 3, 0);
            g1.AddEdge(3, 0, 1);
            g1.AddEdge(3, 4, 2);
            g1.AddEdge(4, 2, 3);
            g1.AddEdge(4, 5, 1);
            g1.AddEdge(6, 3, 1);
            g1.AddEdge(6, 4, 0);
            addStage1a(new Stage1TestCase(g1, startV: 6, endV: 5, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Przykład z zadania", checkR: false));
            addStage1b(new Stage1TestCase(g1, startV: 6, endV: 5, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Przykład z zadania", checkR: true));
            addStage1a(new Stage1TestCase(g1, startV: 3, endV: 4, day: start_day, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf z zadania, inne dane, brak trasy", checkR: false));
            addStage1b(new Stage1TestCase(g1, startV: 3, endV: 4, day: start_day, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf z zadania, inne dane, brak trasy", checkR: true));
            addStage2a(new Stage2TestCase(g1, startV: new int[] { 2, 3 }, endV: new int[] { 6 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf z zadania, brak trasy", checkR: false));
            addStage2b(new Stage2TestCase(g1, startV: new int[] { 2, 3 }, endV: new int[] { 6 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf z zadania, brak trasy", checkR: true));


            DiGraph<int> g2 = new DiGraph<int>(10);
            days_number = 7;
            start_day = 3;
            g2.AddEdge(0, 1, 5);
            g2.AddEdge(0, 5, 1);
            g2.AddEdge(1, 3, 6);
            g2.AddEdge(1, 5, 4);
            g2.AddEdge(2, 8, 0);
            g2.AddEdge(2, 9, 2);
            g2.AddEdge(3, 4, 0);
            g2.AddEdge(4, 2, 1);
            g2.AddEdge(5, 1, 4);
            g2.AddEdge(5, 2, 1);
            g2.AddEdge(5, 6, 3);
            g2.AddEdge(5, 9, 3);
            g2.AddEdge(6, 7, 4);
            g2.AddEdge(6, 3, 5);
            g2.AddEdge(7, 2, 0);
            g2.AddEdge(9, 0, 4);
            addStage1a(new Stage1TestCase(g2, startV: 5, endV: 2, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje jedna trasa", checkR: false));
            addStage1b(new Stage1TestCase(g2, startV: 5, endV: 2, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje jedna trasa", checkR: true));
            addStage2a(new Stage2TestCase(g2, startV: new int[] { 0, 6 }, endV: new int[] { 9, 8 }, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje jedna trasa", checkR: false));
            addStage2b(new Stage2TestCase(g2, startV: new int[] { 0, 6 }, endV: new int[] { 9, 8 }, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje jedna trasa", checkR: true));


            DiGraph<int> g3 = new DiGraph<int>(9);
            days_number = 5;
            start_day = 3;
            g3.AddEdge(0, 6, 3);
            g3.AddEdge(1, 2, 1);
            g3.AddEdge(1, 6, 0);
            g3.AddEdge(1, 7, 0);
            g3.AddEdge(2, 0, 2);
            g3.AddEdge(3, 1, 3);
            g3.AddEdge(3, 2, 1);
            g3.AddEdge(3, 5, 0);
            g3.AddEdge(4, 1, 4);
            g3.AddEdge(4, 3, 4);
            g3.AddEdge(5, 0, 1);
            g3.AddEdge(5, 1, 2);
            g3.AddEdge(6, 1, 0);
            g3.AddEdge(6, 4, 3);
            g3.AddEdge(6, 5, 4);
            g3.AddEdge(6, 8, 4);
            g3.AddEdge(7, 2, 1);

            addStage1a(new Stage1TestCase(g3, startV: 6, endV: 0, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje kilka tras", checkR: false));
            addStage1b(new Stage1TestCase(g3, startV: 6, endV: 0, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje kilka tras", checkR: true));
            addStage1a(new Stage1TestCase(g3, startV: 6, endV: 8, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje trasa, miasto startowe odwiedzane 2 razy", checkR: false));
            addStage1b(new Stage1TestCase(g3, startV: 6, endV: 8, day: start_day, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje trasa, miasto startowe odwiedzane 2 razy", checkR: true));
            addStage2a(new Stage2TestCase(g3, startV: new int[] { 4, 8 }, endV: new int[] { 6, 5 }, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje trasa", checkR: false));
            addStage2b(new Stage2TestCase(g3, startV: new int[] { 4, 8 }, endV: new int[] { 6, 5 }, n: days_number, expectedRes: true, timeLimit: 1, description: "Mały graf, istnieje trasa", checkR: true));
            addStage2a(new Stage2TestCase(g3, startV: new int[] { 1, 2 }, endV: new int[] { 3, 4 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Mały graf, nie istnieje trasa", checkR: false));
            addStage2b(new Stage2TestCase(g3, startV: new int[] { 1, 2 }, endV: new int[] { 3, 4 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Mały graf, nie istnieje trasa", checkR: true));

            vertices_number = 1000;
            days_number = 2000;
            start_day = 100;
            DiGraph<int> g4 = new DiGraph<int>(vertices_number);
            for (int i = 2; i <= vertices_number; ++i)
            {
                g4.AddEdge(vertices_number - 1, vertices_number - i, (2 * i + (start_day - 4)) % days_number);
                g4.AddEdge(vertices_number - i, vertices_number - 1, (2 * i + (start_day - 4) + 1) % days_number);
            }
            addStage1a(new Stage1TestCase(g4, startV: vertices_number - 1, endV: 0, day: start_day, n: days_number, expectedRes: true, timeLimit: 6, description: "Gwiazda, miasto docelowe w centrum, odwiedzane wiele razy", checkR: false));
            addStage1b(new Stage1TestCase(g4, startV: vertices_number - 1, endV: 0, day: start_day, n: days_number, expectedRes: true, timeLimit: 6, description: "Gwiazda, miasto docelowe w centrum, odwiedzane wiele razy", checkR: true));
            int[] startTab = new int[vertices_number / 4];
            int[] endTab = new int[vertices_number / 4];
            for (int i = 0; i < vertices_number / 4; ++i)
            {
                endTab[i] = i;
                startTab[i] = i + vertices_number / 2;
            }
            addStage2a(new Stage2TestCase(g4, startV: startTab, endV: endTab, n: days_number, expectedRes: true, timeLimit: 10, description: "Gwiazda, miasto docelowe w centrum, trasa istnieje", checkR: false));
            addStage2b(new Stage2TestCase(g4, startV: startTab, endV: endTab, n: days_number, expectedRes: true, timeLimit: 10, description: "Gwiazda, miasto docelowe w centrum, trasa istnieje", checkR: true));


            vertices_number = 200;
            days_number = 30;
            DiGraph<int> g5 = new DiGraph<int>(vertices_number);
            for (int i = 0; i < (vertices_number) / 2 - 1; ++i)
                g5.AddEdge(i, i + 1, i % days_number);
            g5.AddEdge((vertices_number) / 2 - 1, (vertices_number) / 2, 0);
            g5.AddEdge(0, vertices_number - 1, 0);
            for (int i = vertices_number - 1; i > (vertices_number) / 2; --i)
                g5.AddEdge(i, i - 1, (vertices_number - i) % days_number);
            addStage1a(new Stage1TestCase(g5, startV: 0, endV: 100, day: 0, n: days_number, expectedRes: true, timeLimit: 1, description: "Dwie sklejone końcami ścieżki, trasa istnieje", checkR: false));
            addStage1b(new Stage1TestCase(g5, startV: 0, endV: 100, day: 0, n: days_number, expectedRes: true, timeLimit: 1, description: "Dwie sklejone końcami ścieżki, trasa istnieje", checkR: true));
            addStage2a(new Stage2TestCase(g5, startV: new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 198 }, endV: new int[] { 110, 111, 123 }, n: days_number, expectedRes: true, timeLimit: 1, description: "Dwie sklejone końcami ścieżki, trasa istnieje", checkR: false));
            addStage2b(new Stage2TestCase(g5, startV: new int[] { 2, 3, 4, 5, 6, 7, 8, 9, 198 }, endV: new int[] { 110, 111, 123 }, n: days_number, expectedRes: true, timeLimit: 1, description: "Dwie sklejone końcami ścieżki, trasa istnieje", checkR: true));
            addStage2a(new Stage2TestCase(g5, startV: new int[] { 2, 4, 6, 8, 10, 12, 14, 16 }, endV: new int[] { 130, 125, 120, 115, 110 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Dwie sklejone końcami ścieżki, start na jednej, koniec na drugiej, trasa nie istnieje", checkR: false));
            addStage2b(new Stage2TestCase(g5, startV: new int[] { 2, 4, 6, 8, 10, 12, 14, 16 }, endV: new int[] { 130, 125, 120, 115, 110 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Dwie sklejone końcami ścieżki, start na jednej, koniec na drugiej, trasa nie istnieje", checkR: true));

            vertices_number = 500;
            days_number = 10;
            DiGraph<int> g6 = new DiGraph<int>(vertices_number);
            for (int i = 0; i < vertices_number / 2; ++i)
                for (int j = vertices_number / 2; j < vertices_number; ++j)
                    g6.AddEdge(i, j, (2 * i) % days_number);

            for (int j = vertices_number / 2; j < vertices_number; ++j)
                for (int i = 0; i < vertices_number / 2; ++i)
                    g6.AddEdge(j, i, ((j - 51) * 2 + 1) % days_number);

            addStage1a(new Stage1TestCase(g6, startV: 0, endV: vertices_number / 2 - 1, day: 0, n: days_number, expectedRes: true, timeLimit: 2, description: "Graf dwudzielny pełny, trasa istnieje", checkR: false));
            addStage1b(new Stage1TestCase(g6, startV: 0, endV: vertices_number / 2 - 1, day: 0, n: days_number, expectedRes: true, timeLimit: 2, description: "Graf dwudzielny pełny, trasa istnieje", checkR: true));
            addStage2a(new Stage2TestCase(g6, startV: new int[] { 250, 300, 350, 400, 450 }, endV: new int[] { 310, 311, 312 }, n: days_number, expectedRes: true, timeLimit: 2, description: "Graf dwudzielny pełny, trasa istnieje", checkR: false));
            addStage2b(new Stage2TestCase(g6, startV: new int[] { 250, 300, 350, 400, 450 }, endV: new int[] { 310, 311, 312 }, n: days_number, expectedRes: true, timeLimit: 2, description: "Graf dwudzielny pełny, trasa istnieje", checkR: true));

            // grafy losowe
            RandomGraphGenerator rgg = new RandomGraphGenerator(2022);
            vertices_number = 1000;
            days_number = 31;
            DiGraph<int> g7 = rgg.AssignWeights(rgg.DiGraph(vertices_number, 0.9), 0, days_number - 1);
            addStage1a(new Stage1TestCase(g7, startV: 789, endV: 1, day: 10, n: days_number, expectedRes: true, timeLimit: 5, description: "Graf losowy gęsty, trasa istnieje", checkR: false));
            addStage1b(new Stage1TestCase(g7, startV: 789, endV: 1, day: 10, n: days_number, expectedRes: true, timeLimit: 5, description: "Graf losowy gęsty, trasa istnieje", checkR: true));
            addStage2a(new Stage2TestCase(g7, startV: new int[] { 250, 300, 350, 400, 450 }, endV: new int[] { 310, 311, 312 }, n: days_number, expectedRes: true, timeLimit: 5, description: "Graf losowy gęsty, trasa istnieje", checkR: false));
            addStage2b(new Stage2TestCase(g7, startV: new int[] { 250, 300, 350, 400, 450 }, endV: new int[] { 310, 311, 312 }, n: days_number, expectedRes: true, timeLimit: 5, description: "Graf losowy gęsty, trasa istnieje", checkR: true));

            vertices_number = 500;
            days_number = 30;
            DiGraph<int> g8 = rgg.AssignWeights(rgg.DiGraph(vertices_number, 0.05), 0, days_number - 1);
            addStage1a(new Stage1TestCase(g8, startV: 489, endV: 1, day: 10, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf losowy rzadki, trasa nie istnieje", checkR: false));
            addStage1b(new Stage1TestCase(g8, startV: 489, endV: 1, day: 10, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf losowy rzadki, trasa nie istnieje", checkR: true));
            addStage2a(new Stage2TestCase(g8, startV: new int[] { 140, 200, 250 }, endV: new int[] { 29, 318, 400 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf losowy rzadki, trasa nie istnieje", checkR: false));
            addStage2b(new Stage2TestCase(g8, startV: new int[] { 140, 200, 250 }, endV: new int[] { 29, 318, 400 }, n: days_number, expectedRes: false, timeLimit: 1, description: "Graf losowy rzadki, trasa nie istnieje", checkR: true));

        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var tests = new Lab04Tests();
            tests.PrepareTestSets();
            foreach (var ts in tests.TestSets)
            {
                ts.Value.PerformTests(verbose: true, checkTimeLimit: false);
            }
        }
    }
}