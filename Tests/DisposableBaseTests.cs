using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Disposable.Tests
{
    /// <summary>
/// Tests for the abstract DisposableBase class
/// </summary>
    [TestFixture]
    public class DisposableBaseTests
    {
        /// <summary>
        /// Test normal resource disposal
        /// </summary>
        [Test]
        public void Dispose_CallsDisposeManagedAndUnmanagedResources()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act
            testDisposable.Dispose();
            
            // Assert
            Assert.IsTrue(testDisposable.IsDisposed, "Object should be disposed");
            Assert.IsTrue(testDisposable.ManagedResourcesDisposed, "Managed resources should be disposed");
            Assert.IsTrue(testDisposable.UnmanagedResourcesDisposed, "Unmanaged resources should be disposed");
        }

        /// <summary>
        /// Test multiple Dispose calls - should be safe
        /// </summary>
        [Test]
        public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act
            testDisposable.Dispose();
            var firstCallCount = testDisposable.DisposeCallCount;
            
            testDisposable.Dispose();
            var secondCallCount = testDisposable.DisposeCallCount;
            
            // Assert
            Assert.AreEqual(1, firstCallCount, "Dispose should be called once");
            Assert.AreEqual(1, secondCallCount, "Dispose should not be called again");
            Assert.IsTrue(testDisposable.IsDisposed, "Object should remain disposed");
        }

        /// <summary>
        /// Test asynchronous resource disposal
        /// </summary>
        [Test]
        public async Task DisposeAsync_CallsDisposeAsyncCore()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act
            await testDisposable.DisposeAsync();
            
            // Assert
            Assert.IsTrue(testDisposable.IsDisposed, "Object should be disposed");
            Assert.IsTrue(testDisposable.AsyncDisposeCoreCalled, "DisposeAsyncCore should be called");
            Assert.IsTrue(testDisposable.UnmanagedResourcesDisposed, "Unmanaged resources should be disposed");
        }

        /// <summary>
        /// Test asynchronous disposal with CancellationToken
        /// </summary>
        [Test]
        public async Task DisposeAsync_WithCancellationToken_PassesTokenToCore()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            using var cts = new CancellationTokenSource();
            
            // Act
            await testDisposable.DisposeAsync(cts.Token);
            
            // Assert
            Assert.IsTrue(testDisposable.IsDisposed, "Object should be disposed");
            Assert.IsTrue(testDisposable.AsyncDisposeCoreCalled, "DisposeAsyncCore should be called");
            Assert.AreEqual(cts.Token, testDisposable.ReceivedToken, "CancellationToken should be passed to DisposeAsyncCore");
        }

        /// <summary>
        /// Test cancellation of asynchronous disposal
        /// </summary>
        [Test]
        public void DisposeAsync_WithCancelledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await testDisposable.DisposeAsync(cts.Token));
        }

        /// <summary>
        /// Test multiple DisposeAsync calls
        /// </summary>
        [Test]
        public async Task DisposeAsync_CalledMultipleTimes_OnlyDisposesOnce()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act
            await testDisposable.DisposeAsync();
            var firstCallCount = testDisposable.AsyncDisposeCallCount;
            
            await testDisposable.DisposeAsync();
            var secondCallCount = testDisposable.AsyncDisposeCallCount;
            
            // Assert
            Assert.AreEqual(1, firstCallCount, "DisposeAsync should be called once");
            Assert.AreEqual(1, secondCallCount, "DisposeAsync should not be called again");
        }

        /// <summary>
        /// Test finalizer (calling Dispose(false))
        /// </summary>
        [Test]
        public void Finalizer_CallsDisposeWithFalse()
        {
            // Arrange & Act
            var testDisposable = new TestDisposableBase();
            testDisposable.CallFinalizer(); // Simulate finalizer call
            
            // Assert
            Assert.IsTrue(testDisposable.IsDisposed, "Object should be disposed");
            Assert.IsFalse(testDisposable.ManagedResourcesDisposed, "Managed resources should not be disposed in finalizer");
            Assert.IsTrue(testDisposable.UnmanagedResourcesDisposed, "Unmanaged resources should be disposed in finalizer");
        }

        /// <summary>
        /// Test checking IsDisposed state
        /// </summary>
        [Test]
        public void IsDisposed_BeforeDispose_ReturnsFalse()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act & Assert
            Assert.IsFalse(testDisposable.IsDisposed, "Object should not be disposed initially");
        }

        /// <summary>
        /// Test thread-safety with concurrent Dispose calls
        /// </summary>
        [Test]
        public void Dispose_ConcurrentCalls_OnlyDisposesOnce()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            var tasks = new Task[10];
            
            // Act
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() => testDisposable.Dispose());
            }
            
            Task.WaitAll(tasks);
            
            // Assert
            Assert.AreEqual(1, testDisposable.DisposeCallCount, "Dispose should be called only once despite concurrent calls");
            Assert.IsTrue(testDisposable.IsDisposed, "Object should be disposed");
        }

        /// <summary>
        /// Test that disposeCancellationToken is accessible and not cancelled before disposal
        /// </summary>
        [Test]
        public void DisposeCancellationToken_BeforeDispose_IsNotCancelled()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act
            var token = testDisposable.GetDisposeCancellationToken();
            
            // Assert
            Assert.IsFalse(token.IsCancellationRequested, "Cancellation token should not be cancelled before disposal");
        }

        /// <summary>
        /// Test that disposeCancellationToken is cancelled after disposal
        /// </summary>
        [Test]
        public void DisposeCancellationToken_AfterDispose_IsCancelled()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            var token = testDisposable.GetDisposeCancellationToken(); // Get token before disposal
            
            // Act
            testDisposable.Dispose();
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Cancellation token should be cancelled after disposal");
        }

        /// <summary>
        /// Test that disposeCancellationToken is cancelled after async disposal
        /// </summary>
        [Test]
        public async Task DisposeCancellationToken_AfterDisposeAsync_IsCancelled()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            var token = testDisposable.GetDisposeCancellationToken();
            
            // Act
            await testDisposable.DisposeAsync();
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Cancellation token should be cancelled after async disposal");
        }

        /// <summary>
        /// Test that disposeCancellationToken can be used to cancel operations
        /// </summary>
        [Test]
        public async Task DisposeCancellationToken_CanBeCombinedWithOtherTokens()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            var disposalToken = testDisposable.GetDisposeCancellationToken();
            using var cts = new CancellationTokenSource();
            using var combined = CancellationTokenSource.CreateLinkedTokenSource(disposalToken, cts.Token);
            
            // Act
            testDisposable.Dispose();
            
            // Assert
            Assert.IsTrue(combined.Token.IsCancellationRequested, "Combined token should be cancelled when disposal token is cancelled");
        }

        /// <summary>
        /// Test that the cancellation token source is properly initialized
        /// </summary>
        [Test]
        public void DisposeCancellationTokenSource_IsInitialized()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act
            var tokenSource = testDisposable.GetDisposeCancellationTokenSource();
            
            // Assert
            Assert.IsNotNull(tokenSource, "Cancellation token source should be initialized");
            Assert.IsFalse(tokenSource.IsCancellationRequested, "Cancellation token source should not be cancelled initially");
        }
        
        /// <summary>
        /// Test that cancellation token source is lazily initialized
        /// </summary>
        [Test]
        public void DisposeCancellationTokenSource_IsLazilyInitialized()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Assert - token source should not be created initially
            Assert.IsFalse(testDisposable.IsTokenSourceCreated(), 
                "CancellationTokenSource should not be created until first access");
            
            // Act - access the token
            var token = testDisposable.GetDisposeCancellationToken();
            
            // Assert - now token source should be created
            Assert.IsTrue(testDisposable.IsTokenSourceCreated(), 
                "CancellationTokenSource should be created after first access");
            Assert.IsFalse(token.IsCancellationRequested, "Token should not be cancelled");
        }
        
        /// <summary>
        /// Test WaitForDisposalAsync completes when disposed
        /// </summary>
        [Test]
        public async Task WaitForDisposalAsync_CompletesWhenDisposed()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            
            // Act - start waiting in background
            var waitTask = testDisposable.WaitForDisposalAsync();
            
            // Assert - task should not be completed before disposal
            Assert.IsFalse(waitTask.IsCompleted, "Wait task should not be completed before disposal");
            
            // Dispose the object
            testDisposable.Dispose();
            
            // Assert - task should complete after disposal
            await waitTask;
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should complete after disposal");
        }
        
        /// <summary>
        /// Test WaitForDisposalAsync returns immediately if already disposed
        /// </summary>
        [Test]
        public async Task WaitForDisposalAsync_ReturnsImmediatelyIfAlreadyDisposed()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            testDisposable.Dispose();
            
            // Act
            var waitTask = testDisposable.WaitForDisposalAsync();
            
            // Assert
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should be completed immediately if already disposed");
            await waitTask; // Should not block
        }
        
        /// <summary>
        /// Test WaitForDisposalAsync with cancellation
        /// </summary>
        [Test]
        public async Task WaitForDisposalAsync_WithCancellation_CancelsWhenRequested()
        {
            // Arrange
            var testDisposable = new TestDisposableBase();
            using var cts = new CancellationTokenSource();
            
            // Act - start waiting with cancellation token
            var waitTask = testDisposable.WaitForDisposalAsync(cts.Token);
            
            // Cancel the token
            cts.Cancel();
            
            // Assert - should throw OperationCanceledException
            try
            {
                await waitTask;
                Assert.Fail("Should have thrown OperationCanceledException");
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

            /// <summary>
        /// Test implementation of DisposableBase for functionality verification
        /// </summary>
    public class TestDisposableBase : DisposableBase
    {
        public bool ManagedResourcesDisposed { get; private set; }
        public bool UnmanagedResourcesDisposed { get; private set; }
        public bool AsyncDisposeCoreCalled { get; private set; }
        public int DisposeCallCount { get; private set; }
        public int AsyncDisposeCallCount { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }
        
        // Expose protected members for testing
        public CancellationToken GetDisposeCancellationToken() => disposeCancellationToken;
        public CancellationTokenSource GetDisposeCancellationTokenSource() => DisposeCancellationTokenSource;
        
        // Check if token source is created (for testing lazy initialization)
        public bool IsTokenSourceCreated()
        {
            // Use reflection to check the private field
            var field = typeof(DisposableBase).GetField("_disposeCancellationTokenSource", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(this) != null;
        }

        protected override void DisposeManagedResources()
        {
            ManagedResourcesDisposed = true;
        }

        protected override void DisposeUnmanagedResources()
        {
            UnmanagedResourcesDisposed = true;
        }

        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            base.Dispose(disposing);
        }

        protected override async ValueTask DisposeAsyncCore(CancellationToken token, bool continueOnCapturedContext)
        {
            AsyncDisposeCoreCalled = true;
            AsyncDisposeCallCount++;
            ReceivedToken = token;
            
            // Simulate asynchronous work
            await Task.Delay(1, token);
            
            DisposeManagedResources();
        }

        // For finalizer testing
        public void CallFinalizer()
        {
            // Simulate real finalizer
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                Dispose(false);
            }
        }
    }
}