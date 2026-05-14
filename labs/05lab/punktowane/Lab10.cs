
using ASD.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASD
{
    public class Lab10 : MarshalByRefObject
    {
        /// <summary>
        /// Etap 1: Szukanie najmniejszego zbioru wierzchołków S, takiego że G - S jest lasem.
        /// </summary>
        /// <param name="G">Graf nieskierowany; wierzchołki = skrzyżowania, krawędzie = rury</param>
        /// <param name="maxBudget">Górne ograniczenie na rozmiar szukanego rozwiązania</param>
        /// <param name="S">Najmniejsza tablica wierzchołków S taka, że G - S jest lasem</param>
        /// <returns>Rozmiar najmniejszego zbioru S</returns>
        public int Stage1(Graph G, int maxBudget, out int[] S)
        {
            int n = G.VertexCount;
            List<int>[] adjacencyList = new List<int>[n];

            for (int i = 0; i < n; i++)
            {
                adjacencyList[i] = new List<int>();
                foreach (int to in G.OutNeighbors(i))
                {
                    adjacencyList[i].Add(to);
                }
            }

            int budgetLimit = Math.Min(maxBudget, n);

            for (int k = 0; k <= budgetLimit; k++)
            {
                bool[] activeVertexes = new bool[n];
                for (int i = 0; i < n; i++)
                {
                    activeVertexes[i] = true;
                }

                bool[] forbiddenVertexes = new bool[n];
                List<int> currentSetS = new List<int>();

                if (checkIfExistSolutionWithK(k, activeVertexes, forbiddenVertexes, currentSetS, adjacencyList, n))
                {
                    S = currentSetS.ToArray();
                    return S.Length;
                }
            }

            S = null;
            return -1;
        }

        private bool checkIfExistSolutionWithK(int k, bool[] activeVertexes, bool[] forbiddenVertexes,
            List<int> currentSetS, List<int>[] adjacencyList, int n)
        {
            int[] degrees = new int[n];
            bool[] in_core = new bool[n];
            List<int> leavesList = new List<int>();

            for (int i = 0; i < n; i++)
            {
                if (activeVertexes[i])
                {
                    in_core[i] = true;
                    int iDegree = 0;
                    foreach (int neighboor in adjacencyList[i])
                    {
                        if (activeVertexes[neighboor])
                        {
                            iDegree++;
                        }
                    }

                    degrees[i] = iDegree;
                    if (iDegree <= 1) leavesList.Add(i);
                }
            }

            foreach (int leaf in leavesList)
            {
                recRemoveLeavs(leaf, in_core, degrees, adjacencyList);
            }

            bool[] visitedVerticies = new bool[n];
            List<int> pickedVertices = new List<int>();
            int componentsWithDegree3OrMore = 0;

            for (int i = 0; i < n; i++)
            {
                if (in_core[i] && !visitedVerticies[i])
                {
                    List<int> component = new List<int>();
                    int maxDegreeInComponent = 0;

                    recExploreComponent(i, in_core, visitedVerticies, component, degrees, ref maxDegreeInComponent,
                        adjacencyList);

                    if (maxDegreeInComponent <= 2)
                    {
                        int pick = -1;
                        foreach (int v in component)
                        {
                            if (!forbiddenVertexes[v])
                            {
                                pick = v;
                                break;
                            }
                        }

                        if (pick == -1) return false;
                        pickedVertices.Add(pick);

                        foreach (int v in component)
                        {
                            in_core[v] = false;
                        }
                    }
                    else
                    {
                        componentsWithDegree3OrMore++;
                    }
                }
            }

            if (k < pickedVertices.Count) return false;

            foreach (int pick in pickedVertices)
            {
                currentSetS.Add(pick);
            }

            int remainingK = k - pickedVertices.Count;

            if (componentsWithDegree3OrMore == 0) return true;

            if (remainingK == 0)
            {
                currentSetS.RemoveRange(currentSetS.Count - pickedVertices.Count, pickedVertices.Count);
                return false;
            }

            int minimalCycleLenght = int.MaxValue;
            int bestU = -1;
            int bestV = -1;

            int[] depth = new int[n];
            int[] parent = new int[n];

            for (int i = 0; i < n; i++)
            {
                depth[i] = -1;
                parent[i] = -1;
            }

            for (int i = 0; i < n; i++)
            {
                if (in_core[i] && depth[i] == -1)
                {
                    recFindCycle(i, -1, 0, in_core, depth, parent, ref minimalCycleLenght, ref bestU, ref bestV,
                        adjacencyList);
                }
            }

            List<int> bestCycle = null;
            if (minimalCycleLenght < int.MaxValue)
            {
                bestCycle = new List<int>();
                int curr = bestU;
                while (curr != bestV)
                {
                    bestCycle.Add(curr);
                    curr = parent[curr];
                }

                bestCycle.Add(bestV);
            }

            if (bestCycle == null) return true;

            List<int> branch = new List<int>();
            foreach (int v in bestCycle)
            {
                if (degrees[v] >= 3 && !forbiddenVertexes[v])
                {
                    branch.Add(v);
                }
            }

            if (branch.Count == 0)
            {
                currentSetS.RemoveRange(currentSetS.Count - pickedVertices.Count, pickedVertices.Count);
                return false;
            }

            branch.Sort((a, b) => degrees[b].CompareTo(degrees[a]));

            foreach (int v in branch)
            {
                activeVertexes[v] = false;
                currentSetS.Add(v);

                if (checkIfExistSolutionWithK(remainingK - 1, activeVertexes, forbiddenVertexes, currentSetS,
                        adjacencyList, n))
                {
                    return true;
                }

                currentSetS.RemoveAt(currentSetS.Count - 1);
                activeVertexes[v] = true;
                forbiddenVertexes[v] = true;
            }

            foreach (int v in branch)
            {
                forbiddenVertexes[v] = false;
            }

            currentSetS.RemoveRange(currentSetS.Count - pickedVertices.Count, pickedVertices.Count);
            return false;
        }

        private void recRemoveLeavs(int u, bool[] in_core, int[] degrees, List<int>[] adjList)
        {
            if (!in_core[u]) return;

            in_core[u] = false;
            foreach (int neighboor in adjList[u])
            {
                if (in_core[neighboor])
                {
                    degrees[neighboor]--;
                    if (degrees[neighboor] == 1)
                    {
                        recRemoveLeavs(neighboor, in_core, degrees, adjList);
                    }
                }
            }
        }

        private void recExploreComponent(int u, bool[] in_core, bool[] visited, List<int> component, int[] degrees,
            ref int maxDegree, List<int>[] adj)
        {
            visited[u] = true;
            component.Add(u);
            if (degrees[u] > maxDegree)
            {
                maxDegree = degrees[u];
            }

            foreach (int neighboor in adj[u])
            {
                if (in_core[neighboor] && !visited[neighboor])
                {
                    recExploreComponent(neighboor, in_core, visited, component, degrees, ref maxDegree, adj);
                }
            }
        }

        private void recFindCycle(int u, int p, int currentDepth, bool[] in_core, int[] depth, int[] parent,
            ref int minCycleLength, ref int bestU, ref int bestV, List<int>[] adj)
        {
            depth[u] = currentDepth;
            foreach (int neighboor in adj[u])
            {
                if (!in_core[neighboor] || neighboor == p) continue;

                if (depth[neighboor] != -1)
                {
                    if (depth[u] > depth[neighboor])
                    {
                        int len = depth[u] - depth[neighboor] + 1;
                        if (len < minCycleLength)
                        {
                            minCycleLength = len;
                            bestU = u;
                            bestV = neighboor;
                        }
                    }
                }
                else
                {
                    parent[neighboor] = u;
                    recFindCycle(neighboor, u, currentDepth + 1, in_core, depth, parent, ref minCycleLength, ref bestU,
                        ref bestV, adj);
                }
            }
        }

        /// <summary>
        /// Etap 2: Szukanie zbioru wierzchołków S, takiego że G - S jest lasem, o minimalnym łącznym koszcie.
        /// </summary>
        /// <param name="G">Graf nieskierowany; wierzchołki = skrzyżowania, krawędzie = rury</param>
        /// <param name="cost">Koszt montażu zaworu w każdym wierzchołku (cost[v] >= 0)</param>
        /// <param name="maxBudget">Górne ograniczenie kosztu szukanego rozwiązania</param>
        /// <param name="S">Tablica wierzchołków S o minimalnym łącznym koszcie, taka że G - S jest lasem</param>
        /// <returns>Suma kosztów montażu zaworów w wierzchołkach z S</returns>
        
        public int Stage2(Graph G, int[] cost, int maxBudget, out int[] S)
        {
            int n = G.VertexCount;
            List<int>[] adjacencyList = new List<int>[n];

            for (int i = 0; i < n; i++)
            {
                adjacencyList[i] = new List<int>();
                foreach (int to in G.OutNeighbors(i))
                {
                    adjacencyList[i].Add(to);
                }
            }

            int bestCost = maxBudget + 1;
            int[] bestSetS = null;

            bool[] active = new bool[n];
            for (int i = 0; i < n; i++) active[i] = true;
            bool[] forbidden = new bool[n];
            List<int> currentSetS = new List<int>();

            SolveStage2(0, currentSetS, active, forbidden, adjacencyList, n, ref bestCost, cost, ref bestSetS);

            if (bestSetS != null)
            {
                S = bestSetS;
                return bestCost;
            }

            S = null;
            return -1;
        }

        private void SolveStage2(int currentCost, List<int> currentSetS, bool[] activeVertexes, bool[] forbidden,
            List<int>[] adjacencyList, int n, ref int bestCost, int [] cost, ref int [] bestSetS)
        {
            if (currentCost >= bestCost) return;

            int[] degrees = new int[n];
            bool[] in_core = new bool[n];
            List<int> leavesList = new List<int>();

            for (int i = 0; i < n; i++)
            {
                if (activeVertexes[i])
                {
                    in_core[i] = true;
                    int iDegree = 0;
                    foreach (int neighboor in adjacencyList[i])
                    {
                        if (activeVertexes[neighboor])
                        {
                            iDegree++;
                        }
                    }

                    degrees[i] = iDegree;
                    if (iDegree <= 1) leavesList.Add(i);
                }
            }

            foreach (int leaf in leavesList)
            {
                if (in_core[leaf])
                {
                    recRemoveLeavs(leaf, in_core, degrees, adjacencyList);
                }
            }

            bool[] visitedVerticies = new bool[n];
            int greedyCosts = 0;
            List<int> greedyPicks = new List<int>();
            List<int> greedyComponentsVerticies = new List<int>();
            int componentsWithDegree3OrMore = 0;

            for (int i = 0; i < n; i++)
            {
                if (in_core[i] && !visitedVerticies[i])
                {
                    List<int> component = new List<int>();
                    int maxDegreeInComponent = 0;

                    recExploreComponent(i, in_core, visitedVerticies, component, degrees, ref maxDegreeInComponent, adjacencyList);
                    
                    if (maxDegreeInComponent <= 2)
                    {
                        int minCost = int.MaxValue;
                        int vv = -1;
                        foreach (int v in component)
                        {
                            if (!forbidden[v] && cost[v] < minCost)
                            {
                                minCost = cost[v];
                                vv = v;
                            }
                        }

                        if (vv == -1) return;

                        greedyCosts += minCost;
                        greedyPicks.Add(vv);
                        greedyComponentsVerticies.AddRange(component);
                    }
                    else
                    {
                        componentsWithDegree3OrMore++;
                    }
                }
            }

            if (currentCost + greedyCosts >= bestCost) return;

            foreach (int pick in greedyPicks)
            {
                currentSetS.Add(pick);
            }
            currentCost += greedyCosts;

            foreach (int v in greedyComponentsVerticies)
            {
                in_core[v] = false;
            }

            if (componentsWithDegree3OrMore == 0)
            {
                bestCost = currentCost;
                bestSetS = currentSetS.ToArray();
                currentSetS.RemoveRange(currentSetS.Count - greedyPicks.Count, greedyPicks.Count);
                return;
            }

            int minimalCycleLenght = int.MaxValue;
            int bestU = -1;
            int bestV = -1;

            int[] depth = new int[n];
            int[] parent = new int[n];
            for (int i = 0; i < n; i++)
            {
                depth[i] = -1;
                parent[i] = -1;
            }

            for (int i = 0; i < n; i++)
            {
                if (in_core[i] && depth[i] == -1)
                {
                    recFindCycle(i, -1, 0, in_core, depth, parent, ref minimalCycleLenght, ref bestU, ref bestV,
                        adjacencyList);
                }
            }

            List<int> bestCycle = null;
            if (minimalCycleLenght != int.MaxValue)
            {
                bestCycle = new List<int>();
                int current = bestU;
                while (current != bestV)
                {
                    bestCycle.Add(current);
                    current = parent[current];
                }

                bestCycle.Add(bestV);
            }

            if (bestCycle != null)
            {
                List<int> branches = new List<int>();
                foreach (int v in bestCycle)
                {
                    if (!forbidden[v])
                    {
                        branches.Add(v);
                    }
                }

                branches.Sort((a, b) => cost[a].CompareTo(cost[b]));

                List<int> branchedAndForbidden = new List<int>();

                foreach (int v in branches)
                {
                    if (currentCost + cost[v] >= bestCost) break;

                    activeVertexes[v] = false;
                    currentSetS.Add(v);

                    SolveStage2(currentCost + cost[v], currentSetS, activeVertexes, forbidden, adjacencyList, n, ref bestCost, cost, ref bestSetS);

                    currentSetS.RemoveAt(currentSetS.Count - 1);
                    activeVertexes[v] = true;

                    forbidden[v] = true;
                    branchedAndForbidden.Add(v);
                }

                foreach (int v in branchedAndForbidden)
                {
                    forbidden[v] = false;
                }
            }

            currentSetS.RemoveRange(currentSetS.Count - greedyPicks.Count, greedyPicks.Count);
        }
        // hope it works
    }
}