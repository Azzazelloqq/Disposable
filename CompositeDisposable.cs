using System;
using System.Collections.Generic;

namespace Disposable
{
/// <summary>
/// Represents a composite disposable that manages multiple <see cref="IDisposable"/> resources.
/// </summary>
public class CompositeDisposable : ICompositeDisposable
{
	/// <summary>
	/// Gets the list of disposables managed by this composite disposable.
	/// This is an explicit interface implementation and is accessible only through the <see cref="ICompositeDisposable"/> interface.
	/// </summary>
	List<IDisposable> ICompositeDisposable.Disposables => _disposables;

	/// <summary>
	/// The internal list of IDisposable resources.
	/// </summary>
	private readonly List<IDisposable> _disposables;

	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeDisposable"/> class with an optional initial capacity.
	/// </summary>
	/// <param name="disposablesCapacity">The initial capacity for the list of disposables. Default is 15.</param>
	public CompositeDisposable(int disposablesCapacity = 15)
	{
		_disposables = new List<IDisposable>(disposablesCapacity);
	}

	/// <summary>
	/// Disposes all managed <see cref="IDisposable"/> resources and clears the disposables list.
	/// </summary>
	public void Dispose()
	{
		foreach (var disposable in _disposables)
		{
			disposable.Dispose();
		}

		_disposables.Clear();
	}

	/// <summary>
	/// Adds a disposable resource to the composite.
	/// </summary>
	/// <param name="disposable">The disposable resource to add.</param>
	public void AddDisposable(IDisposable disposable)
	{
		if (!_disposables.Contains(disposable))
		{
			_disposables.Add(disposable);
		}
	}

	/// <summary>
	/// Adds two disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable)
	{
		if (!_disposables.Contains(firstDisposable))
		{
			_disposables.Add(firstDisposable);
		}

		if (!_disposables.Contains(secondDisposable))
		{
			_disposables.Add(secondDisposable);
		}
	}

	/// <summary>
	/// Adds three disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	/// <param name="thirdDisposable">The third disposable resource to add.</param>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable, IDisposable thirdDisposable)
	{
		if (!_disposables.Contains(firstDisposable))
		{
			_disposables.Add(firstDisposable);
		}

		if (!_disposables.Contains(secondDisposable))
		{
			_disposables.Add(secondDisposable);
		}

		if (!_disposables.Contains(thirdDisposable))
		{
			_disposables.Add(thirdDisposable);
		}
	}

	/// <summary>
	/// Adds a collection of disposable resources to the composite.
	/// </summary>
	/// <param name="disposables">The collection of disposables to add.</param>
	public void AddDisposable(IEnumerable<IDisposable> disposables)
	{
		foreach (var disposable in disposables)
		{
			if (!_disposables.Contains(disposable))
			{
				_disposables.Add(disposable);
			}
		}
	}
}
}