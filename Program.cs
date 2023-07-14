using System;
using System.Diagnostics;

namespace myApp
{

    // Patron de cancelacion de tareas
    public static class TaskExetensionMethods
    {

        public static async Task<T> WithCancellation<T>(this Task<T> task, CancellationToken cancellationToken)
        {
            var taskCompletion = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(state =>
            {
                ((TaskCompletionSource<object>)state).TrySetResult(null);
            }, taskCompletion))
            {
                var taskResult = await Task.WhenAny(task, taskCompletion.Task);
                if (taskResult == taskCompletion.Task)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                return await task;
            }
        }
    }

    class Program
    {
        private static CancellationTokenSource _cancelationToken;

        static async Task Main(string[] args)
        {
            _cancelationToken = new CancellationTokenSource();
            _cancelationToken.CancelAfter(TimeSpan.FromSeconds(3));

            // try
            // {
            //     // Aplicacion de cancelacion de token.
            //     var result = await Task.Run(async () =>
            //     {
            //         await Task.Delay(5000);
            //         return 7;
            //     }).WithCancellation(_cancelationToken.Token);

            //     Console.WriteLine(result);
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine(ex.Message);
            // }
            // finally
            // {
            //     _cancelationToken.Dispose();
            // }


            var watch = new Stopwatch(); 
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




            // var task = EvaluateOne("mayodr");
            // Console.WriteLine($"Completada {task.IsCompleted}");
            // Console.WriteLine($"Cancelada {task.IsCanceled}");
            // Console.WriteLine($"Faulted {task.IsFaulted}");

            // try
            // {
            //     await task;
            // }
            // catch (Exception ex)
            // {
            //     Console.WriteLine(ex.Message);
            // }

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

            using var semaphore = new SemaphoreSlim(400);

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