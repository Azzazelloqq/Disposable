using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disposable.Utils;
using NUnit.Framework;

namespace Disposable.Tests
{
    /// <summary>
/// Tests for CollectionsExtensions
/// </summary>
    [TestFixture]
    public class CollectionsExtensionsTests
    {
        /// <summary>
        /// Test disposing IEnumerable<IDisposable>
        /// </summary>
        [Test]
        public void DisposeAllItems_IEnumerableDisposable_DisposesAllItems()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new MockDisposable())
                .ToList();
            
            // Act
            ((IEnumerable<IDisposable>)disposables).DisposeAllItems();
            
            // Assert
            foreach (var disposable in disposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All disposables should be disposed");
            }
            // Note: Count should NOT be 0 since DisposeAllItems doesn't clear the collection
            Assert.AreEqual(5, disposables.Count, "Collection should not be cleared by DisposeAllItems");
        }

        /// <summary>
        /// Test disposing List<IDisposable> with list clearing
        /// </summary>
        [Test]
        public void DisposeAll_ListDisposable_DisposesAllItemsAndClearsList()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new MockDisposable())
                .ToList();
            
            // Act
            disposables.DisposeAll();
            
            // Assert
            foreach (var disposable in disposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All disposables should be disposed");
            }
            Assert.AreEqual(0, disposables.Count, "List should be cleared");
        }

        /// <summary>
        /// Test disposing IDisposable array with element nulling
        /// </summary>
        [Test]
        public void DisposeAll_ArrayDisposable_DisposesAllItemsAndNullsElements()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new MockDisposable())
                .ToArray();
            
            // Act
            disposables.DisposeAll();
            
            // Assert
            for (int i = 0; i < disposables.Length; i++)
            {
                Assert.IsNull(disposables[i], $"Array element {i} should be null");
            }
        }

        /// <summary>
        /// Test handling null elements in collection
        /// </summary>
        [Test]
        public void DisposeAll_WithNullElements_DoesNotThrow()
        {
            // Arrange
            var mockDisposable1 = new MockDisposable();
            var mockDisposable2 = new MockDisposable();
            var disposables = new List<IDisposable>
            {
                mockDisposable1,
                null,
                mockDisposable2,
                null
            };
            
            // Act & Assert
            Assert.DoesNotThrow(() => disposables.DisposeAll());
            
            Assert.IsTrue(mockDisposable1.IsDisposed, "First disposable should be disposed");
            Assert.IsTrue(mockDisposable2.IsDisposed, "Third disposable should be disposed");
            Assert.AreEqual(0, disposables.Count, "List should be cleared");
        }

        /// <summary>
        /// Test asynchronous disposal of IEnumerable<DisposableBase>
        /// </summary>
        [Test]
        public async Task DisposeAllAsync_DisposableBase_DisposesAllItems()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new TestAsyncDisposableBase())
                .ToList();
            
            // Act
            await ((IEnumerable<DisposableBase>)disposables).DisposeAllAsync();
            
            // Assert
            foreach (var disposable in disposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All disposables should be disposed");
            }
        }

        /// <summary>
        /// Test asynchronous disposal with CancellationToken
        /// </summary>
        [Test]
        public async Task DisposeAllAsync_WithCancellationToken_PassesTokenToDispose()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 3)
                .Select(_ => new TestAsyncDisposableBase())
                .ToList();
            using var cts = new CancellationTokenSource();
            
            // Act
            await ((IEnumerable<DisposableBase>)disposables).DisposeAllAsync(cts.Token);
            
            // Assert
            foreach (var disposable in disposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All disposables should be disposed");
                Assert.AreEqual(cts.Token, ((TestAsyncDisposableBase)disposable).ReceivedToken, 
                    "CancellationToken should be passed to dispose method");
            }
        }

        /// <summary>
        /// Test cancellation of asynchronous disposal
        /// </summary>
        [Test]
        public void DisposeAllAsync_WithCancelledToken_ThrowsOperationCanceledException()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new TestAsyncDisposableBase())
                .ToList();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await ((IEnumerable<DisposableBase>)disposables).DisposeAllAsync(cts.Token));
        }

        /// <summary>
        /// Test asynchronous disposal of IEnumerable<IAsyncDisposable>
        /// </summary>
        [Test]
        public async Task DisposeAllAsync_IAsyncDisposable_DisposesAllItems()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new MockAsyncDisposable())
                .ToList();
            
            // Act
            await ((IEnumerable<IAsyncDisposable>)disposables).DisposeAllAsync();
            
            // Assert
            foreach (var disposable in disposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All async disposables should be disposed");
            }
        }

        /// <summary>
        /// Test asynchronous disposal of IAsyncDisposable array
        /// </summary>
        [Test]
        public async Task DisposeAllAsync_IAsyncDisposableArray_DisposesAllItemsAndNullsElements()
        {
            // Arrange
            var disposables = Enumerable.Range(0, 5)
                .Select(_ => new MockAsyncDisposable())
                .ToArray();
            
            // Act
            await disposables.DisposeAllAsync();
            
            // Assert
            for (int i = 0; i < disposables.Length; i++)
            {
                Assert.IsNull(disposables[i], $"Array element {i} should be null");
            }
        }

        /// <summary>
        /// Test handling null elements in asynchronous collections
        /// </summary>
        [Test]
        public async Task DisposeAllAsync_WithNullElements_DoesNotThrow()
        {
            // Arrange
            var disposables = new IAsyncDisposable[]
            {
                new MockAsyncDisposable(),
                null,
                new MockAsyncDisposable(),
                null
            };
            
            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await disposables.DisposeAllAsync());
            
            // Check that non-null elements were nulled
            for (int i = 0; i < disposables.Length; i++)
            {
                Assert.IsNull(disposables[i], $"Array element {i} should be null");
            }
        }

        /// <summary>
        /// Performance test for large collections
        /// </summary>
        [Test]
        public void DisposeAll_LargeCollection_PerformsWell()
        {
            // Arrange
            const int itemCount = 10000;
            var disposables = Enumerable.Range(0, itemCount)
                .Select(_ => new MockDisposable())
                .ToList();
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            disposables.DisposeAll();
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, "Large collection disposal should complete quickly");
            Assert.AreEqual(0, disposables.Count, "List should be cleared");
        }
    }

            /// <summary>
        /// Test implementation of DisposableBase for asynchronous tests
        /// </summary>
    public class TestAsyncDisposableBase : DisposableBase
    {
        public CancellationToken ReceivedToken { get; private set; }

        protected override async ValueTask DisposeAsyncCore(CancellationToken token, bool continueOnCapturedContext)
        {
            ReceivedToken = token;
            // Simulate asynchronous work
            await Task.Delay(1, token);
        }
    }
}