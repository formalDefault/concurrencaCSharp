using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using myapp;

namespace myApp
{

    class Program
    {
        private static CancellationTokenSource _cancelationToken;

        static async Task Main(string[] args)
        {
            var cancelAfter = TimeSpan.FromSeconds(3);
            _cancelationToken = new CancellationTokenSource(cancelAfter);

            var colsArrayA = 1100;
            var rows= 1000;
            var colsArrayB = 1750;

            var arrayA = Arrays.GenArray(rows, colsArrayA);
            var arrayB = Arrays.GenArray(colsArrayA, colsArrayA);
            var result = new double[rows, colsArrayB];

            var watch = new Stopwatch();

            watch.Start();

            await Task.Run(() => {
                Arrays.MultiplicarMatricesSecuencial(arrayA, arrayB, result);
            });

            var secuentialTime = watch.ElapsedMilliseconds / 1000.0;

            Console.WriteLine($"Tiempo secuencial {secuentialTime}");

            result = new double[rows, colsArrayB];

            watch.Restart();
            

            await Task.Run(() => {
                Arrays.MultiplicarMatricesParalelo(arrayA, arrayB, result);
            });

            var parallelTime = watch.ElapsedMilliseconds / 1000.0;

            Console.WriteLine($"Tiempo paralelo {parallelTime}");

        }

        // Implementacion de IAsyncEnumerable para generar un iterable asincrono 
        static async Task ProcessNames(IAsyncEnumerable<string> names, CancellationToken cancellationToken = default(CancellationToken))
        {
            await foreach (var name in names.WithCancellation(cancellationToken))
            {
                await Task.Delay(2000, cancellationToken);
                Console.WriteLine($"Nombre: {name}");
            }

        }


        static async IAsyncEnumerable<string> GenerateNames(
            [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken))
        {
            yield return "name1";
            yield return "name2";
            await Task.Delay(2000, cancellationToken);
            yield return "name3";
            yield return "name4";
        }

        static async Task<T> Retry<T>(Func<Task<T>> func, int callbacks = 3, int timeout = 500)
        {
            for (int i = 0; i < callbacks - 1; i++)
            {
                try
                {
                    return await func();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await Task.Delay(timeout);
                }
            }

            return await func();
        }


        static async Task<List<double>> GenerateNumbers(int amount, CancellationToken cancellation = default)
        {

            return await Task.Run(async () =>
            {
                List<double> list = new List<double>();

                for (int i = 0; i < amount; i++)
                {
                    await Task.Delay(TimeSpan.FromMicroseconds(new Random().Next(1000)));

                    double number = new Random().Next(2000);

                    list.Add(number);
                }

                return list;
            });
        }

        static async Task<(double number, string result)> CheckNumber(double res)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(new Random().Next(2500)));

            string result = res < 10 ? "MENOR" : "mayor";

            return (res, result);
        }

        static async Task Procesator(List<double> results, CancellationToken cancellationToken = default(CancellationToken))
        {
            var tasks = new List<Task<(double number, string result)>>();

            using var semaphore = new SemaphoreSlim(1000);

            tasks = results.Select(async number =>
            {
                try
                {
                    await semaphore.WaitAsync();
                    return await CheckNumber(number);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            var numberResponse = Task.WhenAll(tasks);

            while (await Task.WhenAny(numberResponse, Task.Delay(1000)) != numberResponse)
            {
                var taskCompleted = tasks.Where(x => x.IsCompleted).Count();

                int percent = (taskCompleted * 100) / tasks.Count;

                Console.WriteLine($"%{percent}, {taskCompleted} tareas completadas");
            }
        }

        static void CheckCancellation(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException("El proceso fue cancelado");
            }
        }

        static Task EvaluateOne(string value)
        {
            var taskCompletion = new TaskCompletionSource<object>
                (TaskCreationOptions.RunContinuationsAsynchronously);

            if (value == "MENOR")
            {
                taskCompletion.SetResult(null);
            }
            else if (value == "mayor")
            {
                taskCompletion.SetCanceled();
            }
            else
            {
                taskCompletion.SetException(new ApplicationException("Valor invalido"));
            }

            return taskCompletion.Task;
        }



    }
}