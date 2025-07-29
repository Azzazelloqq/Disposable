using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Disposable
{
/// <summary>
/// Base class for <see cref="MonoBehaviour"/> that implements the IDisposable pattern.
/// </summary>
public class MonoBehaviourDisposable : MonoBehaviour
{
	protected bool isDisposed = false;
	protected bool isDestroyed = false;

	protected readonly ICompositeDisposable compositeDisposable = new CompositeDisposable();

	public virtual void Dispose()
	{
		if (isDisposed)
		{
			return;
		}

		compositeDisposable?.Dispose();

		Dispose(true);
		GC.SuppressFinalize(this);

		isDisposed = true;

		if (!isDestroyed)
		{
			Destroy(gameObject);
			isDestroyed = true;
		}
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync(CancellationToken token)
	{
		if (isDisposed)
		{
			return;
		}

		if (compositeDisposable is not null)
		{
			await compositeDisposable.DisposeAsync(token).ConfigureAwait(false);
		}

		await DisposeAsyncCore(token).ConfigureAwait(false);

		Dispose(false);

		isDisposed = true;

		if (!isDestroyed)
		{
			Destroy(gameObject);
			isDestroyed = true;
		}

		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (isDisposed)
		{
			return;
		}

		if (disposing)
		{
			DisposeManagedResources();
		}

		DisposeUnmanagedResources();

		if (!isDestroyed)
		{
			Destroy(gameObject);
			isDestroyed = true;
		}

		isDisposed = true;
	}

	private void OnDestroy()
	{
		if (isDisposed)
		{
			return;
		}

		compositeDisposable?.Dispose();
		Dispose(true);
		isDestroyed = true;
	}

	protected virtual void DisposeManagedResources()
	{
	}

	protected virtual void DisposeUnmanagedResources()
	{
	}

	protected virtual ValueTask DisposeAsyncCore(CancellationToken token)
	{
		return default;
	}
}
}