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
	/// Disposes all <see cref="IDisposable"/> objects in the enumerable sequence.
	/// </summary>
	/// <param name="disposables">An enumerable collection of <see cref="IDisposable"/> objects.</param>
	public static void DisposeAll(this IEnumerable<IDisposable> disposables)
	{
		foreach (var disposable in disposables)
		{
			disposable?.Dispose();
		}
	}

	/// <summary>
	/// Disposes all <see cref="IDisposable"/> objects in the list and clears the list.
	/// </summary>
	/// <param name="disposables">A list of <see cref="IDisposable"/> objects.</param>
	public static void DisposeAll(this List<IDisposable> disposables)
	{
		foreach (var disposable in disposables)
		{
			disposable?.Dispose();
		}

		disposables.Clear();
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

	public static async ValueTask DisposeAllAsync(
		this IEnumerable<IAsyncDisposable> disposables,
		CancellationToken token = default)
	{
		foreach (var d in disposables)
		{
			token.ThrowIfCancellationRequested();
			if (d is null)
			{
				continue;
			}

			await d.DisposeAsync().ConfigureAwait(false);
		}
	}

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