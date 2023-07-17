namespace myApp
{
    public static class Arrays
    {
        public static double[,] GenArray(int rows, int cols)
        {
            Random random = new Random();

            double[,] matriz = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matriz[i, j] = random.Next(100);
                }
            }

            return matriz;
        }

        public static double[,] SumarMatricesSecuencial(double[,] matA, double[,] matB)
        {
            int matARows = matA.GetLength(0);
            int matACols = matA.GetLength(1);
            int matBRows = matB.GetLength(0);
            int matBCols = matB.GetLength(1);

            if (matARows != matBRows || matACols != matBCols)
            {
                throw new ApplicationException("Las matrices deben tener las mismas dimensiones");
            }

            double[,] result = new double[matARows, matACols];

            for (int i = 0; i < matARows; i++)
            {
                for (int j = 0; j < matBCols; j++)
                {
                    result[i, j] = matA[i, j] + matB[i, j];
                }
            }

            return result;
        }

        public static void MultiplicarMatricesSecuencial(double[,] matA, double[,] matB,
                                                double[,] result)
        {
            int matACols = matA.GetLength(1);
            int matBCols = matB.GetLength(1);
            int matARows = matA.GetLength(0);

            for (int i = 0; i < matARows; i++)
            {
                for (int j = 0; j < matBCols; j++)
                {
                    double temp = 0;
                    for (int k = 0; k < matACols; k++)
                    {
                        temp += matA[i, k] * matB[k, j];
                    }
                    result[i, j] += temp;
                }
            }
        }

        public static void MultiplicarMatricesParalelo(double[,] matA, double[,] matB,
                                        double[,] result,
                                         CancellationToken token = default(CancellationToken),
                                         int parallelGrade = -1
                                         )
        {
            int matACols = matA.GetLength(1);
            int matBCols = matB.GetLength(1);
            int matARows = matA.GetLength(0);

            try
            {
                Parallel.For(0, matARows,
                    new ParallelOptions()
                    {
                        CancellationToken = token,
                        MaxDegreeOfParallelism = parallelGrade
                    },
                    i =>
                    {
                        for (int j = 0; j < matBCols; j++)
                        {
                            double temp = 0;
                            for (int k = 0; k < matACols; k++)
                            {
                                temp += matA[i, k] * matB[k, j];
                            }
                            result[i, j] += temp;
                        }
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine("La operacion fue cancelada");
            }
        }

    }

}