using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Disposable
{
/// <summary>
/// Represents a composite disposable that manages multiple <see cref="IDisposable"/> resources.
/// </summary>
public interface ICompositeDisposable : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Adds a disposable resource to the composite.
	/// </summary>
	/// <param name="disposable">The disposable resource to add.</param>
	public void AddDisposable<T>(T disposable) where T : IDisposable;

	/// <summary>
	/// Adds two disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	public void AddDisposable<T>(T firstDisposable, T secondDisposable) where T : IDisposable;

	/// <summary>
	/// Adds three disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	/// <param name="thirdDisposable">The third disposable resource to add.</param>
	public void AddDisposable<T>(T firstDisposable, T secondDisposable, T thirdDisposable) where T : IDisposable;

	/// <summary>
	/// Adds a collection of disposable resources to the composite.
	/// </summary>
	/// <param name="disposables">The collection of disposables to add.</param>
	public void AddDisposable<T>(IEnumerable<T> disposables) where T : IDisposable;

	/// <summary>
	/// Adds an async disposable resource to the composite.
	/// </summary>
	/// <param name="disposable">The async disposable resource to add.</param>
	public void AddAsyncDisposable<T>(T disposable) where T : IAsyncDisposable;

	/// <summary>
	/// Adds two async disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first async disposable resource to add.</param>
	/// <param name="secondDisposable">The second async disposable resource to add.</param>
	public void AddAsyncDisposable<T>(T firstDisposable, T secondDisposable) where T : IAsyncDisposable;

	/// <summary>
	/// Adds three async disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first async disposable resource to add.</param>
	/// <param name="secondDisposable">The second async disposable resource to add.</param>
	/// <param name="thirdDisposable">The third async disposable resource to add.</param>
	public void AddAsyncDisposable<T>(
		T firstDisposable,
		T secondDisposable,
		T thirdDisposable) where T : IAsyncDisposable;

	/// <summary>
	/// Adds a collection of async disposable resources to the composite.
	/// </summary>
	/// <param name="disposables">The collection of async disposables to add.</param>
	public void AddAsyncDisposable<T>(IEnumerable<T> disposables) where T : IAsyncDisposable;

	/// <summary>
	/// Asynchronously disposes all resources with cancellation support and context configuration.
	/// </summary>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <param name="continueOnCapturedContext">Whether to continue on the captured context.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext = false);
}
}