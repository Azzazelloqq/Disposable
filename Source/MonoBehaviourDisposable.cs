using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Disposable
{
/// <summary>
/// Base class for <see cref="MonoBehaviour"/> that implements the IDisposable pattern.
/// Provides automatic resource management for Unity components.
/// </summary>
public class MonoBehaviourDisposable : MonoBehaviour, IDisposable, IAsyncDisposable
{
	/// <summary>
	/// The cancellation token source that is used to signal disposal of the object.
	/// </summary>
	protected readonly CancellationTokenSource _disposeCancellationTokenSource = new();
	
	/// <summary>
	/// The cancellation token source that is used to signal destruction of the GameObject.
	/// </summary>
	protected readonly CancellationTokenSource _destroyCancellationTokenSource = new();
	
	/// <summary>
	/// Gets the cancellation token that is triggered when the object is disposed.
	/// </summary>
	protected CancellationToken disposeCancellationToken => _disposeCancellationTokenSource.Token;
	
	/// <summary>
	/// Gets the cancellation token that is triggered when the GameObject is destroyed.
	/// This token is linked with Application.exitCancellationToken in Unity 2022.2+
	/// </summary>
	protected CancellationToken destroyCancellationToken
	{
		get
		{
#if UNITY_2022_2_OR_NEWER
			// Link with Application.exitCancellationToken if available
			if (Application.exitCancellationToken != CancellationToken.None)
			{
				using var linked = CancellationTokenSource.CreateLinkedTokenSource(
					_destroyCancellationTokenSource.Token,
					Application.exitCancellationToken);
				return linked.Token;
			}
#endif
			return _destroyCancellationTokenSource.Token;
		}
	}
	
	/// <summary>
	/// Gets a value indicating whether this instance has been disposed.
	/// </summary>
	/// <value><c>true</c> if this instance is disposed; otherwise, <c>false</c>.</value>
	public bool IsDisposed { get; private set; }
	
	/// <summary>
	/// Gets a value indicating whether this instance has been destroyed.
	/// </summary>
	/// <value><c>true</c> if this instance is destroyed; otherwise, <c>false</c>.</value>
	public bool IsDestroyed { get; private set; }

	/// <inheritdoc/>
	public virtual void Dispose()
	{
		if (IsDisposed)
		{
			return;
		}

		Dispose(true);
		GC.SuppressFinalize(this);

		IsDisposed = true;

		if (!IsDestroyed)
		{
			DestroyGameObjectSafe();
		}
	}
	
	/// <inheritdoc/>
	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(CancellationToken.None).ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Asynchronously disposes this instance with cancellation support.
	/// </summary>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <param name="continueOnCapturedContext">Whether to continue on the captured context.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	public virtual async ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext = false)
	{
		if (IsDisposed)
		{
			return;
		}

		await DisposeAsyncCore(token).ConfigureAwait(continueOnCapturedContext);

		Dispose(false);

		IsDisposed = true;

		if (!IsDestroyed)
		{
			DestroyGameObjectSafe();
		}

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
		if (IsDisposed)
		{
			return;
		}

		if (disposing)
		{
			DisposeManagedResources();
			
			// Cancel and dispose the cancellation token sources
			if (!_disposeCancellationTokenSource.IsCancellationRequested)
			{
				_disposeCancellationTokenSource.Cancel();
			}
			_disposeCancellationTokenSource.Dispose();
			
			if (!_destroyCancellationTokenSource.IsCancellationRequested)
			{
				_destroyCancellationTokenSource.Cancel();
			}
			_destroyCancellationTokenSource.Dispose();
		}

		DisposeUnmanagedResources();

		if (!IsDestroyed)
		{
			DestroyGameObjectSafe();
		}

		IsDisposed = true;
	}

	/// <summary>
	/// Unity callback called when the MonoBehaviour is destroyed.
	/// Automatically disposes all managed resources.
	/// </summary>
	private void OnDestroy()
	{
		// Signal destruction via cancellation token
		if (!_destroyCancellationTokenSource.IsCancellationRequested)
		{
			_destroyCancellationTokenSource.Cancel();
		}
		
		if (IsDisposed)
		{
			return;
		}

		Dispose(true);
		IsDestroyed = true;
	}
	
	/// <summary>
	/// Unity callback called when the application is about to quit.
	/// </summary>
	private void OnApplicationQuit()
	{
		// Signal application quit via destroy token
		if (!_destroyCancellationTokenSource.IsCancellationRequested)
		{
			_destroyCancellationTokenSource.Cancel();
		}
	}

	/// <summary>
	/// Override this method to dispose managed resources specific to your MonoBehaviour.
	/// This method is called during disposal of managed resources.
	/// </summary>
	protected virtual void DisposeManagedResources()
	{
	}

	/// <summary>
	/// Override this method to dispose unmanaged resources specific to your MonoBehaviour.
	/// This method is called during disposal of unmanaged resources.
	/// </summary>
	protected virtual void DisposeUnmanagedResources()
	{
	}

	/// <summary>
	/// Override this method to implement async disposal logic specific to your MonoBehaviour.
	/// This method is called during asynchronous disposal.
	/// </summary>
	/// <param name="token">The cancellation token to observe while waiting for the task to complete.</param>
	/// <returns>A value task that represents the asynchronous dispose operation.</returns>
	protected virtual ValueTask DisposeAsyncCore(CancellationToken token, bool continueOnCapturedContext = false)
	{
		// Cancel and dispose the cancellation token sources
		if (!_disposeCancellationTokenSource.IsCancellationRequested)
		{
			_disposeCancellationTokenSource.Cancel();
		}
		_disposeCancellationTokenSource.Dispose();
		
		if (!_destroyCancellationTokenSource.IsCancellationRequested)
		{
			_destroyCancellationTokenSource.Cancel();
		}
		_destroyCancellationTokenSource.Dispose();
		
		return default;
	}

	/// <summary>
	/// Safely destroys the GameObject, handling potential Unity threading issues.
	/// </summary>
	private void DestroyGameObjectSafe()
	{
		if (IsDestroyed) return;

		try
		{
			if (gameObject != null)
			{
				Destroy(gameObject);
			}
		}
		catch (UnityException ex) when (ex.Message.Contains("main thread"))
		{
			// If we're not on the main thread, schedule destruction for the next frame
#if UNITY_EDITOR
			var localRef = this; // Capture reference to avoid accessing destroyed object
			UnityEditor.EditorApplication.delayCall += () =>
			{
				try
				{
					if (localRef != null && localRef.gameObject != null && !localRef.IsDestroyed)
					{
						Destroy(localRef.gameObject);
					}
				}
				catch
				{
					// Ignore exceptions from destroyed objects
				}
			};
#endif
		}
		finally
		{
			IsDestroyed = true;
		}
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
	
	/// <summary>
	/// Waits for GameObject destruction to complete asynchronously.
	/// </summary>
	/// <returns>A task that completes when the GameObject is destroyed.</returns>
	public Task WaitForDestroyAsync()
	{
		if (IsDestroyed)
		{
			return Task.CompletedTask;
		}
		
		// Create TaskCompletionSource for waiting
		var tcs = new TaskCompletionSource<bool>();
		
		// Register callback to complete the task when destroyed
		destroyCancellationToken.Register(() => tcs.TrySetResult(true));
		
		return tcs.Task;
	}
}
}