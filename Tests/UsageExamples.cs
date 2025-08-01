using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Disposable.Utils;
using NUnit.Framework;

namespace Disposable.Tests
{
    /// <summary>
/// Examples of using the Disposable module in real scenarios
/// </summary>
    [TestFixture]
    public class UsageExamples
    {
        /// <summary>
        /// Example 1: Basic usage of CompositeDisposable
        /// </summary>
        [Test]
        public void Example1_BasicCompositeUsage()
        {
            // Create resource manager
            using var resourceManager = new CompositeDisposable();
            
            // Add various resources
            var fileStream = new MockDisposable(); // FileStream simulation
            var httpClient = new MockDisposable();  // HttpClient simulation
            var timer = new MockDisposable();       // Timer simulation
            
            resourceManager.AddDisposable(fileStream, httpClient, timer);
            
            // Resources are automatically disposed when exiting the using block
            
            // Check the result
            Assert.Pass("Basic composite usage example completed");
        }

        /// <summary>
        /// Example 2: Asynchronous resource disposal
        /// </summary>
        [Test]
        public async Task Example2_AsyncDisposal()
        {
            var serviceManager = new AsyncServiceManager();
            
            // Initialize asynchronous services
            await serviceManager.InitializeAsync();
            
            // Dispose resources asynchronously
            await serviceManager.DisposeAsync();
            
            Assert.IsTrue(serviceManager.IsDisposed, "Service manager should be disposed");
        }

        /// <summary>
        /// Example 3: Usage with collections
        /// </summary>
        [Test]
        public void Example3_CollectionsExtensions()
        {
            // Create collection of resources
            var resources = new List<IDisposable>
            {
                new MockDisposable(),
                new MockDisposable(),
                new MockDisposable()
            };
            
            // Dispose all resources and clear collection in one line
            resources.DisposeAll();
            
            Assert.AreEqual(0, resources.Count, "Collection should be empty after DisposeAll");
        }

        /// <summary>
        /// Example 4: Creating custom Disposable class
        /// </summary>
        [Test]
        public async Task Example4_CustomDisposableClass()
        {
            var databaseManager = new DatabaseManager();
            
            // Use the resource
            await databaseManager.ExecuteQueryAsync("SELECT * FROM Users");
            
            // Dispose resources
            await databaseManager.DisposeAsync();
            
            Assert.IsTrue(databaseManager.IsDisposed, "Database manager should be disposed");
        }

        /// <summary>
        /// Example 5: Handling operation cancellation
        /// </summary>
        [Test]
        public async Task Example5_CancellationHandling()
        {
            var longRunningService = new LongRunningService();
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            
            try
            {
                await longRunningService.DisposeAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Log timeout and fallback to synchronous disposal
                longRunningService.Dispose();
            }
            
            Assert.IsTrue(longRunningService.IsDisposed, "Service should be disposed even after cancellation");
        }

        /// <summary>
        /// Example 6: Pattern for Unity game objects
        /// </summary>
        [Test]
        public async Task Example6_UnityGameObjectPattern()
        {
            // This is an example of how to use in Unity
            var gameObjectManager = new GameObjectManager();
            
            // Add various components and resources
            gameObjectManager.AddAudioSource(new MockDisposable());
            gameObjectManager.AddTexture(new MockDisposable());
            gameObjectManager.AddNetworkComponent(new MockAsyncDisposable());
            
            // Dispose all resources
            await gameObjectManager.DisposeAsync();
            
            Assert.IsTrue(gameObjectManager.IsDisposed, "Game object manager should be disposed");
        }
    }

    #region Example Classes

            /// <summary>
        /// Example of asynchronous service manager
        /// </summary>
    public class AsyncServiceManager : IAsyncDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public bool IsDisposed { get; private set; }

        public async Task InitializeAsync()
        {
            // Simulate initialization of asynchronous services
            var databaseService = new MockAsyncDisposable();
            var apiService = new MockAsyncDisposable();
            var cacheService = new MockAsyncDisposable();
            
            _disposables.AddDisposable(databaseService, apiService, cacheService);
            
            await Task.Delay(10); // Initialization simulation
        }

        public async ValueTask DisposeAsync()
        {
            if (IsDisposed) return;
            
            await _disposables.DisposeAsync();
            IsDisposed = true;
            
            GC.SuppressFinalize(this);
        }
    }

            /// <summary>
        /// Example of database manager
        /// </summary>
    public class DatabaseManager : DisposableBase
    {
        private MockAsyncDisposable _connection;
        private MockDisposable _transactionManager;

        public DatabaseManager()
        {
            _connection = new MockAsyncDisposable();
            _transactionManager = new MockDisposable();
        }

        public async Task<string> ExecuteQueryAsync(string query)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(DatabaseManager));
            
            await Task.Delay(1); // Query execution simulation
            return "Query result";
        }

        protected override async ValueTask DisposeAsyncCore(CancellationToken token, bool _)
        {
            if (_connection != null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }

        protected override void DisposeManagedResources()
        {
            _transactionManager?.Dispose();
            _transactionManager = null;
        }
    }

            /// <summary>
        /// Example of service with long disposal time
        /// </summary>
    public class LongRunningService : IAsyncDisposable, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public async ValueTask DisposeAsync(CancellationToken token = default)
        {
            if (IsDisposed) return;
            
            // Simulate long disposal operation
            for (int i = 0; i < 10; i++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(100, token);
            }
            
            IsDisposed = true;
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            
            // Synchronous disposal as fallback
            IsDisposed = true;
        }

        public ValueTask DisposeAsync()
        {
            return DisposeAsync(CancellationToken.None);
        }
    }

            /// <summary>
        /// Example of game object manager
        /// </summary>
    public class GameObjectManager : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        public bool IsDisposed { get; private set; }

        public void AddAudioSource(IDisposable audioSource)
        {
            _disposables.AddDisposable(audioSource);
        }

        public void AddTexture(IDisposable texture)
        {
            _disposables.AddDisposable(texture);
        }

        public void AddNetworkComponent(IAsyncDisposable networkComponent)
        {
            _disposables.AddDisposable(networkComponent);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            
            _disposables.Dispose();
            IsDisposed = true;
            
            GC.SuppressFinalize(this);
        }
        
        public async Task DisposeAsync()
        {
            if (IsDisposed) return;
            
            await _disposables.DisposeAsync();
            IsDisposed = true;
            
            GC.SuppressFinalize(this);
        }
    }

    #endregion
}