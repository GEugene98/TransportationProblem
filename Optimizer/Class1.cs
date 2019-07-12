
using System.Collections.Generic;

namespace Optimizer
{

    /// <summary>
    /// 
    /// </summary>
    public class Transportation
    {
        /// <summary>
        /// Build First Scheme with northwestern angle method
        /// </summary>
        /// <param name="A">Array of units from 'the sender'</param>
        /// <param name="B">Array of units to 'the recipient'</param>
        /// <returns>Return matrix</returns>
        public static double?[,] GetFirstScheme(double[] A, double[] B)
        {
            int n = A.Length, m = B.Length;

            double?[,] x = new double?[n, m]; // Matrix for allocations
            bool[,] BasisZero = new bool[n, m]; // Matrix for facts of availability basis zeroes

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    if ((j != m - 1) && (A[i] != 0) && (B[j] != 0) && (A[i] == B[j])) BasisZero[i, (j + 1)] = true;

                    if (A[i] < B[j])
                    {
                        x[i, j] = A[i];
                        B[j] -= A[i];
                        A[i] = 0; // Close string
                    }
                    else
                    {
                        x[i, j] = B[j];
                        A[i] -= B[j];
                        B[j] = 0; // Close column
                    }
                }

            for (int i = 0; i < n; i++) //Deliting non-basis zeroes from the matrix
                for (int j = 0; j < m; j++)
                    if (x[i, j] == 0)
                        if (BasisZero[i, j] == true)
                            x[i, j] = 0;
                        else
                            x[i, j] = null;

            return x;
        }

        /// <summary>
        /// Calculate final cost 
        /// </summary>
        /// <param name="x">Array containing the located units</param>
        /// <param name="c">Cost matrix</param>
        /// <returns>Return final cost</returns>
        public static double CalculateCost(double?[,] x, double[,] c)
        {
            double z = 0;

            for (int i = 0; i < x.GetLength(0); i++)
                for (int j = 0; j < x.GetLength(1); j++)
                    if (x[i, j] != null) z += (double)x[i, j] * c[i, j];

            return z;
        }

        /// <summary>
        /// Calculte potentials U and V
        /// </summary>
        /// <param name="x">Array containing the located units</param>
        /// <param name="c">Cost matrix</param>
        /// <returns></returns>
        public static double?[][] GetPotentials(double?[,] x, double[,] c)
        {
            int n = x.GetLength(0), m = x.GetLength(1);

            var P = new double?[2][] { new double?[n] /*U*/ , new double?[m] /*V*/};

            P[0][0] = 0;

            bool noValue = true; int count = 0;
            startWhile: while (noValue)
            {
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                    {
                        if (x[i, j] != null && P[0][i] != null)
                            P[1][j] = c[i, j] - P[0][i];

                        if (x[i, j] != null && P[1][j] != null)
                            P[0][i] = c[i, j] - P[1][j];

                    }

                count++;

                if (count == 1000)
                    return null; 

                for (int i = 0; i < n; i++)
                    for (int j = 0; j < m; j++)
                        if (P[0][i] == null || P[1][j] == null)
                        {
                            noValue = true;
                            goto startWhile;
                        }
                        else noValue = false;

            }

            return P;
        }

        /// <summary>
        /// Calculate valuations of empty cells
        /// </summary>
        /// <param name="x">Array containing the located units</param>
        /// <param name="c">Cost matrix</param>
        /// <param name="p">Potentials</param>
        /// <returns></returns>
        public static double?[,] Evaluation(double?[,] x, double[,] c, double?[][] p)
        {
            int n = x.GetLength(0), m = x.GetLength(1);

            var d = new double?[n, m]; // Evaluation of optimality

            // var P = GetPotentials(x, c);

            ///*Split array of arrays*/
            //var u = new double[n]; // U
            //for (int i = 0; i < n; i++)
            //    u[i] = (double)P[0][i];
            //var v = new double[m]; // V 
            //for (int j = 0; j < m; j++)
            //    v[j] = (double)P[1][j];
            ///*------------------------*/

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (x[i, j] == null) 
                        d[i, j] = System.Convert.ToInt32(c[i, j] - (p[0][i] + p[1][j]));

            return d;
        }

        private static string CharForPutInColumn(int j, int n, string[,] sum)
        {
            for (int i = 0; i < n; i++)
                if (sum[i, j] == "+")
                    return "-";
                else if (sum[i, j] == "-")
                    return "+";

            return null;
        }

        private static string CharForPutInRow(int i, int m, string[,] sum)
        {
            for (int j = 0; j < m; j++)
                if (sum[i, j] == "+")
                    return "-";
                else if (sum[i, j] == "-")
                    return "+";

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="iMin"></param>
        /// <param name="jMin"></param>
        /// <returns></returns>
        public static string[,] GetLambda(double?[,] x, int iMin, int jMin)
        {
            /*---------------Put lambda---------------*/

            #region InitializationVariables
            int n = x.GetLength(0), m = x.GetLength(1);

            var lambda = new bool[n, m]; // Array forming count-loop
            lambda[iMin, jMin] = true;

            var CountStrL = new int[n]; // Count of not empty cells in string (L)
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (lambda[i, j])
                        CountStrL[i]++;

            var CountStlbL = new int[m]; // Count of not empty cells in column (L)
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (lambda[i, j])
                        CountStlbL[j]++;

            var CountStrX = new int[n]; // Count of not empty cells in string (X)
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (x[i, j] != null)
                        CountStrX[i]++;

            var CountStlbX = new int[m]; // Count of not empty cells in string (X)
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (x[i, j] != null)
                        CountStlbX[j]++;
            #endregion

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (x[i, j] != null && (CountStlbX[j] != 1 || CountStlbL[j] == 1) && (CountStrX[i] != 1 || CountStrL[i] == 1) && lambda[i, j] == false)
                        lambda[i, j] = true;

            #region ReCountLamdas
            System.Array.Clear(CountStrL, 0, CountStrL.Length);
            System.Array.Clear(CountStlbL, 0, CountStlbL.Length);

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (lambda[i, j])
                        CountStrL[i]++;

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (lambda[i, j])
                        CountStlbL[j]++;
            #endregion

            for (int i = 0; i < n; i++) // Deleting excess lamdas
                for (int j = 0; j < m; j++)
                    if ((lambda[i, j]) && (CountStrL[i] != 2) && (CountStlbL[j] != 2))
                    {
                        lambda[i, j] = false;
                        //CountStrL[i]--;
                        //CountStlbL[j]--;
                    }

            /*---------------Put symbol---------------*/

            var symbol = new string[n, m]; // + or -
            symbol[iMin, jMin] = "+";

            for (int j = 0; j < m; j++) // Put symbol - in start string
                if (lambda[iMin, j] && string.IsNullOrEmpty(symbol[iMin, j]))
                    symbol[iMin, j] = "-";

            for (int j = 0; j < m; j++) // Put symbols in columns
                for (int i = 0; i < n; i++)
                    if (lambda[i, j] && string.IsNullOrEmpty(symbol[i, j]))
                        symbol[i, j] = CharForPutInColumn(j, n, symbol);

            for (int i = 0; i < n; i++) // Put symbols in rows
                for (int j = 0; j < m; j++)
                    if (lambda[i, j] && string.IsNullOrEmpty(symbol[i, j]))
                        symbol[i, j] = CharForPutInRow(i, m, symbol);

            return symbol;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="MatrixOfLambda"></param>
        /// <param name="Units"></param>
        /// <returns></returns>
        public static double? MinOfLambda(string[,] MatrixOfLambda, double?[,] Units)
        {
            int n = MatrixOfLambda.GetLength(0), m = MatrixOfLambda.GetLength(1);

            var ListOfMin = new List<double?>();

            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                    if (MatrixOfLambda[i, j] == "-")
                        ListOfMin.Add(Units[i, j]);

            var min = ListOfMin[0];

            for (int i = 0; i < ListOfMin.Count; i++)
                if (ListOfMin[i] < min)
                    min = ListOfMin[i];

            return min;
        }


    }
}
