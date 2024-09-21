using System;
using System.Collections.Generic;

namespace Disposable
{
/// <summary>
/// Represents a composite disposable that manages multiple <see cref="IDisposable"/> resources.
/// </summary>
public interface ICompositeDisposable : IDisposable
{
	/// <summary>
	/// Gets the list of disposables managed by this composite disposable.
	/// </summary>
	internal List<IDisposable> Disposables { get; }

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
}
}