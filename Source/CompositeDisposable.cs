using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Disposable
{
/// <inheritdoc/>
public class CompositeDisposable : ICompositeDisposable
{

	/// <summary>
	/// The internal list of IDisposable resources.
	/// </summary>
	private List<IDisposable> _disposables;
	
	/// <summary>
	/// The internal list of IAsyncDisposable resources.
	/// </summary>
	private List<IAsyncDisposable> _asyncDisposables;
	
	/// <summary>
	/// The internal list of DisposableBase resources.
	/// </summary>
	private List<DisposableBase> _asyncDisposablesWithToken;
	private readonly int _disposablesCapacity;
	
	/// <summary>
	/// Synchronization object for thread-safe operations.
	/// </summary>
	private readonly object _lock = new object();
	
	/// <summary>
	/// Initializes a new instance of the <see cref="CompositeDisposable"/> class with an optional initial capacity.
	/// </summary>
	/// <param name="disposablesCapacity">The initial capacity for the list of disposables. Default is 15.</param>
	public CompositeDisposable(int disposablesCapacity = 15)
	{
		_disposablesCapacity = disposablesCapacity;
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		Exception firstException = null;
		
		// Copy collections inside lock to avoid blocking disposal
		List<IDisposable> disposables;
		List<DisposableBase> universalDisposables;
		List<IAsyncDisposable> asyncDisposables;
		
		lock (_lock)
		{
			disposables = _disposables;
			universalDisposables = _asyncDisposablesWithToken;
			asyncDisposables = _asyncDisposables;
			
			// Clear references
			_disposables = null;
			_asyncDisposablesWithToken = null;
			_asyncDisposables = null;
		}

		if (disposables != null)
		{
			foreach (var disposable in disposables)
			{
				try
				{
					disposable?.Dispose();
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (universalDisposables != null )
		{
			foreach (var disposable in universalDisposables)
			{
				try
				{
					disposable?.Dispose();
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (asyncDisposables != null)
		{
			if (asyncDisposables.Count > 0)
			{
				Logger.LogError(new Exception($"Have async disposables. Invoke async dispose with lock thread. Maybe need invoke {DisposeAsync()}."));
			}

			foreach (var asyncDisposable in asyncDisposables)
			{
				asyncDisposable?.DisposeAsync();
			}
		}

		if (firstException != null)
		{
			throw firstException;
		}
	}
	
	/// <inheritdoc/>
	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
	}

	/// <summary>
	/// Asynchronously disposes all resources with cancellation support.
	/// </summary>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public async ValueTask DisposeAsync(CancellationToken token)
	{
		Exception firstException = null;
		
		// Copy collections inside lock to avoid blocking disposal
		List<IDisposable> disposables;
		List<DisposableBase> asyncDisposablesWithToken;
		List<IAsyncDisposable> asyncDisposables;
		
		lock (_lock)
		{
			disposables = _disposables;
			asyncDisposablesWithToken = _asyncDisposablesWithToken;
			asyncDisposables = _asyncDisposables;
			
			// Clear references
			_disposables = null;
			_asyncDisposablesWithToken = null;
			_asyncDisposables = null;
		}

		if (asyncDisposablesWithToken != null)
		{
			foreach (var disposable in asyncDisposablesWithToken)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				try
				{
					await disposable.DisposeAsync(token).ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (asyncDisposables != null)
		{
			foreach (var disposable in asyncDisposables)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				try
				{
					await disposable.DisposeAsync().ConfigureAwait(false);
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (disposables != null)
		{
			foreach (var disposable in disposables)
			{
				token.ThrowIfCancellationRequested();
			
				try
				{
					disposable?.Dispose();
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (firstException != null)
		{
			throw firstException;
		}
	}
	
	/// <inheritdoc/>
	public async ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext)
	{
		Exception firstException = null;
		
		// Copy collections inside lock to avoid blocking disposal
		List<IDisposable> disposables;
		List<DisposableBase> asyncDisposablesWithToken;
		List<IAsyncDisposable> asyncDisposables;
		
		lock (_lock)
		{
			disposables = _disposables;
			asyncDisposablesWithToken = _asyncDisposablesWithToken;
			asyncDisposables = _asyncDisposables;
			
			// Clear references
			_disposables = null;
			_asyncDisposablesWithToken = null;
			_asyncDisposables = null;
		}

		if (asyncDisposablesWithToken != null)
		{
			foreach (var disposable in asyncDisposablesWithToken)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				try
				{
					await disposable.DisposeAsync(token).ConfigureAwait(continueOnCapturedContext);
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (asyncDisposables != null)
		{
			foreach (var disposable in asyncDisposables)
			{
				token.ThrowIfCancellationRequested();
				if (disposable is null)
				{
					continue;
				}

				try
				{
					await disposable.DisposeAsync().ConfigureAwait(continueOnCapturedContext);
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (disposables != null)
		{
			foreach (var disposable in disposables)
			{
				token.ThrowIfCancellationRequested();
			
				try
				{
					disposable?.Dispose();
				}
				catch (Exception ex)
				{
					firstException ??= ex;
				}
			}
		}

		if (firstException != null)
		{
			throw firstException;
		}
	}


	/// <inheritdoc/>
	public void AddDisposable(IDisposable disposable)
	{
		lock (_lock)
		{
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.Add(disposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable)
	{
		lock (_lock)
		{
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.Add(firstDisposable);
			_disposables.Add(secondDisposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IDisposable firstDisposable, IDisposable secondDisposable, IDisposable thirdDisposable)
	{
		lock (_lock)
		{
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.Add(firstDisposable);
			_disposables.Add(secondDisposable);
			_disposables.Add(thirdDisposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IEnumerable<IDisposable> disposables)
	{
		lock (_lock)
		{
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.AddRange(disposables);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IAsyncDisposable disposable)
	{
		lock (_lock)
		{
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.Add(disposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IAsyncDisposable firstDisposable, IAsyncDisposable secondDisposable)
	{
		lock (_lock)
		{
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.Add(firstDisposable);
			_asyncDisposables.Add(secondDisposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IAsyncDisposable firstDisposable, IAsyncDisposable secondDisposable, IAsyncDisposable thirdDisposable)
	{
		lock (_lock)
		{
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.Add(firstDisposable);
			_asyncDisposables.Add(secondDisposable);
			_asyncDisposables.Add(thirdDisposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IEnumerable<IAsyncDisposable> disposables)
	{
		lock (_lock)
		{
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.AddRange(disposables);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(DisposableBase disposable)
	{
		lock (_lock)
		{
			_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
			_asyncDisposablesWithToken.Add(disposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(DisposableBase firstDisposable, DisposableBase secondDisposable)
	{
		lock (_lock)
		{
			_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
			_asyncDisposablesWithToken.Add(firstDisposable);
			_asyncDisposablesWithToken.Add(secondDisposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(DisposableBase firstDisposable, DisposableBase secondDisposable, DisposableBase thirdDisposable)
	{
		lock (_lock)
		{
			_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
			_asyncDisposablesWithToken.Add(firstDisposable);
			_asyncDisposablesWithToken.Add(secondDisposable);
			_asyncDisposablesWithToken.Add(thirdDisposable);
		}
	}

	/// <inheritdoc/>
	public void AddDisposable(IEnumerable<DisposableBase> disposables)
	{
		lock (_lock)
		{
			_asyncDisposablesWithToken ??= new List<DisposableBase>(_disposablesCapacity);
			_asyncDisposablesWithToken.AddRange(disposables);
		}
	}
}
}