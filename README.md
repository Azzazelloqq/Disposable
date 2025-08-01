# Disposable Module

Модуль для управления ресурсами в Unity с использованием паттерна Disposable. Предоставляет удобные инструменты для корректного освобождения managed и unmanaged ресурсов, включая поддержку асинхронного освобождения.

## Особенности

- ✅ Поддержка синхронного и асинхронного освобождения ресурсов
- ✅ Composite паттерн для управления группами disposable объектов
- ✅ Базовые классы для легкой реализации IDisposable
- ✅ Интеграция с Unity MonoBehaviour
- ✅ Thread-safe реализация
- ✅ Поддержка CancellationToken для асинхронных операций
- ✅ Расширения для работы с коллекциями

## Структура модуля

### Основные компоненты

#### `ICompositeDisposable`
Интерфейс для управления группой disposable ресурсов.

```csharp
public interface ICompositeDisposable : IDisposable, IAsyncDisposable
{
    void AddDisposable(IDisposable disposable);
    void AddDisposable(IAsyncDisposable disposable);
    void AddDisposable(DisposableBase disposable);
    ValueTask DisposeAsync(CancellationToken token, bool continueOnCapturedContext = false);
}
```

#### `CompositeDisposable`
Основная реализация composite паттерна для управления множеством disposable ресурсов.

**Особенности:**
- Поддержка `IDisposable`, `IAsyncDisposable` и `DisposableBase` объектов
- Возможность добавления 1, 2, 3 или коллекции объектов за раз
- Настраиваемая начальная ёмкость для оптимизации памяти
- Thread-safe освобождение ресурсов

```csharp
var composite = new CompositeDisposable(capacity: 20);

// Добавление различных типов disposable объектов
composite.AddDisposable(fileStream);
composite.AddDisposable(httpClient, database);
composite.AddDisposable(new[] { resource1, resource2, resource3 });

// Синхронное освобождение всех ресурсов
composite.Dispose();

// Асинхронное освобождение с поддержкой отмены
await composite.DisposeAsync(cancellationToken);
```

#### `DisposableBase`
Абстрактный базовый класс для реализации IDisposable паттерна.

**Особенности:**
- Thread-safe реализация с использованием `Interlocked`
- Поддержка финализатора
- Защита от повторного вызова Dispose
- Разделение managed и unmanaged ресурсов
- Поддержка асинхронного освобождения

```csharp
public class MyResource : DisposableBase
{
    private FileStream _fileStream;
    private IntPtr _unmanagedHandle;

    protected override void DisposeManagedResources()
    {
        _fileStream?.Dispose();
    }

    protected override void DisposeUnmanagedResources()
    {
        if (_unmanagedHandle != IntPtr.Zero)
        {
            NativeMethods.ReleaseHandle(_unmanagedHandle);
            _unmanagedHandle = IntPtr.Zero;
        }
    }

    protected override async ValueTask DisposeAsyncCore(CancellationToken token)
    {
        if (_fileStream != null)
        {
            await _fileStream.DisposeAsync();
        }
    }
}
```

#### `MonoBehaviourDisposable`
Базовый класс для Unity MonoBehaviour с поддержкой IDisposable.

**Особенности:**
- Автоматическое освобождение при вызове OnDestroy
- Встроенный CompositeDisposable для управления дочерними ресурсами
- Синхронизация между Unity lifecycle и IDisposable
- Поддержка асинхронного освобождения

```csharp
public class GameManager : MonoBehaviourDisposable
{
    private DatabaseConnection _database;
    private NetworkClient _network;

    protected override void Start()
    {
        _database = new DatabaseConnection();
        _network = new NetworkClient();
        
        // Добавляем ресурсы в composite для автоматического освобождения
        compositeDisposable.AddDisposable(_database, _network);
    }

    protected override void DisposeManagedResources()
    {
        // Дополнительная логика освобождения
        PlayerPrefs.Save();
    }
}
```

### Утилиты

#### `CollectionsExtensions`
Расширения для работы с коллекциями disposable объектов.

```csharp
// Синхронное освобождение
List<IDisposable> resources = GetResources();
resources.DisposeAll(); // Освобождает все и очищает список

IDisposable[] array = GetResourceArray();
array.DisposeAll(); // Освобождает все и обнуляет элементы

// Асинхронное освобождение
IEnumerable<IAsyncDisposable> asyncResources = GetAsyncResources();
await asyncResources.DisposeAllAsync(cancellationToken);
```

## Примеры использования

### Базовое использование CompositeDisposable

```csharp
public class ServiceManager : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    public void Initialize()
    {
        var dbConnection = new DatabaseConnection();
        var httpClient = new HttpClient();
        var fileWatcher = new FileSystemWatcher();
        
        _disposables.AddDisposable(dbConnection, httpClient, fileWatcher);
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
```

### Асинхронное освобождение с отменой

```csharp
public class AsyncServiceManager : IAsyncDisposable
{
    private readonly CompositeDisposable _disposables = new();
    
    public async ValueTask DisposeAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        
        try
        {
            await _disposables.DisposeAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Логирование timeout
            Debug.LogWarning("Dispose operation timed out");
            _disposables.Dispose(); // Fallback к синхронному освобождению
        }
    }
}
```

### Unity интеграция

```csharp
public class UIManager : MonoBehaviourDisposable
{
    [SerializeField] private Button[] _buttons;
    private readonly List<IDisposable> _subscriptions = new();
    
    protected override void Start()
    {
        foreach (var button in _buttons)
        {
            var subscription = button.OnClickAsObservable()
                .Subscribe(OnButtonClick);
            _subscriptions.Add(subscription);
        }
        
        compositeDisposable.AddDisposable(_subscriptions);
    }
    
    private void OnButtonClick(Unit _)
    {
        // Обработка клика
    }
}
```

## Рекомендации по использованию

### Performance
- Используйте подходящую начальную ёмкость для `CompositeDisposable` если знаете примерное количество ресурсов
- Предпочитайте группированное добавление ресурсов (`AddDisposable(a, b, c)`) вместо множественных вызовов
- Для критичных по производительности участков используйте `CollectionsExtensions.DisposeAll()` для массивов

### Thread Safety
- `CompositeDisposable` и `DisposableBase` являются thread-safe
- Будьте осторожны при доступе к ресурсам после вызова Dispose
- Используйте `IsDisposed` в `DisposableBase` для проверки состояния

### Unity Specifics
- `MonoBehaviourDisposable` автоматически освобождает ресурсы при уничтожении GameObject
- Избегайте блокирующих операций в `DisposeManagedResources()`
- Для долгих асинхронных операций используйте `DisposeAsyncCore()` с CancellationToken

### Memory Management
- Всегда вызывайте `Dispose()` или `DisposeAsync()` для корректного освобождения ресурсов
- Используйте `using` statements где это возможно
- Не забывайте про unmanaged ресурсы в `DisposeUnmanagedResources()`

## Версионность

**Текущая версия:** 1.0.2

### Changelog
- v1.0.2: Улучшения производительности и стабильности
- v1.0.1: Добавлена поддержка асинхронных операций
- v1.0.0: Первый релиз

## Требования

- **Unity:** 2020.3+
- **C#:** 8.0+
- **.NET Standard:** 2.1

## Лицензия

Смотрите [LICENSE](LICENSE) файл для подробностей.

## Автор

**Azzazelloqq**
- Email: bog22232@ya.ru
- GitHub: [Azzazelloqq](https://github.com/Azzazelloqq)