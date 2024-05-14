using System;

namespace Disposable
{
public abstract class DisposableBase : IDisposable
{
    protected bool disposed = false;

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

    protected virtual void DisposeManagedResources()
    {
    }

    protected virtual void DisposeUnmanagedResources()
    {
    }
}
}