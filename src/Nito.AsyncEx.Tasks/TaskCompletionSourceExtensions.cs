﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx.Synchronous;

namespace Nito.AsyncEx
{
    /// <summary>
    /// Provides extension methods for <see cref="TaskCompletionSource{TResult}"/>.
    /// </summary>
    public static class TaskCompletionSourceExtensions
    {
        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/>, propagating the completion of <paramref name="task"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the target asynchronous operation.</typeparam>
        /// <typeparam name="TSourceResult">The type of the result of the source asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <returns><c>true</c> if this method completed the task completion source; <c>false</c> if it was already completed.</returns>
        public static bool TryCompleteFromCompletedTask<TResult, TSourceResult>(this TaskCompletionSource<TResult> @this, Task<TSourceResult> task) where TSourceResult : TResult
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (task.IsFaulted)
                return @this.TrySetException(task.Exception.InnerExceptions);
            if (task.IsCanceled)
            { 
                try
                {
                    task.WaitAndUnwrapException();
                }
                catch (OperationCanceledException exception)
                {
                    var token = exception.CancellationToken;
                    return token.IsCancellationRequested ? @this.TrySetCanceled(token) : @this.TrySetCanceled();
                }
            }
            return @this.TrySetResult(task.Result);
        }

        /// <summary>
        /// Attempts to complete a <see cref="TaskCompletionSource{TResult}"/>, propagating the completion of <paramref name="task"/> but using the result value from <paramref name="resultFunc"/> if the task completed successfully.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the target asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <param name="resultFunc">A delegate that returns the result with which to complete the task completion source, if the task completed successfully. May not be <c>null</c>.</param>
        /// <returns><c>true</c> if this method completed the task completion source; <c>false</c> if it was already completed.</returns>
        public static bool TryCompleteFromCompletedTask<TResult>(this TaskCompletionSource<TResult> @this, Task task, Func<TResult> resultFunc)
        {
            if (@this == null)
                throw new ArgumentNullException(nameof(@this));
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            if (resultFunc == null)
                throw new ArgumentNullException(nameof(resultFunc));

            if (task.IsFaulted)
                return @this.TrySetException(task.Exception.InnerExceptions);
            if (task.IsCanceled)
            {
                try
                {
                    task.WaitAndUnwrapException();
                }
                catch (OperationCanceledException exception)
                {
                    var token = exception.CancellationToken;
                    return token.IsCancellationRequested ? @this.TrySetCanceled(token) : @this.TrySetCanceled();
                }
            }
            return @this.TrySetResult(resultFunc());
        }

        /// <summary>
        /// Asynchronously waits until <paramref name="task"/> is complete, and then attempts to complete this <see cref="TaskCompletionSource{TResult}"/>, propagating the completion of <paramref name="task"/>.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the target asynchronous operation.</typeparam>
        /// <typeparam name="TSourceResult">The type of the result of the source asynchronous operation.</typeparam>
        /// <param name="this">The task completion source. May not be <c>null</c>.</param>
        /// <param name="task">The task. May not be <c>null</c>.</param>
        /// <param name="context">The context in which to execute the continuation. May be <c>null</c>.</param>
        /// <returns><c>true</c> if this method completed the task completion source; <c>false</c> if it was already completed.</returns>
        public static Task<bool> LinkCompletionFromTask<TResult, TSourceResult>(this TaskCompletionSource<TResult> @this, Task<TSourceResult> task, SynchronizationContext context = null) where TSourceResult : TResult
        {
            return SynchronizationContextSwitcher.ApplyContext(context, async () =>
            {
                bool result;
                try
                {
                    await task.ConfigureAwait(false);
                }
                finally
                {
                    result = TryCompleteFromCompletedTask(@this, task);
                }
                return result;
            });
        }

        /// <summary>
        /// Creates a new TCS for use with async code, and which forces its continuations to execute asynchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the TCS.</typeparam>
        public static TaskCompletionSource<TResult> CreateAsyncTaskSource<TResult>()
        {
            return new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}
