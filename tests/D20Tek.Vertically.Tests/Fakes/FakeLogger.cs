namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>Captured log entry for assertions.</summary>
public readonly record struct LogEntry(LogLevel Level, string Message);

/// <summary>
/// In-memory logger that captures entries and honors a configurable minimum enabled level so
/// tests can verify the IsEnabled guards in the built-in behaviors.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FakeLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = [];

    public LogLevel MinLevel { get; set; } = LogLevel.Trace;

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= MinLevel && logLevel != LogLevel.None;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        _entries.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

