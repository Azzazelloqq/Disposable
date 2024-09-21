using System;

namespace Disposable
{
/// <summary>
/// Provides a base class for implementing the IDisposable pattern.
/// </summary>
public abstract class DisposableBase : IDisposable
{
	/// <summary>
	/// Indicates whether the object has already been disposed.
	/// </summary>
	protected bool disposed = false;

	/// <summary>
	/// Finalizer to release resources during garbage collection.
	/// </summary>
	~DisposableBase()
	{
		Dispose(false);
	}

	/// <summary>
	/// Releases all resources used by the object.
	/// </summary>
	public virtual void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Releases unmanaged and optionally managed resources.
	/// </summary>
	/// <param name="disposing">
	///   <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.
	/// </param>
	protected virtual void Dispose(bool disposing)
	{
		if (disposed)
		{
			return;
		}

		if (disposing)
		{
			DisposeManagedResources();
		}

		DisposeUnmanagedResources();

		disposed = true;
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