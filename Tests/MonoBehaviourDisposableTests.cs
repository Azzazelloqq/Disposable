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