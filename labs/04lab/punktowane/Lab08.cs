using ASD.Graphs;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace ASD2
{
    public class TreasureTrackers : MarshalByRefObject
    {
        bool partyCanExitAndEnter(int day, DiGraph map, int startChamber, int endChamber, int[] durability,
            int[] opensOn,
            int expeditionSize)
        {
            if (day < opensOn[startChamber] || day < opensOn[endChamber])
            {
                return false;
            }

            if (durability[startChamber] < expeditionSize || durability[endChamber] < expeditionSize)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Etap I: Wybór dnia ekspedycji.
        /// Wyznaczenie pierwszego dnia, w którym cała ekspedycja będzie w stanie
        /// przejść przez podziemia.
        /// </summary>
        /// <param name="map">Graf skierowany reprezentujący połączenia pomiędzy komnatami w podziemiach.</param>
        /// <param name="startChamber">Wierzchołek będący wejściem do podziemi.</param>
        /// <param name="endChamber">Wierzchołek będący wyjściem z podziemi.</param>
        /// <param name="durability">Tablica utrzymująca wytrzymałość każdej komnaty.</param>
        /// <param name="opensOn">Tablica informująca, którego dnia otwiera się dana komnata.</param>
        /// <param name="expeditionSize">Rozmiar ekspedycji, chcącej przejść przez podziemia.</param>
        public int? Stage1(DiGraph map, int startChamber, int endChamber, int[] durability, int[] opensOn,
            int expeditionSize)
        {
            // return null;
            int[] sortedUniqueOpensOn = (int[])opensOn.Clone();
            Array.Sort(sortedUniqueOpensOn);
            sortedUniqueOpensOn = sortedUniqueOpensOn.Distinct().ToArray();

            int fromDayIndex = 0;
            int toDayIndex = sortedUniqueOpensOn.Length - 1;

            // while (fromDayIndex <= toDayIndex && !partyCanExitAndEnter(sortedUniqueOpensOn[fromDayIndex], map,
            //            startChamber, endChamber, durability, opensOn, expeditionSize))
            // {
            //     fromDayIndex++;
            // }

            if (!partyCanExitAndEnter(sortedUniqueOpensOn[toDayIndex], map,
                    startChamber, endChamber, durability, opensOn, expeditionSize))
            {
                return null;
            }
            
            int numOfVetreciesInGraph = map.VertexCount;

            Dictionary<int, List<int>> dayList = new Dictionary<int, List<int>>();
            // List<int>[] dayList = new List<int>[sortedUniqueOpensOn.Length];
            for (int dayId = 0; dayId < sortedUniqueOpensOn.Length; dayId++)
            {
                dayList[sortedUniqueOpensOn[dayId]] = new List<int>();
            }

            for (int v = 0; v < numOfVetreciesInGraph; v++)
            {
                dayList[opensOn[v]].Add(v);
            }

            if (!testIfCurrentDayAllows(toDayIndex, map, durability, expeditionSize, startChamber, endChamber, dayList, sortedUniqueOpensOn)) return null;

            int left = 0;
            int right = sortedUniqueOpensOn.Length - 1;
            int? bestDay = null;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                if (testIfCurrentDayAllows(mid, map, durability, expeditionSize, startChamber, endChamber, dayList,
                        sortedUniqueOpensOn))
                {
                    bestDay = sortedUniqueOpensOn[mid];
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return bestDay;

        }
        
        bool testIfCurrentDayAllows(int currentDayAbsolute, DiGraph map, int [] durability, int expeditionSize, int startChamber, int endChamber, Dictionary<int, List<int>> dayList, int [] sortedUniqueOpensOn)
        {
            int numOfVetreciesInGraph = map.VertexCount;
            DiGraph<int> testGraph = new  DiGraph<int>(2 * numOfVetreciesInGraph);
            
            for (int dayAdd = 0; dayAdd <= currentDayAbsolute; dayAdd++)
            {
                foreach (int i in dayList[sortedUniqueOpensOn[dayAdd]])
                {
                    testGraph.AddEdge(i, i + numOfVetreciesInGraph,Math.Min( durability[i], expeditionSize));
                    foreach (var neighbor in map.OutNeighbors(i))
                    {
                        testGraph.AddEdge(i + numOfVetreciesInGraph, neighbor, expeditionSize);
                    }
                }
            }

            var (maxFlowForCurrentDay, _) = Flows.FordFulkerson(testGraph, startChamber, endChamber + numOfVetreciesInGraph);
            return maxFlowForCurrentDay >= expeditionSize;
        }

        /// <summary>
        /// Etap II: 
        /// Wyznaczenie minimalnej liczby poszukiwaczy skarbów,
        /// która będzie w stanie zebrać wszystkie skarby.
        /// </summary>
        /// <param name="map">Acykliczny graf skierowany reprezentujący połączenia pomiędzy komnatami w podziemiach.</param>
        /// <param name="startChamber">Wierzchołek będący wejściem do podziemi.</param>
        /// <param name="endChamber">Wierzchołek będący wyjściem z podziemi.</param>
        /// <param name="durability">Tablica utrzymująca wytrzymałość każdej komnaty.</param>
        public int? Stage2(DiGraph map, int startChamber, int endChamber, int[] durability)
        {
            int maxExpeditionSize = GetMaxPossibleFlowInDungeon(map, durability, startChamber, endChamber);
            if (maxExpeditionSize == 0) return null;
            
            int numberOfVerticies = map.VertexCount;
            
            NetworkWithCosts<int, int> networkGraph = new NetworkWithCosts<int, int>(3 * map.VertexCount + 1);
            for (int v = 0; v < numberOfVerticies; v++)
            {
                networkGraph.AddEdge(v, v + 2 * numberOfVerticies, 1, -1);
                networkGraph.AddEdge(v + 2 * numberOfVerticies, v + numberOfVerticies, 1, 0);

                if (durability[v] > 1)
                {
                    networkGraph.AddEdge(v, v + numberOfVerticies, durability[v] - 1,0);
                }

                foreach (int neighbor in map.OutNeighbors(v))
                {
                    networkGraph.AddEdge(v+numberOfVerticies, neighbor, maxExpeditionSize, 0);
                }
            }

            if (!TestIfThereAreEnoughPartyMembers(maxExpeditionSize, startChamber, endChamber, networkGraph,
                    numberOfVerticies)) return null;
            
            int left = 1;
            int right = maxExpeditionSize;
            int best = maxExpeditionSize;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;

                if (TestIfThereAreEnoughPartyMembers(mid, startChamber, endChamber, networkGraph,
                        numberOfVerticies))
                {
                    best = mid;
                    right = mid - 1;
                }
                else
                {
                    left = mid + 1;
                }
            }

            return best;
        }
        int GetMaxPossibleFlowInDungeon(DiGraph gT, int[] durability, int startChamber, int endChamber)
        {
            int numOfVetreciesInGraph = gT.VertexCount;
            DiGraph<int> testGraph = new DiGraph<int>(2 * numOfVetreciesInGraph);
            for (int i = 0; i < numOfVetreciesInGraph; i++)
            {
                testGraph.AddEdge(i, i + numOfVetreciesInGraph, durability[i]);
                foreach (var neighbor in gT.OutNeighbors(i)) 
                {
                    testGraph.AddEdge(i + numOfVetreciesInGraph, neighbor, Int32.MaxValue);
                }
            }
            var (maxFlowForCurrentDay, _) = Flows.FordFulkerson(testGraph, startChamber, endChamber + numOfVetreciesInGraph);
            return maxFlowForCurrentDay;
        }
        bool TestIfThereAreEnoughPartyMembers(int expeditionSizee, int startChamber, int endChamber, NetworkWithCosts<int, int> networkGraph, int numberOfVerticies)
        {
            networkGraph.AddEdge(3 * numberOfVerticies, startChamber, expeditionSizee, 0);
            var (cap, cost, diGraph) = Flows.MinCostMaxFlow(networkGraph, 3 * numberOfVerticies, endChamber + numberOfVerticies);
            networkGraph.RemoveEdge(3 * numberOfVerticies, startChamber);
            return ((-1) * cost == numberOfVerticies);
        }
    }
}