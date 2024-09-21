using System;
using System.Collections.Generic;

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

		disposables = Array.Empty<IDisposable>();
	}
}
}