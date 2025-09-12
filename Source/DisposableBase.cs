using System;
using System.Threading;
using System.Threading.Tasks;

namespace Disposable
{
/// <summary>
/// Provides a base class for implementing the IDisposable pattern.
/// </summary>
public abstract class DisposableBase : IDisposable, IAsyncDisposable
{
	protected int _disposed;
	
	/// <summary>
	/// The cancellation token source that is used to signal disposal of the object.
	/// Lazily initialized to avoid unnecessary allocations.
	/// </summary>
	private CancellationTokenSource _disposeCancellationTokenSource;
	private readonly object _tokenSourceLock = new object();
	
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
	public bool IsDisposed => _disposed == 1;

	/// <summary>
	/// Finalizes an instance of the <see cref="DisposableBase"/> class.
	/// </summary>
	~DisposableBase()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 0)
		{
			Dispose(false);
		}
	}

	/// <inheritdoc/>
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		
		Dispose(true);
		GC.SuppressFinalize(this);
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
			
			// Cancel and dispose the cancellation token source if it was created
			if (_disposeCancellationTokenSource != null)
			{
				if (!_disposeCancellationTokenSource.IsCancellationRequested)
				{
					_disposeCancellationTokenSource.Cancel();
				}
				_disposeCancellationTokenSource.Dispose();
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
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		await DisposeAsyncCore(token, continueOnCapturedContext).ConfigureAwait(continueOnCapturedContext);

		DisposeUnmanagedResources();

		GC.SuppressFinalize(this);
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
		
		// Cancel and dispose the cancellation token source if it was created
		if (_disposeCancellationTokenSource != null)
		{
			if (!_disposeCancellationTokenSource.IsCancellationRequested)
			{
				_disposeCancellationTokenSource.Cancel();
			}
			_disposeCancellationTokenSource.Dispose();
		}
		
		return default;
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
		var tcs = new TaskCompletionSource<bool>();
		
		// Register callback to complete the task when disposed
		disposeCancellationToken.Register(() => tcs.TrySetResult(true));
		
		return tcs.Task;
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
		var tcs = new TaskCompletionSource<bool>();
		
		// Register callback to complete the task when disposed
		using var disposeRegistration = disposeCancellationToken.Register(() => tcs.TrySetResult(true));
		
		// Register callback to cancel the task if external token is cancelled
		using var cancelRegistration = cancellationToken.Register(() => tcs.TrySetCanceled());
		
		await tcs.Task.ConfigureAwait(false);
	}
}
}