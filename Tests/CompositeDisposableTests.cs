using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Disposable.Tests
{
    /// <summary>
/// Tests for the CompositeDisposable class
/// </summary>
    [TestFixture]
    public class CompositeDisposableTests
    {
        private CompositeDisposable _compositeDisposable;

        [SetUp]
        public void SetUp()
        {
            _compositeDisposable = new CompositeDisposable();
        }

        [TearDown]
        public void TearDown()
        {
            _compositeDisposable?.Dispose();
        }

        /// <summary>
        /// Test adding and disposing a single IDisposable resource
        /// </summary>
        [Test]
        public void AddDisposable_SingleDisposable_DisposesCorrectly()
        {
            // Arrange
            var mockDisposable = new MockDisposable();
            
            // Act
            _compositeDisposable.AddDisposable(mockDisposable);
            _compositeDisposable.Dispose();
            
            // Assert
            Assert.IsTrue(mockDisposable.IsDisposed, "Disposable should be disposed");
        }

        /// <summary>
        /// Test adding and disposing two IDisposable resources
        /// </summary>
        [Test]
        public void AddDisposable_TwoDisposables_DisposesCorrectly()
        {
            // Arrange
            var mockDisposable1 = new MockDisposable();
            var mockDisposable2 = new MockDisposable();
            
            // Act
            _compositeDisposable.AddDisposable(mockDisposable1, mockDisposable2);
            _compositeDisposable.Dispose();
            
            // Assert
            Assert.IsTrue(mockDisposable1.IsDisposed, "First disposable should be disposed");
            Assert.IsTrue(mockDisposable2.IsDisposed, "Second disposable should be disposed");
        }

        /// <summary>
        /// Test adding and disposing three IDisposable resources
        /// </summary>
        [Test]
        public void AddDisposable_ThreeDisposables_DisposesCorrectly()
        {
            // Arrange
            var mockDisposable1 = new MockDisposable();
            var mockDisposable2 = new MockDisposable();
            var mockDisposable3 = new MockDisposable();
            
            // Act
            _compositeDisposable.AddDisposable(mockDisposable1, mockDisposable2, mockDisposable3);
            _compositeDisposable.Dispose();
            
            // Assert
            Assert.IsTrue(mockDisposable1.IsDisposed, "First disposable should be disposed");
            Assert.IsTrue(mockDisposable2.IsDisposed, "Second disposable should be disposed");
            Assert.IsTrue(mockDisposable3.IsDisposed, "Third disposable should be disposed");
        }

        /// <summary>
        /// Test adding and disposing a collection of IDisposable resources
        /// </summary>
        [Test]
        public void AddDisposable_CollectionOfDisposables_DisposesCorrectly()
        {
            // Arrange
            var disposables = new List<IDisposable>
            {
                new MockDisposable(),
                new MockDisposable(),
                new MockDisposable()
            };
            
            // Act
            _compositeDisposable.AddDisposable(disposables);
            _compositeDisposable.Dispose();
            
            // Assert
            foreach (var disposable in disposables)
            {
                Assert.IsTrue(((MockDisposable)disposable).IsDisposed, "All disposables should be disposed");
            }
        }

        /// <summary>
        /// Test asynchronous adding and disposing of IAsyncDisposable resources
        /// </summary>
        [Test]
        public async Task AddDisposable_AsyncDisposable_DisposesCorrectly()
        {
            // Arrange
            var mockAsyncDisposable = new MockAsyncDisposable();
            
            // Act
            _compositeDisposable.AddDisposable(mockAsyncDisposable);
            await _compositeDisposable.DisposeAsync();
            
            // Assert
            Assert.IsTrue(mockAsyncDisposable.IsDisposed, "Async disposable should be disposed");
        }

        /// <summary>
        /// Test handling null values
        /// </summary>
        [Test]
        public void AddDisposable_NullDisposable_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _compositeDisposable.AddDisposable((IDisposable)null));
            Assert.DoesNotThrow(() => _compositeDisposable.Dispose());
        }

        /// <summary>
        /// Test creating CompositeDisposable with specified capacity
        /// </summary>
        [Test]
        public void Constructor_WithCapacity_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => new CompositeDisposable(50));
        }

        /// <summary>
        /// Test asynchronous disposal with CancellationToken
        /// </summary>
        [Test]
        public async Task DisposeAsync_WithCancellationToken_DisposesCorrectly()
        {
            // Arrange
            var mockAsyncDisposable = new MockAsyncDisposable();
            using var cts = new CancellationTokenSource();
            
            // Act
            _compositeDisposable.AddDisposable(mockAsyncDisposable);
            await _compositeDisposable.DisposeAsync(cts.Token);
            
            // Assert
            Assert.IsTrue(mockAsyncDisposable.IsDisposed, "Async disposable should be disposed with cancellation token");
        }

        /// <summary>
        /// Test cancellation of asynchronous disposal
        /// </summary>
        [Test]
        public void DisposeAsync_WithCancelledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var mockAsyncDisposable = new MockAsyncDisposable();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // Act
            _compositeDisposable.AddDisposable(mockAsyncDisposable);
            
            // Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await _compositeDisposable.DisposeAsync(cts.Token));
        }

        /// <summary>
        /// Test adding DisposableBase objects
        /// </summary>
        [Test]
        public async Task AddDisposable_DisposableBase_DisposesCorrectly()
        {
            // Arrange
            var mockDisposableBase = new MockDisposableBase();
            
            // Act
            _compositeDisposable.AddDisposable(mockDisposableBase);
            await _compositeDisposable.DisposeAsync();
            
            // Assert
            Assert.IsTrue(mockDisposableBase.IsDisposed, "DisposableBase should be disposed");
        }

        /// <summary>
        /// Test multiple Dispose calls - should be safe
        /// </summary>
        [Test]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var mockDisposable = new MockDisposable();
            _compositeDisposable.AddDisposable(mockDisposable);
            
            // Act & Assert
            Assert.DoesNotThrow(() => _compositeDisposable.Dispose());
            Assert.DoesNotThrow(() => _compositeDisposable.Dispose());
            Assert.IsTrue(mockDisposable.IsDisposed, "Disposable should remain disposed");
        }

        /// <summary>
        /// Test that adding a disposable to already disposed CompositeDisposable
        /// immediately disposes the added disposable
        /// </summary>
        [Test]
        public void AddDisposable_ToDisposedComposite_DisposesImmediately()
        {
            // Arrange
            _compositeDisposable.Dispose();
            var mockDisposable = new MockDisposable();
            
            // Act
            _compositeDisposable.AddDisposable(mockDisposable);
            
            // Assert
            Assert.IsTrue(mockDisposable.IsDisposed, 
                "Disposable should be disposed immediately when added to disposed composite");
        }

        /// <summary>
        /// Test that adding multiple disposables to already disposed CompositeDisposable
        /// immediately disposes all added disposables
        /// </summary>
        [Test]
        public void AddDisposable_MultipleToDisposedComposite_AllDisposeImmediately()
        {
            // Arrange
            _compositeDisposable.Dispose();
            var mockDisposable1 = new MockDisposable();
            var mockDisposable2 = new MockDisposable();
            var mockDisposable3 = new MockDisposable();
            
            // Act
            _compositeDisposable.AddDisposable(mockDisposable1, mockDisposable2);
            _compositeDisposable.AddDisposable(mockDisposable3);
            
            // Assert
            Assert.IsTrue(mockDisposable1.IsDisposed, 
                "First disposable should be disposed immediately");
            Assert.IsTrue(mockDisposable2.IsDisposed, 
                "Second disposable should be disposed immediately");
            Assert.IsTrue(mockDisposable3.IsDisposed, 
                "Third disposable should be disposed immediately");
        }

        /// <summary>
        /// Test that adding async disposable to already disposed CompositeDisposable
        /// attempts to dispose it synchronously if it implements IDisposable
        /// </summary>
        [Test]
        public void AddAsyncDisposable_ToDisposedComposite_DisposesImmediately()
        {
            // Arrange
            _compositeDisposable.Dispose();
            var mockAsyncDisposable = new MockAsyncDisposableWithSync();
            
            // Act
            _compositeDisposable.AddDisposable((IAsyncDisposable)mockAsyncDisposable);
            
            // Assert
            Assert.IsTrue(mockAsyncDisposable.IsSyncDisposed, 
                "Async disposable should be disposed synchronously when added to disposed composite");
        }
    }

            /// <summary>
        /// Mock class for testing IDisposable
        /// </summary>
    public class MockDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

            /// <summary>
        /// Mock class for testing IAsyncDisposable
        /// </summary>
    public class MockAsyncDisposable : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }

            /// <summary>
        /// Mock class for testing IAsyncDisposable that also implements IDisposable
        /// </summary>
    public class MockAsyncDisposableWithSync : IAsyncDisposable, IDisposable
    {
        public bool IsAsyncDisposed { get; private set; }
        public bool IsSyncDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsAsyncDisposed = true;
            return default;
        }

        public void Dispose()
        {
            IsSyncDisposed = true;
        }
    }

            /// <summary>
        /// Mock class for testing DisposableBase
        /// </summary>
    public class MockDisposableBase : DisposableBase
    {
        protected override void DisposeManagedResources()
        {
            // Mock implementation
        }

        protected override void DisposeUnmanagedResources()
        {
            // Mock implementation
        }
    }
}