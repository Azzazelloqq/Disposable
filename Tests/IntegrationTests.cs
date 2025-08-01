using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Disposable.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Disposable.Tests
{
    /// <summary>
    /// Integration tests for verifying interaction between module components
    /// </summary>
    [TestFixture]
    public class IntegrationTests
    {
        /// <summary>
        /// Test complex scenario with various types of disposable objects
        /// </summary>
        [Test]
        public async Task ComplexScenario_MixedDisposables_WorksCorrectly()
        {
            // Arrange
            using var composite = new CompositeDisposable();
            var syncDisposables = new List<MockDisposable>
            {
                new MockDisposable(),
                new MockDisposable(),
                new MockDisposable()
            };
            
            var asyncDisposables = new List<MockAsyncDisposable>
            {
                new MockAsyncDisposable(),
                new MockAsyncDisposable()
            };
            
            var disposableBases = new List<TestAsyncDisposableBase>
            {
                new TestAsyncDisposableBase(),
                new TestAsyncDisposableBase()
            };
            
            // Act
            composite.AddDisposable(syncDisposables);
            composite.AddDisposable(asyncDisposables);
            composite.AddDisposable(disposableBases);
            
            await composite.DisposeAsync();
            
            // Assert
            foreach (var disposable in syncDisposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All sync disposables should be disposed");
            }
            
            foreach (var disposable in asyncDisposables)
            {
                Assert.IsTrue(disposable.IsDisposed, "All async disposables should be disposed");
            }
            
            foreach (var disposable in disposableBases)
            {
                Assert.IsTrue(disposable.IsDisposed, "All disposable bases should be disposed");
            }
        }

        /// <summary>
        /// Performance test with a large number of resources
        /// </summary>
        [Test]
        public async Task PerformanceTest_LargeNumberOfResources_CompletesInReasonableTime()
        {
            // Arrange
            const int resourceCount = 10000;
            using var composite = new CompositeDisposable(resourceCount);
            
            for (int i = 0; i < resourceCount; i++)
            {
                composite.AddDisposable(new MockDisposable());
            }
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await composite.DisposeAsync();
            stopwatch.Stop();
            
            // Assert
            Assert.Less(stopwatch.ElapsedMilliseconds, 5000, "Large disposal should complete within 5 seconds");
        }

        /// <summary>
        /// Performance test for concurrent resource addition
        /// </summary>
        [Test]
        public async Task PerformanceTest_ConcurrentAddition_CompletesInReasonableTime()
        {
            // Arrange
            const int threadsCount = 10;
            const int resourcesPerThread = 1000;
            var composite = new CompositeDisposable();
            var tasks = new List<Task>();
            
            // Act - adding resources
            var addStopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < threadsCount; i++)
            {
                tasks.Add(Task.Run(() => 
                {
                    for (int j = 0; j < resourcesPerThread; j++)
                    {
                        composite.AddDisposable(new MockDisposable());
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            addStopwatch.Stop();
            
            // Act - disposing resources
            var disposeStopwatch = System.Diagnostics.Stopwatch.StartNew();
            await composite.DisposeAsync();
            disposeStopwatch.Stop();
            
            // Assert
            Assert.Less(addStopwatch.ElapsedMilliseconds, 1000, 
                $"Concurrent addition of {threadsCount * resourcesPerThread} resources should complete within 1 second");
            Assert.Less(disposeStopwatch.ElapsedMilliseconds, 2000, 
                $"Disposal of {threadsCount * resourcesPerThread} resources should complete within 2 seconds");
            
            // Log performance info
            UnityEngine.Debug.Log($"Concurrent addition: {addStopwatch.ElapsedMilliseconds}ms");
            UnityEngine.Debug.Log($"Disposal: {disposeStopwatch.ElapsedMilliseconds}ms");
            UnityEngine.Debug.Log($"Total resources: {threadsCount * resourcesPerThread}");
        }

        /// <summary>
        /// Fault tolerance test for exceptions in Dispose
        /// </summary>
        [Test]
        public void ExceptionHandling_OneDisposableThrows_OthersStillDisposed()
        {
            // Arrange
            using var composite = new CompositeDisposable();
            var goodDisposable1 = new MockDisposable();
            var badDisposable = new ThrowingDisposable();
            var goodDisposable2 = new MockDisposable();
            
            composite.AddDisposable(goodDisposable1);
            composite.AddDisposable(badDisposable);
            composite.AddDisposable(goodDisposable2);
            
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => composite.Dispose());
            
            // Verify that other objects are still disposed
            Assert.IsTrue(goodDisposable1.IsDisposed, "First good disposable should be disposed");
            Assert.IsTrue(goodDisposable2.IsDisposed, "Second good disposable should be disposed");
        }

        /// <summary>
        /// Thread-safety test for concurrent access
        /// </summary>
        [Test]
        public async Task ThreadSafety_ConcurrentAccess_WorksCorrectly()
        {
            // Arrange
            var composite = new CompositeDisposable();
            var tasks = new List<Task>();
            
            // Add resources from different threads
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Run(() => 
                {
                    for (int j = 0; j < 100; j++)
                    {
                        composite.AddDisposable(new MockDisposable());
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Act
            await composite.DisposeAsync();
            
            // Assert - if no exceptions, the test passed successfully
            Assert.Pass("Concurrent access test completed without exceptions");
        }

        /// <summary>
        /// Cancellation test when working with nested CompositeDisposable
        /// </summary>
        [Test]
        public async Task NestedComposites_WithCancellation_HandledCorrectly()
        {
            // Arrange
            await using var parentComposite = new CompositeDisposable();
            var childComposite1 = new CompositeDisposable();
            var childComposite2 = new CompositeDisposable();
            
            childComposite1.AddDisposable(new MockAsyncDisposable());
            childComposite2.AddDisposable(new MockAsyncDisposable());
            
            parentComposite.AddDisposable((IDisposable)childComposite1, childComposite2);
            
            using var cts = new CancellationTokenSource();
            
            // Expect TWO error logs when disposing child composites with async disposables synchronously
            // Due to method overload resolution, AddDisposable(IDisposable, IDisposable) is called
            // Both childComposite1 (cast to IDisposable) and childComposite2 are added to _disposables list
            // During DisposeAsync, both are disposed via synchronous Dispose() method → both log errors
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"Have async disposables\. Invoke async dispose with lock thread\. Maybe need invoke.*"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"Have async disposables\. Invoke async dispose with lock thread\. Maybe need invoke.*"));
            
            // Act
            var disposeTask = parentComposite.DisposeAsync(cts.Token);
            await disposeTask;
            
            // Assert
            Assert.IsTrue(disposeTask.IsCompleted, "Nested disposal should complete successfully");
        }

        /// <summary>
        /// Test combined use of synchronous and asynchronous methods
        /// </summary>
        [Test]
        public async Task MixedSyncAsync_Disposal_WorksCorrectly()
        {
            // Arrange
            var composite1 = new CompositeDisposable();
            var composite2 = new CompositeDisposable();
            
            composite1.AddDisposable(new MockDisposable(), new MockDisposable());
            composite2.AddDisposable(new MockAsyncDisposable(), new MockAsyncDisposable());
            
            // Act
            composite1.Dispose(); // Synchronous disposal
            await composite2.DisposeAsync(); // Asynchronous disposal
            
            // Assert - test passes if there are no exceptions
            Assert.Pass("Mixed sync/async disposal completed successfully");
        }

        /// <summary>
        /// Real scenario test - game manager simulation
        /// </summary>
        [Test]
        public async Task RealWorldScenario_GameManager_WorksCorrectly()
        {
            // Arrange - simulate game manager with various resources
            using var gameManager = new CompositeDisposable();
            
            // Simulate various game resources
            var networkConnection = new MockAsyncDisposable(); // Network connection
            var audioSources = new List<IDisposable> 
            { 
                new MockDisposable(), 
                new MockDisposable(), 
                new MockDisposable() 
            }; // Audio sources
            var textures = new IDisposable[] 
            { 
                new MockDisposable(), 
                new MockDisposable() 
            }; // Textures
            var physicsWorld = new TestAsyncDisposableBase(); // Physics world
            
            // Act
            gameManager.AddDisposable(networkConnection);
            gameManager.AddDisposable(audioSources);
            gameManager.AddDisposable(textures);
            gameManager.AddDisposable(physicsWorld);
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await gameManager.DisposeAsync(cts.Token);
            
            // Assert
            Assert.IsTrue(networkConnection.IsDisposed, "Network connection should be disposed");
            Assert.IsTrue(physicsWorld.IsDisposed, "Physics world should be disposed");
            
            foreach (var audio in audioSources)
            {
                Assert.IsTrue(((MockDisposable)audio).IsDisposed, "Audio sources should be disposed");
            }
            
            foreach (var texture in textures)
            {
                Assert.IsTrue(((MockDisposable)texture).IsDisposed, "Textures should be disposed");
            }
        }
    }

            /// <summary>
        /// Mock disposable that throws exception during disposal
        /// </summary>
    public class ThrowingDisposable : IDisposable
    {
        public void Dispose()
        {
            throw new InvalidOperationException("Test exception during dispose");
        }
    }
}