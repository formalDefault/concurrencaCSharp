using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace myapp
{ 

    public static class TaskExtensionMethods
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
}