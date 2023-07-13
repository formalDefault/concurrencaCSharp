using System;
using System.Diagnostics;

namespace myApp
{
    class Program
    {
        private static CancellationTokenSource _cancelationToken;

        static async Task Main(string[] args)
        {
            _cancelationToken = new CancellationTokenSource();
            var watch = new Stopwatch();
            _cancelationToken.CancelAfter(TimeSpan.FromSeconds(5));

            watch.Start();

            try
            {
                List<double> results = await GenerateNumbers(1000, _cancelationToken.Token);
                await Procesator(results, _cancelationToken.Token);
                Console.WriteLine($"Tareas completadas en {watch.ElapsedMilliseconds / 1000.0}");
            }
            catch (TaskCanceledException ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        static async Task<List<double>> GenerateNumbers(int amount, CancellationToken cancellation = default)
        {
            return await Task.Run(async () =>
            {
                List<double> list = new List<double>();

                for (int i = 0; i < amount; i++)
                {
                    CheckCancellation(cancellation);
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

            using var semaphore = new SemaphoreSlim(400);

            tasks = results.Select(async number =>
            {
                try
                {
                    CheckCancellation(cancellationToken);
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
            if(cancellationToken.IsCancellationRequested) 
            {
                throw new TaskCanceledException("El proceso fue cancelado");
            }
        }

    }
}