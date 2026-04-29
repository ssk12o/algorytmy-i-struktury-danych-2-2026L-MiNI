using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ASD
{
    public class Lab02 : MarshalByRefObject
    {
       int[] CalculateSumsOfZ(int[] inputZTab)
        {
            int[] calculated = new int[inputZTab.Length];
            calculated[0] = inputZTab[0];
            for (int i = 1; i < inputZTab.Length; i++)
            {
                calculated[i] = calculated[i - 1] + inputZTab[i];
            }

            return calculated;
        }

        /// <summary>
        /// Optymalne rozmieszczenie parasolek w wariancie, w którym każda parasolka ma taki sam promień
        /// oraz mamy do dyspozycji tylko zadaną liczbę parasolek (rozmieszczenie parasolek nie wiąże się z żadnym kosztem)
        /// </summary>
        /// <param name="Z">Tablica zysków, Z[i] to zysk za pokrycie punktu o numerze i</param>
        /// <param name="umbrellaCount">Liczba dostępnych parasolek</param>
        /// <param name="umbrellaRadius">Promień parasolki (parasolka o promieniu r umieszczona w punkcie i pokrywa punkty i-r, i-r+1, ..., i+r)</param>
        /// <returns></returns>
        public (int profit, int[] umbrellaPosition) Stage1(int[] Z, int umbrellaCount, int umbrellaRadius)
        {
            if (Z.Length <= 0 || umbrellaCount <= 0)
            {
                return (0, new int[]{});
            }
            
            int[,] dp = new int [Z.Length, umbrellaCount];
            int[,] umbrellaRecoveryPos = new int [Z.Length, umbrellaCount];

            int[] tableOfSumsOfZ = CalculateSumsOfZ(Z);
        
            for (int j = 0; j < Z.Length; j++)
            {
                for (int d = 0; d < umbrellaCount; d++)
                {
                    if (j <= (d + 1) * (2 * umbrellaRadius + 1) - 1) 
                    {
                        dp[j, d] = tableOfSumsOfZ[j];
                        umbrellaRecoveryPos[j, d] = - (d + 1);
                    }
                    else
                    {

                        int c1 = tableOfSumsOfZ[j] - tableOfSumsOfZ[j - 2 * umbrellaRadius - 1];
                        if (d > 0) c1 += dp[j - 2 * umbrellaRadius - 1, d - 1];

                        int c2 = dp[j - 1, d];
                        if (c1 > c2)
                        {
                            dp[j, d] = c1;
                            umbrellaRecoveryPos[j, d] = d + 1;
                        }
                        else
                        {
                            dp[j, d] = c2;
                        }
                    }
                }
            }

            List<int> umbrellaPositions = new List<int>();
            int jP = Z.Length - 1, dP = umbrellaCount - 1;
            while (jP >= 0 && dP >= 0)
            {
                if (umbrellaRecoveryPos[jP, dP] == 0)
                {
                    jP--;
                }
                else if (umbrellaRecoveryPos[jP, dP] > 0)
                {
                    umbrellaPositions.Add(jP - umbrellaRadius);
                    dP--;
                    jP -= 2 * umbrellaRadius + 1;
                }
                else if (umbrellaRecoveryPos[jP, dP] < 0)
                {
                    while (dP >= 0)
                    {
                        umbrellaPositions.Add(Math.Abs(jP - umbrellaRadius));
                        jP -= 2 * umbrellaRadius + 1;
                        dP--;
                    }
                }
            }
            
            return (dp[Z.Length-1, umbrellaCount-1], umbrellaPositions.ToArray());
        }

        /// <summary>
        /// Optymalne rozmieszczenie parasolek w wariancie, w którym mamy dostępne modele parasolek o różnych promieniach.
        /// Każdego modelu możemy użyć dowolną liczbę razy, jednak za każdym razem musimy ponieść jego koszt.
        /// </summary>
        /// <param name="Z">Tablica zysków, Z[i] to zysk za pokrycie punktu o numerze i</param>
        /// <param name="umbrellaType">Tablice dostępnych modeli parasolek, gdzie i-ty model ma promień umbrellaType[i].radius i koszt umbrellaType[i].cost</param>
        /// <returns></returns>
        public (int profit, (int position, int model)[] umbrellas) Stage2(int[] Z, (int radius, int cost)[] umbrellaType)
        {
            if (Z.Length <= 0 || umbrellaType.Length <= 0)
            {
                return (0, new (int position, int model)[]{});
            }
            
            int[] tableOfSumsOfZ = CalculateSumsOfZ(Z);

            int[] dpMaxProfit = new int [Z.Length];
            int[] dpPrex = new int [Z.Length];

            for (int i = 0; i < Z.Length; i++)
            {
                dpPrex[i] = -1;
                // dpMaxProfit[i] = Int32.MinValue;
                for(int d = 0; d < umbrellaType.Length; d++)
                {
                    (int r, int cost) = umbrellaType[d];
                    int c1;
                    if (i <= 2 * r)
                    {
                        c1 = tableOfSumsOfZ[i] - cost;
                    }
                    else
                    {
                        c1 = tableOfSumsOfZ[i] - tableOfSumsOfZ[i - 2 * r - 1] - cost;
                        c1 += dpMaxProfit[i - 2 * r - 1];
                    }

                    int c2 = Math.Max(dpMaxProfit[Math.Max(i - 1, 0)], dpMaxProfit[i]);
                    if (c1 > c2)
                    {
                        dpMaxProfit[i] = c1;
                        dpPrex[i] = d;
                    }
                    else
                    {
                        dpMaxProfit[i] = c2;
                    }

                }
            }

            int posRecovery = Z.Length - 1;
            List<(int, int)> recoveryUmbrellas = new List<(int, int)>();
            while (posRecovery >= 0)
            {
                if (dpPrex[posRecovery] == -1)
                {
                    posRecovery--;
                    continue;
                }
                else
                {
                    int r = umbrellaType[dpPrex[posRecovery]].radius;
                    recoveryUmbrellas.Add((Math.Max(posRecovery - r, 0), dpPrex[posRecovery]));
                    posRecovery -= 2 * r + 1;
                }
            }
            
            return (dpMaxProfit[Z.Length-1], recoveryUmbrellas.ToArray());
        }
    }
}
