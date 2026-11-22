using System.Text;
using Microsoft.Extensions.Logging;

namespace Animal.IntegrationTests.Api.TestDoubles
{
    public sealed class InMemoryLoggerProvider : ILoggerProvider
    {
        private readonly object _gate = new();
        private readonly List<string> _entries = [];

        public IReadOnlyList<string> Entries
        {
            get
            {
                lock (_gate)
                {
                    return _entries.ToList();
                }
            }
        }

        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(this, categoryName);

        public void Dispose()
        {
        }

        private sealed class InMemoryLogger : ILogger
        {
            private readonly InMemoryLoggerProvider _provider;
            private readonly string _categoryName;

            public InMemoryLogger(InMemoryLoggerProvider provider, string categoryName)
            {
                _provider = provider;
                _categoryName = categoryName;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                var message = formatter(state, exception);
                var line = new StringBuilder()
                    .Append(_categoryName)
                    .Append(" | ")
                    .Append(logLevel)
                    .Append(" | ")
                    .Append(message)
                    .ToString();

                lock (_provider._gate)
                {
                    _provider._entries.Add(line);
                    if (exception is not null)
                    {
                        _provider._entries.Add(exception.ToString());
                    }
                }
            }
        }
    }
}
