using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Disposable
{
/// <summary>
/// Provides a base class for implementing the IDisposable pattern.
/// </summary>
public abstract class DisposableBase : IDisposable, IAsyncDisposable
{
	protected int disposed;

	/// <summary>
	/// The cancellation token source that is used to signal disposal of the object.
	/// Lazily initialized to avoid unnecessary allocations.
	/// </summary>
	private CancellationTokenSource _disposeCancellationTokenSource;

	private readonly object _tokenSourceLock = new();

	/// <summary>
	/// Composite disposable used to track child disposables.
	/// </summary>
	private ICompositeDisposable _compositeDisposable;

	private readonly object _compositeDisposableLock = new();

	/// <summary>
	/// Gets the cancellation token source, creating it if necessary.
	/// </summary>
	protected CancellationTokenSource DisposeCancellationTokenSource
	{
		get
		{
			if (_disposeCancellationTokenSource == null)
			{
				lock (_tokenSourceLock)
				{
					_disposeCancellationTokenSource ??= new CancellationTokenSource();
				}
			}

			return _disposeCancellationTokenSource;
		}
	}

	/// <summary>
	/// Gets the cancellation token that is triggered when the object is disposed.
	/// </summary>
	protected CancellationToken disposeCancellationToken => DisposeCancellationTokenSource.Token;

	/// <summary>
	/// Gets a value indicating whether this instance has been disposed.
	/// </summary>
	/// <value><c>true</c> if this instance is disposed; otherwise, <c>false</c>.</value>
	public bool IsDisposed => disposed == 1;

	/// <summary>
	/// Finalizes an instance of the <see cref="DisposableBase"/> class.
	/// </summary>
	~DisposableBase()
	{
		if (Interlocked.Exchange(ref disposed, 1) == 0)
		{
			Dispose(false);
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (Interlocked.Exchange(ref disposed, 1) != 0)
		{
			return;
		}

		Dispose(true);
		GC.SuppressFinalize(this);

		OnDispose();
	}

	/// <summary>
	/// Disposes the object.
	/// </summary>
	/// <param name="disposing">
	/// <c>true</c> to release both managed and unmanaged resources;
	/// <c>false</c> to release only unmanaged resources.
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			DisposeManagedResources();
			DisposeCompositeDisposables();

			// Cancel the cancellation token source if it was created
			if (_disposeCancellationTokenSource != null)
			{
				if (!_disposeCancellationTokenSource.IsCancellationRequested)
				{
					_disposeCancellationTokenSource.Cancel();
				}
				// Don't dispose immediately - tokens handed out to external code need to remain valid
				// The GC will eventually clean this up
			}
		}

		DisposeUnmanagedResources();
	}

	/// <inheritdoc/>
	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Asynchronously disposes the object with cancellation support.
	/// </summary>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <param name="continueOnCapturedContext">Whether to continue on the captured context.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public virtual async ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext = false)
	{
		if (Interlocked.Exchange(ref disposed, 1) != 0)
		{
			return;
		}

		await DisposeAsyncCore(token, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext);
		await DisposeCompositeDisposablesAsync(token, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext);

		// Cancel the cancellation token source if it was created
		if (_disposeCancellationTokenSource != null)
		{
			if (!_disposeCancellationTokenSource.IsCancellationRequested)
			{
				_disposeCancellationTokenSource.Cancel();
			}
			// Don't dispose immediately - tokens handed out to external code need to remain valid
		}

		DisposeUnmanagedResources();

		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Waits for disposal to complete asynchronously.
	/// </summary>
	/// <returns>A task that completes when the object is disposed.</returns>
	public Task WaitForDisposalAsync()
	{
		if (IsDisposed)
		{
			return Task.CompletedTask;
		}

		// Create TaskCompletionSource for waiting
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Register callback to complete the task when disposed
		var disposeRegistration = disposeCancellationToken.Register(() => tcs.TrySetResult(true));

		return AwaitWithRegistrations(tcs.Task, disposeRegistration);
	}

	/// <summary>
	/// Waits for disposal to complete asynchronously with cancellation support.
	/// </summary>
	/// <param name="cancellationToken">The cancellation token to observe while waiting.</param>
	/// <returns>A task that completes when the object is disposed or the wait is cancelled.</returns>
	public async Task WaitForDisposalAsync(CancellationToken cancellationToken)
	{
		if (IsDisposed)
		{
			return;
		}

		// Create TaskCompletionSource for waiting
		var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		// Register callback to complete the task when disposed
		var disposeRegistration = disposeCancellationToken.Register(() => tcs.TrySetResult(true));

		// Register callback to cancel the task if external token is cancelled
		var cancelRegistration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

		await AwaitWithRegistrations(tcs.Task, disposeRegistration, cancelRegistration).ConfigureAwait(false);
	}


	/// <summary>
	/// Override this method to implement async disposal logic.
	/// This method is called when the object is disposed asynchronously.
	/// </summary>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <param name="continueOnCapturedContext"></param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	protected virtual ValueTask DisposeAsyncCore(CancellationToken token, bool continueOnCapturedContext)
	{
		DisposeManagedResources();
		return default;
	}

	/// <summary>
	/// Adds a disposable resource that should be cleaned up with this instance.
	/// </summary>
	/// <param name="disposable">Disposable resource to register.</param>
	/// <returns>The disposable resource that was added.</returns>
	protected T AddDisposable<T>(T disposable) where T : IDisposable
	{
		if (disposable == null)
		{
			return disposable;
		}

		if (IsDisposed)
		{
			disposable.Dispose();
			return disposable;
		}

		return EnsureCompositeDisposable().AddDisposable(disposable);
	}

	/// <summary>
	/// Adds two disposable resources that should be cleaned up with this instance.
	/// </summary>
	/// <returns>A tuple containing both disposable resources that were added.</returns>
	protected (T firstDisposable, T secondDisposable) AddDisposable<T>(T firstDisposable, T secondDisposable) where T : IDisposable
	{
		if (firstDisposable == null && secondDisposable == null)
		{
			return (firstDisposable, secondDisposable);
		}

		if (IsDisposed)
		{
			firstDisposable?.Dispose();
			secondDisposable?.Dispose();
			return (firstDisposable, secondDisposable);
		}

		return EnsureCompositeDisposable().AddDisposable(firstDisposable, secondDisposable);
	}

	/// <summary>
	/// Adds three disposable resources that should be cleaned up with this instance.
	/// </summary>
	/// <returns>A tuple containing all three disposable resources that were added.</returns>
	protected (T firstDisposable, T secondDisposable, T thirdDisposable) AddDisposable<T>(T firstDisposable, T secondDisposable, T thirdDisposable) where T : IDisposable
	{
		if (firstDisposable == null && secondDisposable == null && thirdDisposable == null)
		{
			return (firstDisposable, secondDisposable, thirdDisposable);
		}

		if (IsDisposed)
		{
			firstDisposable?.Dispose();
			secondDisposable?.Dispose();
			thirdDisposable?.Dispose();
			return (firstDisposable, secondDisposable, thirdDisposable);
		}

		return EnsureCompositeDisposable().AddDisposable(firstDisposable, secondDisposable, thirdDisposable);
	}

	/// <summary>
	/// Adds a collection of disposable resources that should be cleaned up with this instance.
	/// </summary>
	/// <returns>The collection of disposables that were added.</returns>
	protected IEnumerable<T> AddDisposable<T>(IEnumerable<T> disposables) where T : IDisposable
	{
		if (disposables == null)
		{
			return disposables;
		}

		if (IsDisposed)
		{
			foreach (var disposable in disposables)
			{
				disposable?.Dispose();
			}

			return disposables;
		}

		return EnsureCompositeDisposable().AddDisposable(disposables);
	}

	/// <summary>
	/// Adds an async disposable resource that should be cleaned up with this instance.
	/// </summary>
	/// <returns>The async disposable resource that was added.</returns>
	protected T AddAsyncDisposable<T>(T disposable) where T : IAsyncDisposable
	{
		if (disposable == null)
		{
			return disposable;
		}

		if (IsDisposed)
		{
			DisposeAsyncBlocking(disposable);
			return disposable;
		}

		return EnsureCompositeDisposable().AddAsyncDisposable(disposable);
	}

	/// <summary>
	/// Adds two async disposable resources that should be cleaned up with this instance.
	/// </summary>
	/// <returns>A tuple containing both async disposable resources that were added.</returns>
	protected (T firstDisposable, T secondDisposable) AddAsyncDisposable<T>(T firstDisposable, T secondDisposable) where T : IAsyncDisposable
	{
		if (firstDisposable == null && secondDisposable == null)
		{
			return (firstDisposable, secondDisposable);
		}

		if (IsDisposed)
		{
			DisposeAsyncBlocking(firstDisposable);
			DisposeAsyncBlocking(secondDisposable);
			return (firstDisposable, secondDisposable);
		}

		return EnsureCompositeDisposable().AddAsyncDisposable(firstDisposable, secondDisposable);
	}

	/// <summary>
	/// Adds three async disposable resources that should be cleaned up with this instance.
	/// </summary>
	/// <returns>A tuple containing all three async disposable resources that were added.</returns>
	protected (T firstDisposable, T secondDisposable, T thirdDisposable) AddAsyncDisposable<T>(
		T firstDisposable,
		T secondDisposable,
		T thirdDisposable) where T : IAsyncDisposable
	{
		if (firstDisposable == null && secondDisposable == null && thirdDisposable == null)
		{
			return (firstDisposable, secondDisposable, thirdDisposable);
		}

		if (IsDisposed)
		{
			DisposeAsyncBlocking(firstDisposable);
			DisposeAsyncBlocking(secondDisposable);
			DisposeAsyncBlocking(thirdDisposable);
			return (firstDisposable, secondDisposable, thirdDisposable);
		}

		return EnsureCompositeDisposable().AddAsyncDisposable(firstDisposable, secondDisposable, thirdDisposable);
	}

	/// <summary>
	/// Adds a collection of async disposable resources that should be cleaned up with this instance.
	/// </summary>
	/// <returns>The collection of async disposables that were added.</returns>
	protected IEnumerable<T> AddAsyncDisposable<T>(IEnumerable<T> disposables) where T : IAsyncDisposable
	{
		if (disposables == null)
		{
			return disposables;
		}

		if (IsDisposed)
		{
			foreach (var disposable in disposables)
			{
				DisposeAsyncBlocking(disposable);
			}

			return disposables;
		}

		return EnsureCompositeDisposable().AddAsyncDisposable(disposables);
	}

	/// <summary>
	/// Disposes the managed resources. Override this method to dispose managed resources.
	/// </summary>
	protected virtual void DisposeManagedResources()
	{
	}

	/// <summary>
	/// Disposes the unmanaged resources. Override this method to dispose unmanaged resources.
	/// </summary>
	protected virtual void DisposeUnmanagedResources()
	{
	}

	protected virtual void OnDispose()
	{
	}

	private static async Task AwaitWithRegistrations(
		Task awaitable,
		CancellationTokenRegistration firstRegistration,
		CancellationTokenRegistration secondRegistration = default)
	{
		try
		{
			await awaitable.ConfigureAwait(false);
		}
		finally
		{
			await firstRegistration.DisposeAsync();
			await secondRegistration.DisposeAsync();
		}
	}

	private ICompositeDisposable EnsureCompositeDisposable()
	{
		if (_compositeDisposable != null)
		{
			return _compositeDisposable;
		}

		lock (_compositeDisposableLock)
		{
			return _compositeDisposable ??= new CompositeDisposable();
		}
	}

	private void DisposeCompositeDisposables()
	{
		var compositeDisposable = Interlocked.Exchange(ref _compositeDisposable, null);
		compositeDisposable?.Dispose();
	}

	private async ValueTask DisposeCompositeDisposablesAsync(CancellationToken token, bool continueOnCapturedContext)
	{
		var compositeDisposable = Interlocked.Exchange(ref _compositeDisposable, null);
		if (compositeDisposable == null)
		{
			return;
		}

		await compositeDisposable.DisposeAsync(token, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext);
	}

	private static void DisposeAsyncBlocking(IAsyncDisposable asyncDisposable)
	{
		asyncDisposable?.DisposeAsync().GetAwaiter().GetResult();
	}
}
}