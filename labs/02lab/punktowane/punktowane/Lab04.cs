using System;
using ASD.Graphs;
using ASD;
using System.Collections.Generic;

namespace ASD
{
    public class Lab04 : System.MarshalByRefObject
    {
        /// <summary>
        /// Etap 1 - szukanie trasy z miasta start_v do miasta end_v, startując w dniu day
        /// </summary>
        /// <param name="g">Ważony graf skierowany będący mapą</param>
        /// <param name="start_v">Indeks wierzchołka odpowiadającego miastu startowemu</param>
        /// <param name="end_v">Indeks wierzchołka odpowiadającego miastu docelowemu</param>
        /// <param name="day">Dzień startu (w tym dniu należy wyruszyć z miasta startowego)</param>
        /// <param name="days_number">Liczba dni uwzględnionych w rozkładzie (tzn. wagi krawędzi są z przedziału [0, days_number-1])</param>
        /// <returns>(result, route) - result ma wartość true gdy podróż jest możliwa, wpp. false, 
        /// route to tablica z indeksami kolejno odwiedzanych miast (pierwszy indeks to indeks miasta startowego, ostatni to indeks miasta docelowego),
        /// jeżeli result == false to route ustawiamy na null</returns>
        
        public (bool result, int[] route) Lab04_FindRoute(DiGraph<int> g, int start_v, int end_v, int day,
            int days_number)
        {
            bool found = false;
            int foundDay = 0;
            
            bool[,] visitedInDayNo = new bool[g.VertexCount, days_number];
            List<int>[,] newGraph = new List<int>[g.VertexCount, days_number];

            for (int i = 0; i < g.VertexCount; i++)
            {
                foreach (Edge<int> e in g.OutEdges(i))
                {
                    if(newGraph[e.From, e.Weight] == null) newGraph[e.From, e.Weight] = new List<int>();
                    newGraph[e.From, e.Weight].Add(e.To);
                }   
            }
            
            visitedInDayNo[start_v, day] = true;

            int[,] prev = new int [g.VertexCount, days_number];

            Queue<(int qver, int qday)> queue = new Queue<(int, int)>();
            queue.Enqueue((start_v, day));

            while (queue.Count > 0)
            {
                (int vertex, int currentDay) = queue.Dequeue();

                if (vertex == end_v)
                {
                    found = true;
                    foundDay = currentDay;
                    break;
                }

                int nextDay = (currentDay + 1) % days_number;
                if (newGraph[vertex, currentDay] == null) continue;
                foreach (int eTo in newGraph[vertex, currentDay])
                {
                    if (!visitedInDayNo[eTo, nextDay])
                    {
                        queue.Enqueue((eTo, nextDay));
                        prev[eTo, nextDay] = vertex;
                        visitedInDayNo[eTo, nextDay] = true;
                    }
                }
            }

            if (!found)
            {
                return (false, null);
            }

            int traveller = end_v;

            List<int> path = new List<int>();
            while (true)
            {
                path.Add(traveller);

                if (traveller == start_v && foundDay == day) break;
                traveller = prev[traveller, foundDay];
                if (foundDay == 0) foundDay = days_number - 1;
                else foundDay = (foundDay - 1) % days_number;
            }


            path.Reverse();
            return (found, path.ToArray());
        }

        // public (bool result, int[] route) Lab04_FindRoute(DiGraph<int> g, int start_v, int end_v, int day,
        //     int days_number)
        // {
        //     bool found = false;
        //     int foundDay = 0;
        //     bool[,] visitedInDayNo = new bool[g.VertexCount, days_number];
        //     visitedInDayNo[start_v, day] = true;
        //
        //     int[,] prev = new int [g.VertexCount, days_number];
        //
        //     Queue<(int qver, int qday)> queue = new Queue<(int, int)>();
        //     queue.Enqueue((start_v, day));
        //
        //     while (queue.Count > 0)
        //     {
        //         (int vertex, int currentDay) = queue.Dequeue();
        //
        //         if (vertex == end_v)
        //         {
        //             found = true;
        //             foundDay = currentDay;
        //             break;
        //         }
        //
        //         int nextDay = (currentDay + 1) % days_number;
        //
        //         foreach (Edge<int> e in g.OutEdges(vertex))
        //         {
        //             if (e.Weight == currentDay && visitedInDayNo[e.To, nextDay] == false)
        //             {
        //                 queue.Enqueue((e.To, nextDay));
        //                 prev[e.To, nextDay] = e.From;
        //                 visitedInDayNo[e.To, nextDay] = true;
        //             }
        //         }
        //     }
        //
        //     if (!found)
        //     {
        //         return (false, null);
        //     }
        //
        //     int traveller = end_v;
        //
        //     List<int> path = new List<int>();
        //     while (true)
        //     {
        //         path.Add(traveller);
        //
        //         if (traveller == start_v && foundDay == day) break;
        //         traveller = prev[traveller, foundDay];
        //         if (foundDay == 0) foundDay = days_number - 1;
        //         else foundDay = (foundDay - 1) % days_number;
        //     }
        //
        //
        //     path.Reverse();
        //     return (found, path.ToArray());
        // }

        // public (bool result, int[] route) Lab04_FindRoute(DiGraph<int> g, int start_v, int end_v, int day, int days_number)
        // {
        //     int currentDay = day;
        //     bool[,] canBeAchivedInDayNo = new bool[g.VertexCount, days_number]; 
        //     Stack<(int station, int day)> kolej = new Stack<(int, int)>();
        //     // canBeAchivedInDayNo[start_v, day] = true;
        //     if (day - 1 < 0)
        //     {
        //         kolej.Push((start_v, days_number - 1));
        //     }
        //     else
        //     {
        //         kolej.Push((start_v, day - 1));
        //     }
        //     
        //     while( kolej.Count > 0)
        //     {
        //         (int curStation, int curDay) = kolej.Extract();
        //         if(canBeAchivedInDayNo[curStation, curDay] == true) continue;
        //         else canBeAchivedInDayNo[curStation, curDay] = true;
        //         
        //         curDay = (curDay + 1) % days_number;
        //         foreach (var v in g.OutEdges(curStation))
        //         {
        //             if (!canBeAchivedInDayNo[v.To, curDay] && v.Weight == curDay)
        //             {
        //                 kolej.Push((v.To, curDay));
        //             }
        //         }
        //     }
        //
        //     for (int i = 0; i < days_number; i++)
        //     {
        //         if (canBeAchivedInDayNo[end_v, i] == true)
        //             return (true, null);
        //     }
        //     
        //     
        //     
        //     return (false, null);
        // }

        /// <summary>
        /// Etap 2 - szukanie trasy z jednego z miast z tablicy start_v do jednego z miast z tablicy end_v (startować można w dowolnym dniu)
        /// </summary>
        /// <param name="g">Ważony graf skierowany będący mapą</param>
        /// <param name="start_v">Tablica z indeksami wierzchołków startowych (trasę trzeba zacząć w jednym z nich)</param>
        /// <param name="end_v">Tablica z indeksami wierzchołków docelowych (trasę trzeba zakończyć w jednym z nich)</param>
        /// <param name="days_number">Liczba dni uwzględnionych w rozkładzie (tzn. wagi krawędzi są z przedziału [0, days_number-1])</param>
        /// <returns>(result, route) - result ma wartość true gdy podróż jest możliwa, wpp. false, 
        /// route to tablica z indeksami kolejno odwiedzanych miast (pierwszy indeks to indeks miasta startowego, ostatni to indeks miasta docelowego),
        /// jeżeli result == false to route ustawiamy na null</returns>
        public (bool result, int[] route) Lab04_FindRouteSets(DiGraph<int> g, int[] start_v, int[] end_v,
            int days_number)
        {
            bool found = false;
            int foundDay = 0;
            
            bool[,] visitedInDayNo = new bool[g.VertexCount, days_number];
            List<int>[,] newGraph = new List<int>[g.VertexCount, days_number];

            for (int i = 0; i < g.VertexCount; i++)
            {
                foreach (Edge<int> e in g.OutEdges(i))
                {
                    if(newGraph[e.From, e.Weight] == null) newGraph[e.From, e.Weight] = new List<int>();
                    newGraph[e.From, e.Weight].Add(e.To);
                }   
            }
            bool []targetCity = new bool[g.VertexCount];
            foreach (int city in end_v)
            {
                targetCity[city] = true;
            }
            Queue<(int qver, int qday)> queue = new Queue<(int, int)>();
            
            int[,] prev  = new int[g.VertexCount, days_number];

            foreach (int  startingCity in start_v)
            {
                for (int day = 0; day < days_number; day++)
                {
                    visitedInDayNo[startingCity, day] = true;
                    queue.Enqueue((startingCity, day));
                    prev[startingCity, day] = -1;
                    
                }
            }

            int traveller = -1;

            while (queue.Count > 0)
            {
                (int vertex, int currentDay) = queue.Dequeue();

                if (targetCity[vertex])
                {
                    found = true;
                    foundDay = currentDay;
                    traveller = vertex;
                    break;
                }

                int nextDay = (currentDay + 1) % days_number;
                if (newGraph[vertex, currentDay] == null) continue;
                foreach (int eTo in newGraph[vertex, currentDay])
                {
                    if (!visitedInDayNo[eTo, nextDay])
                    {
                        queue.Enqueue((eTo, nextDay));
                        prev[eTo, nextDay] = vertex;
                        visitedInDayNo[eTo, nextDay] = true;
                    }
                }
            }

            if (!found)
            {
                return (false, null);
            }


            List<int> path = new List<int>();
            while (traveller >= 0)
            {
                path.Add(traveller);
                traveller = prev[traveller, foundDay];
                if (foundDay == 0) foundDay = days_number - 1;
                else foundDay = (foundDay - 1) % days_number;
            }


            path.Reverse();
            return (found, path.ToArray());
        }
    }
}
