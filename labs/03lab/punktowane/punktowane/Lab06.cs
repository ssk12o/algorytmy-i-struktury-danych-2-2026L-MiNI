using System;
using ASD.Graphs;
using ASD;
using System.Collections.Generic;

namespace ASD2
{
    public class CoinFlow : MarshalByRefObject
    {
        /// <summary>
        /// Etap I: Arbitraż.
        /// Celem jest wykrycie, czy na rynku istnieje możliwość darmowego wzbogacenia się, 
        /// czyli znalezienie cyklu, dla którego iloczyn kursów wymiany jest ściśle większy niż 1.
        /// </summary>
        /// <param name="G">Ważony graf skierowany, gdzie waga krawędzi w(u,v) to mnożnik kapitału.</param>
        /// <returns>Wartość true, jeśli istnieje cykl o iloczynie wag > 1, w przeciwnym razie false.</returns>
        public bool Stage1(DiGraph<double> G)
        {
            DiGraph<double> g = new DiGraph<double>(G.VertexCount, G.Representation);
            for (int v = 0; v < G.VertexCount; v++)
            {
                foreach (Edge<double> edge in G.OutEdges(v))
                {
                    g.AddEdge(edge.From, edge.To, -Math.Log10(edge.Weight));
                }
            }
            int[] negativeCycle = Paths.NegativeCycle(g);

            if (negativeCycle == null)
                return false;
            return true;
        }

        /// <summary>
        /// Etap II: Ograniczone ryzyko.
        /// Wyznaczenie najlepszej możliwej ścieżki wymiany między walutą startNode i endNode,
        /// przy zachowaniu limitu k "słabych" walut na ścieżce (waluty startowa i końcowa również się wliczają).
        /// Zakładamy, że rynek jest stabilny - brak cykli o iloczynie > 1.
        /// </summary>
        /// <param name="G">Graf dostępnych kursów wymiany.</param>
        /// <param name="isStrong">Tablica informująca, czy dana waluta jest "mocna" (false oznacza walutę "słabą").</param>
        /// <param name="k">Maksymalna dopuszczalna liczba słabych walut na ścieżce.</param>
        /// <param name="startNode">Indeks waluty początkowej.</param>
        /// <param name="endNode">Indeks waluty docelowej.</param>
        /// <returns>Najlepsza ścieżka wymiany (ciąg indeksów) lub null, jeśli ścieżka spełniająca warunki nie istnieje.</returns>
        public int[] Stage2(DiGraph<double> G, bool[] isStrong, int k, int startNode, int endNode)
        {
            int numberOfvertInOriginalGraph = G.VertexCount;

            int getGIDfromVandLayer(int vk, int layerK)
            {
                return vk + layerK * numberOfvertInOriginalGraph;
            }

            int startingLayer = 0;
            if (!isStrong[startNode]) startingLayer = 1;
            if (k == 0 && startingLayer == 1) return null;
            
            DiGraph<double> g = new DiGraph<double>(G.VertexCount * (k + 1), G.Representation);
            for (int v = 0; v < numberOfvertInOriginalGraph; v++)
            {
                foreach (Edge<double> edge in G.OutEdges(v))
                {
                    for (int layer = startingLayer; layer <= k; layer++)
                    {
                        double weight = - Math.Log10(edge.Weight);
                        if (!isStrong[edge.To] )
                        {
                            if(layer < k) g.AddEdge(getGIDfromVandLayer(v, layer), getGIDfromVandLayer(edge.To, layer + 1), weight);
                        }
                        else g.AddEdge(getGIDfromVandLayer(v, layer), getGIDfromVandLayer(edge.To, layer), weight);
                    }
                }
            }

            int[] minimalPath = null;
            double minimalPathVal = double.PositiveInfinity;

            PathsInfo<double> pathInfo = Paths.BellmanFord(g, getGIDfromVandLayer(startNode, startingLayer));
            for (int layer = startingLayer; layer <= k; layer++)
            {
                if (pathInfo.Reachable(getGIDfromVandLayer(startNode ,startingLayer), getGIDfromVandLayer(endNode, layer)))
                {
                    if (pathInfo.GetDistance(getGIDfromVandLayer(startNode, startingLayer), getGIDfromVandLayer(endNode, layer)) <
                        minimalPathVal)
                    {
                        minimalPath = pathInfo.GetPath(getGIDfromVandLayer(startNode, startingLayer), getGIDfromVandLayer(endNode, layer));
                        minimalPathVal = pathInfo.GetDistance(getGIDfromVandLayer(startNode, startingLayer),
                            getGIDfromVandLayer(endNode, layer));
                    }
                }
            }

            if (minimalPath == null)
            {
                if(startNode == endNode) return new int[] {startNode};
                return null;
            }
            
            
            for (int i = 0; i < minimalPath.Length; i++)
            {
                minimalPath[i] %= numberOfvertInOriginalGraph;
            }
            return minimalPath;
        }

        // public int[] Stage2(DiGraph<double> G, bool[] isStrong, int k, int startNode, int endNode)
        // {
        //     int numberOfVertecies = G.VertexCount;
        //
        //     int GetIDfromVertexNumAndK(int v, int kV)
        //     {
        //         return v + kV * numberOfVertecies;
        //     }
        //
        //     int startingLayer = 0;
        //     if (!isStrong[startNode]) startingLayer = 1;
        //     if (k == 0 && !isStrong[startNode]) return null;
        //     
        //     DiGraph<double> g = new DiGraph<double>(G.VertexCount * (k + 1), G.Representation);
        //     for (int v = 0; v < G.VertexCount; v++)
        //     {
        //         foreach (Edge<double> edge in G.OutEdges(v))
        //         { 
        //             for(int kLayer = 0; kLayer < k + 1; kLayer++)
        //             {
        //                 double weight = -Math.Log10(edge.Weight);
        //                 if (!isStrong[edge.To] && kLayer < k)
        //                 {
        //                     g.AddEdge(GetIDfromVertexNumAndK(edge.From, kLayer), GetIDfromVertexNumAndK(edge.To, kLayer + 1), weight);
        //                 }
        //                 else
        //                 {
        //                     g.AddEdge(GetIDfromVertexNumAndK(edge.From, kLayer), GetIDfromVertexNumAndK(edge.To, kLayer), weight);
        //                 }
        //             }
        //         }
        //     }
        //
        //     int[]? minimalPath = null;
        //     double minimalPathVal = double.MaxValue;
        //     
        //     PathsInfo<double> path = Paths.BellmanFord(g, startNode + numberOfVertecies * startingLayer);
        //     for (int i = 0; i <= k; i++)
        //     {
        //         if(path!= null &&  path.GetDistance(startNode + numberOfVertecies * startingLayer, GetIDfromVertexNumAndK(endNode, i)) < minimalPathVal)
        //         {
        //             minimalPathVal = path.GetDistance(startNode + numberOfVertecies * startingLayer, GetIDfromVertexNumAndK(endNode, i));
        //             minimalPath = path.GetPath(startNode + numberOfVertecies * startingLayer, GetIDfromVertexNumAndK(endNode, i));
        //         }
        //     }
        //
        //     for (int i = 0; i < minimalPath.Length; i++)
        //     {
        //         minimalPath[i] %= numberOfVertecies;
        //     }
        //     return minimalPath;
        // }

        /// <summary>
        /// Etap III: Diamentowa Droga.
        /// Znalezienie ścieżki maksymalizującej końcową ilość złota uzyskaną z jednego diamentu.
        /// Proces obejmuje: wymianę diamentu na walutę początkową, dowolną liczbę wymian rynkowych 
        /// oraz końcową wymianę waluty na złoto.
        /// </summary>
        /// <param name="G">Graf dostępnych kursów wymiany walut.</param>
        /// <param name="diamondToCurrency">Lista ofert wymiany diamentu na konkretną walutę (indeks waluty, kurs).</param>
        /// <param name="currencyToGold">Lista ofert wymiany waluty na złoto (indeks waluty, kurs).</param>
        /// <returns>Ciąg indeksów walut (od pierwszej zakupionej za diament do ostatniej sprzedanej za złoto) prowadzący do najlepszego wyniku.</returns>
        public int[] Stage3(DiGraph<double> G, (int currencyIdx, double rate)[] diamondToCurrency, (int currencyIdx, double goldRate)[] currencyToGold)
        {
            // znalezione w sieci -- nie mam lepszego pomysłu jak to zrobić XD
            // bo trzeba pozbyć się dorysowanych wierzchołków -- pierwszego i ostatniego na liście
            int[] TrimEnds(int[] path)
            {
                int[] result = new int[path.Length - 2];
                Array.Copy(path, 1, result, 0, result.Length);
                return result;
            }
            int numberOfvertInOriginalGraph = G.VertexCount;
            
            DiGraph<double> g = new DiGraph<double>(G.VertexCount + 2, G.Representation);
            for (int v = 0; v < numberOfvertInOriginalGraph; v++)
            {
                foreach (Edge<double> edge in G.OutEdges(v))
                {
                    double weight = - Math.Log10(edge.Weight);
                    g.AddEdge(v, edge.To, weight);
                }
            }

            foreach (var DTC in diamondToCurrency)
            {
                g.AddEdge(numberOfvertInOriginalGraph,  DTC.currencyIdx, - Math.Log10( DTC.rate));
            }
            foreach (var DTC in currencyToGold)
            {
                g.AddEdge(DTC.currencyIdx, numberOfvertInOriginalGraph+1, - Math.Log10( DTC.goldRate));
            }

            PathsInfo<double> pathInfo = Paths.BellmanFord(g, numberOfvertInOriginalGraph);
            if (pathInfo.Reachable(numberOfvertInOriginalGraph, numberOfvertInOriginalGraph + 1))
            {
                int [] paht = pathInfo.GetPath(numberOfvertInOriginalGraph, numberOfvertInOriginalGraph + 1);
                return TrimEnds(paht);
            }

            return null;
        }
    }
}