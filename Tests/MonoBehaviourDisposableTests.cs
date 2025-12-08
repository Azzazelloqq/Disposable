using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Disposable.Tests
{
    /// <summary>
/// Tests for the MonoBehaviourDisposable class
/// </summary>
    [TestFixture]
    public class MonoBehaviourDisposableTests
    {
        private GameObject _testGameObject;
        private TestMonoBehaviourDisposable _testComponent;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestGameObject");
            _testComponent = _testGameObject.AddComponent<TestMonoBehaviourDisposable>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        /// <summary>
        /// Test that calling Dispose releases resources and destroys GameObject
        /// </summary>
        [Test]
        public void Dispose_DisposesResourcesAndDestroysGameObject()
        {
            // Act
            _testComponent.Dispose();
            
            // Assert
            Assert.IsTrue(_testComponent.IsDisposed, "Component should be disposed");
            Assert.IsTrue(_testComponent.IsDestroyed, "Component should be marked as destroyed");
            Assert.IsTrue(_testComponent.ManagedResourcesDisposed, "Managed resources should be disposed");
            Assert.IsTrue(_testComponent.UnmanagedResourcesDisposed, "Unmanaged resources should be disposed");
        }

        /// <summary>
        /// Test multiple Dispose calls - should be safe
        /// </summary>
        [Test]
        public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
        {
            // Act
            _testComponent.Dispose();
            var firstCallCount = _testComponent.DisposeCallCount;
            
            _testComponent.Dispose();
            var secondCallCount = _testComponent.DisposeCallCount;
            
            // Assert
            Assert.AreEqual(1, firstCallCount, "Dispose should be called once");
            Assert.AreEqual(1, secondCallCount, "Dispose should not be called again");
            Assert.IsTrue(_testComponent.IsDisposed, "Component should remain disposed");
        }

        /// <summary>
        /// Test asynchronous resource disposal
        /// </summary>
        [Test]
        public async Task DisposeAsync_DisposesResourcesAndDestroysGameObject()
        {
            // Act
            await _testComponent.DisposeAsync();
            
            // Assert
            Assert.IsTrue(_testComponent.IsDisposed, "Component should be disposed");
            Assert.IsTrue(_testComponent.IsDestroyed, "Component should be marked as destroyed");
            Assert.IsTrue(_testComponent.AsyncDisposeCoreCalled, "DisposeAsyncCore should be called");
        }

        /// <summary>
        /// Test asynchronous disposal with CancellationToken
        /// </summary>
        [Test]
        public async Task DisposeAsync_WithCancellationToken_PassesTokenToCore()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            
            // Act
            await _testComponent.DisposeAsync(cts.Token);
            
            // Assert
            Assert.IsTrue(_testComponent.IsDisposed, "Component should be disposed");
            Assert.IsTrue(_testComponent.AsyncDisposeCoreCalled, "DisposeAsyncCore should be called");
            Assert.AreEqual(cts.Token, _testComponent.ReceivedToken, "CancellationToken should be passed to DisposeAsyncCore");
        }

        /// <summary>
        /// Test that OnDestroy automatically triggers resource disposal
        /// </summary>
        [Test]
        public void OnDestroy_AutomaticallyDisposesResources()
        {
            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            
            // Assert
            Assert.IsTrue(_testComponent.IsDisposed, "Component should be disposed after OnDestroy");
            Assert.IsTrue(_testComponent.IsDestroyed, "Component should be marked as destroyed");
        }

        /// <summary>
        /// Test that OnDestroy doesn't trigger repeated disposal if already disposed
        /// </summary>
        [Test]
        public void OnDestroy_AfterDispose_DoesNotDisposeAgain()
        {
            // Arrange
            _testComponent.Dispose();
            var disposeCallCountAfterExplicitDispose = _testComponent.DisposeCallCount;
            
            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            var disposeCallCountAfterDestroy = _testComponent.DisposeCallCount;
            
            // Assert
            Assert.AreEqual(disposeCallCountAfterExplicitDispose, disposeCallCountAfterDestroy,
                "OnDestroy should not call dispose again if already disposed");
        }

        /// <summary>
        /// Test checking initial component state
        /// </summary>
        [Test]
        public void InitialState_IsNotDisposed()
        {
            // Assert
            Assert.IsFalse(_testComponent.IsDisposed, "Component should not be disposed initially");
            Assert.IsFalse(_testComponent.IsDestroyed, "Component should not be destroyed initially");
        }

        /// <summary>
        /// Test cancellation of asynchronous disposal
        /// </summary>
        [Test]
        public void DisposeAsync_WithCancelledToken_ThrowsTaskCanceledException()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(
                async () => await _testComponent.DisposeAsync(cts.Token));
        }

        /// <summary>
        /// Test that disposeCancellationToken is accessible and not cancelled before disposal
        /// </summary>
        [Test]
        public void DisposeCancellationToken_BeforeDispose_IsNotCancelled()
        {
            // Arrange & Act
            var token = _testComponent.GetDisposeCancellationToken();
            
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
            var token = _testComponent.GetDisposeCancellationToken(); // Get token before disposal
            
            // Act
            _testComponent.Dispose();
            
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
            var token = _testComponent.GetDisposeCancellationToken();
            
            // Act
            await _testComponent.DisposeAsync();
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Cancellation token should be cancelled after async disposal");
        }

        /// <summary>
        /// Test that disposeCancellationToken is cancelled when GameObject is destroyed
        /// </summary>
        [Test]
        public void DisposeCancellationToken_AfterOnDestroy_IsCancelled()
        {
            // Arrange
            var token = _testComponent.GetDisposeCancellationToken();
            
            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Cancellation token should be cancelled after OnDestroy");
        }
        
        /// <summary>
        /// Test that destroyCancellationToken is accessible and not cancelled before destruction
        /// </summary>
        [Test]
        public void DestroyCancellationToken_BeforeDestroy_IsNotCancelled()
        {
            // Arrange & Act
            var token = _testComponent.GetDestroyCancellationToken();
            
            // Assert
            Assert.IsFalse(token.IsCancellationRequested, "Destroy cancellation token should not be cancelled before destruction");
        }
        
        /// <summary>
        /// Test that destroyCancellationToken supports registrations without throwing
        /// </summary>
        [Test]
        public void DestroyCancellationToken_Register_DoesNotThrow()
        {
            // Arrange
            var token = _testComponent.GetDestroyCancellationToken();
            
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                using var registration = token.Register(() => { });
            });
        }
        
        /// <summary>
        /// Test that destroyCancellationToken is cancelled after GameObject destruction
        /// </summary>
        [Test]
        public void DestroyCancellationToken_AfterDestroy_IsCancelled()
        {
            // Arrange
            var token = _testComponent.GetDestroyCancellationToken();
            
            // Act
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Destroy cancellation token should be cancelled after destruction");
        }
        
        /// <summary>
        /// Test WaitForDisposalAsync completes when disposed
        /// </summary>
        [Test]
        public async Task WaitForDisposalAsync_CompletesWhenDisposed()
        {
            // Arrange
            var waitTask = _testComponent.WaitForDisposalAsync();
            
            // Assert - task should not be completed before disposal
            Assert.IsFalse(waitTask.IsCompleted, "Wait task should not be completed before disposal");
            
            // Act - dispose the component
            _testComponent.Dispose();
            
            // Assert - task should complete after disposal
            await waitTask;
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should complete after disposal");
        }
        
        /// <summary>
        /// Test WaitForDestroyAsync completes when GameObject is destroyed
        /// </summary>
        [Test]
        public async Task WaitForDestroyAsync_CompletesWhenDestroyed()
        {
            // Arrange
            var waitTask = _testComponent.WaitForDestroyAsync();
            
            // Assert - task should not be completed before destruction
            Assert.IsFalse(waitTask.IsCompleted, "Wait task should not be completed before destruction");
            
            // Act - destroy the GameObject
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            
            // Assert - task should complete after destruction
            await waitTask;
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should complete after destruction");
        }
        
        /// <summary>
        /// Test WaitForDisposalAsync returns immediately if already disposed
        /// </summary>
        [Test]
        public async Task WaitForDisposalAsync_ReturnsImmediatelyIfAlreadyDisposed()
        {
            // Arrange
            _testComponent.Dispose();
            
            // Act
            var waitTask = _testComponent.WaitForDisposalAsync();
            
            // Assert
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should be completed immediately if already disposed");
            await waitTask; // Should not block
        }
        
        /// <summary>
        /// Test WaitForDestroyAsync returns immediately if already destroyed
        /// </summary>
        [Test]
        public async Task WaitForDestroyAsync_ReturnsImmediatelyIfAlreadyDestroyed()
        {
            // Arrange
            UnityEngine.Object.DestroyImmediate(_testGameObject);
            
            // Act
            var waitTask = _testComponent.WaitForDestroyAsync();
            
            // Assert
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should be completed immediately if already destroyed");
            await waitTask; // Should not block
        }
    }

            /// <summary>
        /// Test implementation of MonoBehaviourDisposable for functionality verification
        /// </summary>
    public class TestMonoBehaviourDisposable : MonoBehaviourDisposable
    {
        public bool ManagedResourcesDisposed { get; private set; }
        public bool UnmanagedResourcesDisposed { get; private set; }
        public bool AsyncDisposeCoreCalled { get; private set; }
        public int DisposeCallCount { get; private set; }
        public CancellationToken ReceivedToken { get; private set; }
        
        // Expose protected members for testing
        public CancellationToken GetDisposeCancellationToken() => disposeCancellationToken;
        public CancellationToken GetDestroyCancellationToken() => destroyCancellationToken;

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

        protected override async ValueTask DisposeAsyncCore(CancellationToken token, bool _)
        {
            AsyncDisposeCoreCalled = true;
            ReceivedToken = token;
            
            // Simulate asynchronous work
            await Task.Delay(1, token);
        }
    }
}