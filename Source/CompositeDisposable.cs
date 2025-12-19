using System;
using System.Collections;
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
	/// Indicates whether this instance has been disposed.
	/// </summary>
	private bool _isDisposed;
	
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
			if (_isDisposed)
			{
				return;
			}
			
			_isDisposed = true;
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
				Logger.LogError("Have async disposables. Invoke async dispose with lock thread. Maybe need invoke System.Threading.Tasks.ValueTask.");
			}

			foreach (var asyncDisposable in asyncDisposables)
			{
				if (asyncDisposable is null)
				{
					continue;
				}

				try
				{
					DisposeAsyncBlocking(asyncDisposable);
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
			if (_isDisposed)
			{
				return;
			}
			
			_isDisposed = true;
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
			if (_isDisposed)
			{
				return;
			}

			_isDisposed = true;
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
	public T AddDisposable<T>(T disposable) where T : IDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposable immediately
			if (_isDisposed)
			{
				disposable?.Dispose();
				return disposable;
			}
			
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.Add(disposable);
		}
		
		return disposable;
	}

	/// <inheritdoc/>
	public (T firstDisposable, T secondDisposable) AddDisposable<T>(T firstDisposable, T secondDisposable) where T : IDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposables immediately
			if (_isDisposed)
			{
				firstDisposable?.Dispose();
				secondDisposable?.Dispose();
				return (firstDisposable, secondDisposable);
			}
			
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.Add(firstDisposable);
			_disposables.Add(secondDisposable);
		}
		
		return (firstDisposable, secondDisposable);
	}

	/// <inheritdoc/>
	public (T firstDisposable, T secondDisposable, T thirdDisposable) AddDisposable<T>(T firstDisposable, T secondDisposable, T thirdDisposable) where T : IDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposables immediately
			if (_isDisposed)
			{
				firstDisposable?.Dispose();
				secondDisposable?.Dispose();
				thirdDisposable?.Dispose();
				return (firstDisposable, secondDisposable, thirdDisposable);
			}
			
			_disposables ??= new List<IDisposable>(_disposablesCapacity);
			_disposables.Add(firstDisposable);
			_disposables.Add(secondDisposable);
			_disposables.Add(thirdDisposable);
		}
		
		return (firstDisposable, secondDisposable, thirdDisposable);
	}

	/// <inheritdoc/>
	public IEnumerable<T> AddDisposable<T>(IEnumerable<T> disposables) where T : IDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposables immediately
			if (_isDisposed)
			{
				foreach (var disposable in disposables)
				{
					disposable?.Dispose();
				}
				return disposables;
			}
			
			_disposables ??= new List<IDisposable>(GetCapacityForEnumerable(disposables, _disposablesCapacity));
			foreach (var disposable in disposables)
			{
				_disposables.Add(disposable);
			}
		}
		
		return disposables;
	}

	/// <inheritdoc/>
	public T AddAsyncDisposable<T>(T disposable) where T : IAsyncDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposable immediately
			if (_isDisposed)
			{
				DisposeAsyncBlocking(disposable);
				return disposable;
			}
			
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.Add(disposable);
		}
		
		return disposable;
	}

	/// <inheritdoc/>
	public (T firstDisposable, T secondDisposable) AddAsyncDisposable<T>(T firstDisposable, T secondDisposable) where T : IAsyncDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposables immediately
			if (_isDisposed)
			{
				DisposeAsyncBlocking(firstDisposable);
				DisposeAsyncBlocking(secondDisposable);
				return (firstDisposable, secondDisposable);
			}
			
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.Add(firstDisposable);
			_asyncDisposables.Add(secondDisposable);
		}
		
		return (firstDisposable, secondDisposable);
	}

	/// <inheritdoc/>
	public (T firstDisposable, T secondDisposable, T thirdDisposable) AddAsyncDisposable<T>(T firstDisposable, T secondDisposable, T thirdDisposable) where T : IAsyncDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposables immediately
			if (_isDisposed)
			{
				DisposeAsyncBlocking(firstDisposable);
				DisposeAsyncBlocking(secondDisposable);
				DisposeAsyncBlocking(thirdDisposable);
				return (firstDisposable, secondDisposable, thirdDisposable);
			}
			
			_asyncDisposables ??= new List<IAsyncDisposable>(_disposablesCapacity);
			_asyncDisposables.Add(firstDisposable);
			_asyncDisposables.Add(secondDisposable);
			_asyncDisposables.Add(thirdDisposable);
		}
		
		return (firstDisposable, secondDisposable, thirdDisposable);
	}

	/// <inheritdoc/>
	public IEnumerable<T> AddAsyncDisposable<T>(IEnumerable<T> disposables) where T : IAsyncDisposable
	{
		lock (_lock)
		{
			// If already disposed, dispose the incoming disposables immediately
			if (_isDisposed)
			{
				foreach (var disposable in disposables)
				{
					DisposeAsyncBlocking(disposable);
				}
				return disposables;
			}
			
			_asyncDisposables ??= new List<IAsyncDisposable>(GetCapacityForEnumerable(disposables, _disposablesCapacity));
			foreach (var disposable in disposables)
			{
				_asyncDisposables.Add(disposable);
			}
		}
		
		return disposables;
	}

	private static void DisposeAsyncBlocking(IAsyncDisposable asyncDisposable)
	{
		asyncDisposable?.DisposeAsync().GetAwaiter().GetResult();
	}

	private static int GetCapacityForEnumerable<T>(IEnumerable<T> items, int defaultCapacity)
	{
		if (items is ICollection<T> typedCollection)
		{
			return Math.Max(defaultCapacity, typedCollection.Count);
		}

		if (items is ICollection untypedCollection)
		{
			return Math.Max(defaultCapacity, untypedCollection.Count);
		}

		return defaultCapacity;
	}
}
}