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
	public void AddDisposable(IDisposable disposable);

	/// <summary>
	/// Adds two disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable);

	/// <summary>
	/// Adds three disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	/// <param name="thirdDisposable">The third disposable resource to add.</param>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable, IDisposable thirdDisposable);

	/// <summary>
	/// Adds a collection of disposable resources to the composite.
	/// </summary>
	/// <param name="disposables">The collection of disposables to add.</param>
	public void AddDisposable(IEnumerable<IDisposable> disposables);
	
	public void AddDisposable(IAsyncDisposable disposable);
	public void AddDisposable(IAsyncDisposable firstDisposable, IAsyncDisposable secondDisposable);
	public void AddDisposable(IAsyncDisposable firstDisposable, IAsyncDisposable secondDisposable, IAsyncDisposable thirdDisposable);
	public void AddDisposable(IEnumerable<IAsyncDisposable> disposables);
	public ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext = false);
	
	public void AddDisposable(DisposableBase disposable);
	public void AddDisposable(DisposableBase firstDisposable, DisposableBase secondDisposable);
	public void AddDisposable(DisposableBase firstDisposable, DisposableBase secondDisposable, DisposableBase thirdDisposable);
	public void AddDisposable(IEnumerable<DisposableBase> disposables);
}
}