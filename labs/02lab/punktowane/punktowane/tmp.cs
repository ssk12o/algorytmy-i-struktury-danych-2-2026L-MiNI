using System;
using ASD;
using ASD.Graphs;
using System.Collections.Generic;

namespace ASD

{
  public class Solution{
    public (bool result, int [] route) sol(DiGraph<int> g, int startv , ind days_number){
      bool found = false; 
      int found_day = 0; 
      bool [,] visitedInDayNo = new bool[g.VertexCount, days_number];
      List<int>[,] newGraph = new List<int>[g.VertexCount, days_number  ];
    }
  }
}
