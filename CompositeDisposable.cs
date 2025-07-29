using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Disposable
{
/// <summary>
/// Represents a composite disposable that manages multiple <see cref="IDisposable"/> resources.
/// </summary>
public class CompositeDisposable : ICompositeDisposable
{

	/// <summary>
	/// The internal list of IDisposable resources.
	/// </summary>
	private List<IDisposable> _disposables;
	private List<IAsyncDisposable> _asyncDisposables;
	private List<DisposableBase> _asyncDisposablesWithToken;
	private readonly int _disposablesCapacity;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeDisposable"/> class with an optional initial capacity.
	/// </summary>
	/// <param name="disposablesCapacity">The initial capacity for the list of disposables. Default is 15.</param>
	public CompositeDisposable(int disposablesCapacity = 15)
	{
		_disposablesCapacity = disposablesCapacity;
	}

	/// <summary>
	/// Disposes all managed <see cref="IDisposable"/> resources and clears the disposables list.
	/// </summary>
	public void Dispose()
	{
		if (_disposables != null)
		{
			foreach (var disposable in _disposables)
			{
				disposable?.Dispose();
			}
		}

		if (_asyncDisposablesWithToken != null)
		{
			foreach (var disposable in _asyncDisposablesWithToken)
			{
				disposable?.DisposeAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
			}
		}

		if (_asyncDisposables != null)
		{
			foreach (var disposable in _asyncDisposables)
			{
				disposable?.DisposeAsync().AsTask().GetAwaiter().GetResult();
			}
		}

		ClearAll();
	}
	
	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync(CancellationToken token)
	{
		if (_asyncDisposablesWithToken != null)
		{
			foreach (var disposable in _asyncDisposablesWithToken)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				await disposable.DisposeAsync(token).ConfigureAwait(false);
			}
		}

		if (_asyncDisposables != null)
		{
			foreach (var disposable in _asyncDisposables)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				await disposable.DisposeAsync().ConfigureAwait(false);
			}
		}

		if (_disposables != null)
		{
			foreach (var disposable in _disposables)
			{
				token.ThrowIfCancellationRequested();
			
				disposable?.Dispose();
			}
		}

		ClearAll();
	}
	
	public async ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext)
	{
		if (_asyncDisposablesWithToken != null)
		{
			foreach (var disposable in _asyncDisposablesWithToken)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				await disposable.DisposeAsync(token).ConfigureAwait(continueOnCapturedContext);
			}
		}

		if (_asyncDisposables != null)
		{
			foreach (var disposable in _asyncDisposables)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				await disposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext);
			}
		}

		if (_disposables != null)
		{
			foreach (var disposable in _disposables)
			{
				token.ThrowIfCancellationRequested();
			
				disposable?.Dispose();
			}
		}

		ClearAll();
	}
	
	private void ClearAll()
	{
		_disposables?.Clear();
		_asyncDisposables?.Clear();
		_asyncDisposablesWithToken?.Clear();
	}

	/// <summary>
	/// Adds a disposable resource to the composite.
	/// </summary>
	/// <param name="disposable">The disposable resource to add.</param>
	public void AddDisposable(IDisposable disposable)
	{
		_disposables ??= new List<IDisposable>();

		_disposables.Add(disposable);
	}

	/// <summary>
	/// Adds two disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable)
	{
		_disposables ??= new List<IDisposable>(_disposablesCapacity);

		_disposables.Add(firstDisposable);
		_disposables.Add(secondDisposable);
	}

	/// <summary>
	/// Adds three disposable resources to the composite.
	/// </summary>
	/// <param name="firstDisposable">The first disposable resource to add.</param>
	/// <param name="secondDisposable">The second disposable resource to add.</param>
	/// <param name="thirdDisposable">The third disposable resource to add.</param>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable, IDisposable thirdDisposable)
	{
		_disposables ??= new List<IDisposable>(_disposablesCapacity);

		_disposables.Add(firstDisposable);
		_disposables.Add(secondDisposable);
		_disposables.Add(thirdDisposable);
	}

	/// <summary>
	/// Adds a collection of disposable resources to the composite.
	/// </summary>
	/// <param name="disposables">The collection of disposables to add.</param>
	public void AddDisposable(IEnumerable<IDisposable> disposables)
	{
		_disposables ??= new List<IDisposable>(_disposablesCapacity);
		
		_disposables.AddRange(disposables);
	}

	public void AddDisposable(IAsyncDisposable disposable)
	{
		_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
		_asyncDisposables.Add(disposable);
	}

	public void AddDisposable(IAsyncDisposable firstDisposable, IAsyncDisposable secondDisposable)
	{
		_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
		_asyncDisposables.Add(firstDisposable);
		_asyncDisposables.Add(secondDisposable);
	}

	public void AddDisposable(IAsyncDisposable firstDisposable, IAsyncDisposable secondDisposable, IAsyncDisposable thirdDisposable)
	{
		_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
		_asyncDisposables.Add(firstDisposable);
		_asyncDisposables.Add(secondDisposable);
		_asyncDisposables.Add(thirdDisposable);
	}

	public void AddDisposable(IEnumerable<IAsyncDisposable> disposables)
	{
		_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);

		_asyncDisposables.AddRange(disposables);
	}

	public void AddDisposable(DisposableBase disposable)
	{
		_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
		
		_asyncDisposablesWithToken.Add(disposable);
	}

	public void AddDisposable(DisposableBase firstDisposable, DisposableBase secondDisposable)
	{
		_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
		_asyncDisposablesWithToken.Add(firstDisposable);
		_asyncDisposablesWithToken.Add(secondDisposable);
	}

	public void AddDisposable(DisposableBase firstDisposable, DisposableBase secondDisposable, DisposableBase thirdDisposable)
	{
		_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
		
		_asyncDisposablesWithToken.Add(firstDisposable);
		_asyncDisposablesWithToken.Add(secondDisposable);
		_asyncDisposablesWithToken.Add(thirdDisposable);
	}

	public void AddDisposable(IEnumerable<DisposableBase> disposables)
	{
		_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
		_asyncDisposablesWithToken.AddRange(disposables);
	}
}
}