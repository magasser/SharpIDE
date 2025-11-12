using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace SharpIDE.Application.Features.Logging;

public class InternalTerminalLoggerFactory
{
	public static ILogger CreateLogger()
	{
		var logger = CreateLogger("FORCECONSOLECOLOR", LoggerVerbosity.Minimal);
		return logger;
	}

	private static ILogger CreateLogger(string parameters, LoggerVerbosity loggerVerbosity)
	{
		string[]? args = [];
		bool supportsAnsi = true;
		bool outputIsScreen = true;
		uint? originalConsoleMode = 0x0007;

		var logger = TerminalLogger.CreateTerminalOrConsoleLogger(args, supportsAnsi, outputIsScreen, originalConsoleMode);

		logger.Parameters = parameters;
		logger.Verbosity = loggerVerbosity;
		return logger;
	}
}
