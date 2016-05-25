﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReCache
{
	internal static class ExtensionMethods
	{
		//internal static async Task<TResult> TimeoutAfter<TResult>(
		//	this Task<TResult> task,
		//	TimeSpan timeout)
		//{
		//	return await task.TimeoutAfter((int)(timeout.TotalMilliseconds)).ConfigureAwait(false);
		//}

		//// Implementation sourced from: http://blogs.msdn.com/b/pfxteam/archive/2011/11/10/10235834.aspx
		//internal static Task<TResult> TimeoutAfter<TResult>(
		//	this Task<TResult> task,
		//	int millisecondsTimeout)
		//{
		//	// Short-circuit #1: infinite timeout or task already completed
		//	if (task.IsCompleted || (millisecondsTimeout == Timeout.Infinite))
		//	{
		//		// Either the task has already completed or timeout will never occur.
		//		// No proxy necessary.
		//		return task;
		//	}

		//	// tcs.Task will be returned as a proxy to the caller
		//	TaskCompletionSource<TResult> tcs = new TaskCompletionSource<TResult>();

		//	// Short-circuit #2: zero timeout
		//	if (millisecondsTimeout == 0)
		//	{
		//		// We've already timed out.
		//		tcs.SetException(new TimeoutException());
		//		return tcs.Task;
		//	}

		//	// Set up a timer to complete after the specified timeout period
		//	Timer timer = new Timer(state =>
		//	{
		//		// Recover your state information
		//		var myTcs = (TaskCompletionSource<TResult>)state;

		//		// Fault our proxy with a TimeoutException
		//		myTcs.TrySetException(new TimeoutException());
		//	}, tcs, millisecondsTimeout, Timeout.Infinite);

		//	// Wire up the logic for what happens when source task completes
		//	task.ContinueWith((antecedent, state) =>
		//	{
		//		// Recover our state data
		//		var tuple = (Tuple<Timer, TaskCompletionSource<TResult>>)state;

		//		// Cancel the Timer
		//		tuple.Item1.Dispose();

		//		// Marshal results to proxy
		//		MarshalTaskResults(antecedent, tuple.Item2);
		//	},
		//	Tuple.Create(timer, tcs),
		//	CancellationToken.None,
		//	TaskContinuationOptions.ExecuteSynchronously,
		//	TaskScheduler.Default);

		//	return tcs.Task;
		//}

		//internal static void MarshalTaskResults<TResult>(
		//	Task source, TaskCompletionSource<TResult> proxy)
		//{
		//	switch (source.Status)
		//	{
		//		case TaskStatus.Faulted:
		//			proxy.TrySetException(source.Exception);
		//			break;
		//		case TaskStatus.Canceled:
		//			proxy.TrySetCanceled();
		//			break;
		//		case TaskStatus.RanToCompletion:
		//			Task<TResult> castedSource = source as Task<TResult>;
		//			proxy.TrySetResult(
		//				 castedSource == null ? default(TResult) : // source is a Task
		//					  castedSource.Result); // source is a Task<TResult>
		//			break;
		//	}
		//}

		//internal static async Task<TResult> _TimeoutAfter<TResult>(
		//	this Task<TResult> task,
		//	TimeSpan timeout)
		//{
		//	var timeoutCancellationTokenSource = new CancellationTokenSource();
		//	var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token)).ConfigureAwait(false);
		//	if (completedTask == task)
		//	{
		//		timeoutCancellationTokenSource.Cancel();
		//		return await task.ConfigureAwait(false);  // Very important in order to propagate exceptions
		//	}
		//	else
		//	{
		//		throw new TimeoutException("The operation has timed out.");
		//	}
		//}
	}
}
