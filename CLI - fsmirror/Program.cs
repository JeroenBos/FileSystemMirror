using System;
using System.IO;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;

// Override some default messages:
#if false  // can be enabled with new version of System.CommandLine
using fsmirror;
MyResources.Initialize();
#endif

// the CLI arguments and options
var arguments = new Symbol[]
{
	new Argument<DirectoryInfo>("source").With
	(
		alias: "--source",
		description: "The base path to mirror from, to which patterns are relative"
	),
	new Argument<DirectoryInfo>("destination").With
	(
		alias: "--dest",
		description: "The destination path to reflect (i.e. copy) to"
	),
	new Argument<string>("patterns").With
	(
		alias: "--patterns",
		description: $"Patterns of files to mirror, `{Path.PathSeparator}` separated. Starting with `!` ignores the path. Accepted wildcards are `**`, `*` and `?`. Defaults to `*`",
		defaultValue: "*"
	),
	// non-positional arguments (i.e. options):
	new Option<bool>("--mirrorDeletions",
		description: "Whether to delete matching files at the destination if they're deleted at the source",
		getDefaultValue: () => false
	),
	new Option<string>("--logfile",
		description: "The path of the file to log to. Special accepted values is `stdout`",
		getDefaultValue: getLoggerPath
	),
	new Option<string?>("--tag",
		description: "A tag to identify a fsmirror process"
	)
};

return new RootCommand("Copies all files matching patterns on modification/creation from source to dest")
{
	Handler = CommandHandler.Create<DirectoryInfo, DirectoryInfo, string, bool, string, string?, CancellationToken>(main),
	Name = "fsmirror",
}.With(arguments).InvokeAsync(args).Result;

string getLoggerPath()
{
	string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
	string appfolder = OperatingSystem.IsWindows() ? "fsmirror" : ".fsmirror";

	return Path.Combine(appdata, appfolder, "log.txt");
}

ILogger getLogger(string logfile)
{
	if (logfile.Trim() == "stdout")
	{
		return new StdoutLogger();
	}
	return new FileLogger(getLoggerPath());
}

/// <param name="cancellationToken"> Canceled on e.g. process exit or Ctrl+C events. </param>
void main(DirectoryInfo source, DirectoryInfo destination, string patterns, bool mirrorDeletions, string logfile, string? tag, CancellationToken cancellationToken)
{

	var logger = getLogger(logfile);
	logger.TryLog($"Start up {(tag == null ? "" : tag + " ")}:(source=`{source.FullName}`, dest=`{destination.FullName}`, patterns=`{patterns}`, mirrorDeletions={mirrorDeletions}, version={CommandLineBuilderExtensions.AssemblyVersion})");

	try
	{
		cancellationToken.ThrowIfCancellationRequested();

		using (FileSystemMirror.Mirror(source.FullName, destination.FullName, patterns, mirrorDeletions, logger, cancellationToken: cancellationToken))
		{
			while (true)
				cancellationToken.ThrowIfCancellationRequested();
		}
	}
	catch (OperationCanceledException)
	{
	}
	finally
	{
		logger.TryLog($"Stopping {tag}");
	}
}
