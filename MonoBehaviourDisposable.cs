using System;
using UnityEngine;

namespace Disposable
{
/// <summary>
/// Base class for <see cref="MonoBehaviour"/> that implements the IDisposable pattern.
/// </summary>
public class MonoBehaviourDisposable : MonoBehaviour
{
	/// <summary>
	/// Flag indicating whether the object has already been disposed.
	/// </summary>
	protected bool isDisposed = false;

	/// <summary>
	/// Flag indicating whether the object has already been destroyed.
	/// </summary>
	protected bool isDestroyed = false;

	/// <summary>
	/// Composite IDisposable to manage multiple IDisposable resources.
	/// </summary>
	protected readonly ICompositeDisposable compositeDisposable = new CompositeDisposable();

	/// <summary>
	/// Destructor to release resources during garbage collection.
	/// </summary>
	~MonoBehaviourDisposable()
	{
		Dispose(false);
	}

	/// <summary>
	/// Public method to release resources and destroy the GameObject.
	/// </summary>
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

		Destroy(gameObject);
	}

	/// <summary>
	/// Releases managed and unmanaged resources.
	/// </summary>
	/// <param name="disposing">True if called from Dispose(); false if called from the destructor.</param>
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

	/// <summary>
	/// Called when the Unity GameObject is destroyed.
	/// </summary>
	private void OnDestroy()
	{
		if (isDisposed)
		{
			return;
		}

		Dispose(true);
		isDestroyed = true;
	}

	/// <summary>
	/// Releases managed resources.
	/// Override this method to release your own managed resources.
	/// </summary>
	protected virtual void DisposeManagedResources()
	{
	}

	/// <summary>
	/// Releases unmanaged resources.
	/// Override this method to release your own unmanaged resources.
	/// </summary>
	protected virtual void DisposeUnmanagedResources()
	{
	}
}
}