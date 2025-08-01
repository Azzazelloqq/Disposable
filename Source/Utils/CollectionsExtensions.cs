using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Disposable.Utils
{
/// <summary>
/// Provides extension methods for collections of <see cref="IDisposable"/> objects.
/// </summary>
public static class CollectionsExtensions
{
	/// <summary>
	/// Disposes all objects that implement <see cref="IDisposable"/> in the list and clears the list.
	/// </summary>
	/// <typeparam name="T">The type of objects in the list, must implement <see cref="IDisposable"/>.</typeparam>
	/// <param name="disposables">A list of disposable objects.</param>
	public static void DisposeAll<T>(this List<T> disposables) where T : IDisposable
	{
		foreach (var disposable in disposables)
		{
			disposable?.Dispose();
		}

		disposables.Clear();
	}

	/// <summary>
	/// Disposes all <see cref="IDisposable"/> objects in the enumerable sequence.
	/// This method does not modify the original collection.
	/// </summary>
	/// <param name="disposables">An enumerable collection of <see cref="IDisposable"/> objects.</param>
	public static void DisposeAllItems(this IEnumerable<IDisposable> disposables)
	{
		foreach (var disposable in disposables)
		{
			disposable?.Dispose();
		}
	}

	/// <summary>
	/// Disposes all <see cref="IDisposable"/> objects in the array and resets the array to an empty array.
	/// </summary>
	/// <param name="disposables">An array of <see cref="IDisposable"/> objects.</param>
	public static void DisposeAll(this IDisposable[] disposables)
	{
		for (var i = 0; i < disposables.Length; i++)
		{
			var disposable = disposables[i];
			disposable?.Dispose();
			disposables[i] = null;
		}
	}

	/// <summary>
	/// Asynchronously disposes all <see cref="DisposableBase"/> objects in the enumerable sequence.
	/// </summary>
	/// <param name="disposables">An enumerable collection of <see cref="DisposableBase"/> objects.</param>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public static async ValueTask DisposeAllAsync(
		this IEnumerable<DisposableBase> disposables,
		CancellationToken token = default)
	{
		foreach (var disposableBase in disposables)
		{
			token.ThrowIfCancellationRequested();
			if (disposableBase is null)
			{
				continue;
			}

			await disposableBase.DisposeAsync(token).ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Asynchronously disposes all <see cref="IAsyncDisposable"/> objects in the enumerable sequence.
	/// </summary>
	/// <param name="disposables">An enumerable collection of <see cref="IAsyncDisposable"/> objects.</param>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public static async ValueTask DisposeAllAsync(
		this IEnumerable<IAsyncDisposable> disposables,
		CancellationToken token = default)
	{
		foreach (var disposable in disposables)
		{
			token.ThrowIfCancellationRequested();
			if (disposable is null)
			{
				continue;
			}

			await disposable.DisposeAsync().ConfigureAwait(false);
		}
	}

	/// <summary>
	/// Asynchronously disposes all <see cref="IAsyncDisposable"/> objects in the array and nulls the elements.
	/// </summary>
	/// <param name="disposables">An array of <see cref="IAsyncDisposable"/> objects.</param>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public static async ValueTask DisposeAllAsync(
		this IAsyncDisposable[] disposables,
		CancellationToken token = default)
	{
		for (var i = 0; i < disposables.Length; i++)
		{
			token.ThrowIfCancellationRequested();
			var d = disposables[i];
			if (d is not null)
			{
				await d.DisposeAsync().ConfigureAwait(false);
				disposables[i] = null;
			}
		}
	}
}
}