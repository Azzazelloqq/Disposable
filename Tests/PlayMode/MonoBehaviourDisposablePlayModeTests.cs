using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Disposable.Tests.PlayMode
{
    /// <summary>
    /// Play Mode tests for MonoBehaviourDisposable that verify real Unity destruction behavior.
    /// These tests require Play Mode because OnDestroy is not called by DestroyImmediate in Edit Mode.
    /// </summary>
    [TestFixture]
    public class MonoBehaviourDisposablePlayModeTests
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
                Object.Destroy(_testGameObject);
            }
        }

        /// <summary>
        /// Test that OnDestroy is called and resources are disposed when GameObject is destroyed
        /// </summary>
        [UnityTest]
        public IEnumerator Destroy_CallsOnDestroy_AndDisposesResources()
        {
            // Act
            Object.Destroy(_testGameObject);
            yield return null; // Wait for OnDestroy to be called
            
            // Assert
            Assert.IsTrue(_testComponent.IsDisposed, "Component should be disposed after destruction");
            Assert.IsTrue(_testComponent.IsDestroyed, "Component should be marked as destroyed");
        }

        /// <summary>
        /// Test that destroyCancellationToken is cancelled when GameObject is destroyed
        /// </summary>
        [UnityTest]
        public IEnumerator Destroy_CancelsDestroyCancellationToken()
        {
            // Arrange
            var token = _testComponent.GetDestroyCancellationToken();
            Assert.IsFalse(token.IsCancellationRequested, "Token should not be cancelled before destruction");
            
            // Act
            Object.Destroy(_testGameObject);
            yield return null; // Wait for OnDestroy to be called
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Destroy cancellation token should be cancelled after destruction");
        }

        /// <summary>
        /// Test that disposeCancellationToken is cancelled when GameObject is destroyed
        /// </summary>
        [UnityTest]
        public IEnumerator Destroy_CancelsDisposeCancellationToken()
        {
            // Arrange
            var token = _testComponent.GetDisposeCancellationToken();
            Assert.IsFalse(token.IsCancellationRequested, "Token should not be cancelled before destruction");
            
            // Act
            Object.Destroy(_testGameObject);
            yield return null; // Wait for OnDestroy to be called
            
            // Assert
            Assert.IsTrue(token.IsCancellationRequested, "Dispose cancellation token should be cancelled after destruction");
        }

        /// <summary>
        /// Test that WaitForDestroyAsync completes when GameObject is destroyed
        /// </summary>
        [UnityTest]
        public IEnumerator Destroy_CompletesWaitForDestroyAsync()
        {
            // Arrange
            var waitTask = _testComponent.WaitForDestroyAsync();
            Assert.IsFalse(waitTask.IsCompleted, "Wait task should not be completed before destruction");
            
            // Act
            Object.Destroy(_testGameObject);
            yield return null; // Wait for OnDestroy to be called
            
            // Assert
            Assert.IsTrue(waitTask.IsCompleted, "Wait task should complete after destruction");
        }

        /// <summary>
        /// Test that OnDestroy doesn't dispose again if already disposed
        /// </summary>
        [UnityTest]
        public IEnumerator Destroy_AfterDispose_DoesNotDisposeAgain()
        {
            // Arrange
            _testComponent.Dispose();
            var disposeCallCountAfterExplicitDispose = _testComponent.DisposeCallCount;
            
            // Act
            Object.Destroy(_testGameObject);
            yield return null; // Wait for OnDestroy to be called
            var disposeCallCountAfterDestroy = _testComponent.DisposeCallCount;
            
            // Assert
            Assert.AreEqual(disposeCallCountAfterExplicitDispose, disposeCallCountAfterDestroy,
                "OnDestroy should not call dispose again if already disposed");
        }
    }

    /// <summary>
    /// Test implementation of MonoBehaviourDisposable for Play Mode tests
    /// </summary>
    public class TestMonoBehaviourDisposable : MonoBehaviourDisposable
    {
        public bool ManagedResourcesDisposed { get; private set; }
        public bool UnmanagedResourcesDisposed { get; private set; }
        public int DisposeCallCount { get; private set; }
        
        /// <summary>
        /// Exposes the dispose cancellation token for testing
        /// </summary>
        public CancellationToken GetDisposeCancellationToken() => disposeCancellationToken;
        
        /// <summary>
        /// Exposes the destroy cancellation token for testing
        /// </summary>
        public CancellationToken GetDestroyCancellationToken() => destroyCancellationToken;

        /// <inheritdoc/>
        protected override void DisposeManagedResources()
        {
            ManagedResourcesDisposed = true;
        }

        /// <inheritdoc/>
        protected override void DisposeUnmanagedResources()
        {
            UnmanagedResourcesDisposed = true;
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            DisposeCallCount++;
            base.Dispose(disposing);
        }
    }
}

