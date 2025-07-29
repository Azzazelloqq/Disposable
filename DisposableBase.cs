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
	private int _disposed;

	protected bool IsDisposed => _disposed == 1;

	~DisposableBase()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		if (disposing)
		{
			DisposeManagedResources();
		}

		DisposeUnmanagedResources();
	}

	protected virtual void DisposeManagedResources()
	{
	}

	protected virtual void DisposeUnmanagedResources()
	{
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext = false)
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		await DisposeAsyncCore(token).ConfigureAwait(continueOnCapturedContext);

		DisposeUnmanagedResources();

		GC.SuppressFinalize(this);
	}

	protected virtual ValueTask DisposeAsyncCore(CancellationToken token)
	{
		DisposeManagedResources();
		return default;
	}
}
}