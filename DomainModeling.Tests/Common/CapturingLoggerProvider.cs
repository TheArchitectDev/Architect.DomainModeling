using Microsoft.Extensions.Logging;

namespace Architect.DomainModeling.Tests.Common;

public sealed class CapturingLoggerProvider : ILoggerProvider
{
	public IReadOnlyList<string> Logs => this._logs;
	private readonly List<string> _logs = [];

	public ILogger CreateLogger(string categoryName)
	{
		return new CapturingLogger(this._logs);
	}

	public void Dispose()
	{
	}

	public sealed class CapturingLogger(
		List<string> logs)
		: ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return null;
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return true;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			logs.Add($"[{logLevel}] {formatter(state, exception)}");
		}
	}
}
