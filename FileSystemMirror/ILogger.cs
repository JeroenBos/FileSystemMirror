using System;
using System.IO;

public interface ILogger
{
	void Log(string s);
	void TryLog(string s)
	{
		try
		{
			Log(s);
		}
		catch
		{ }
	}
	void LogEntry(FileSystemEventArgs e)
	{
		switch (e.ChangeType)
		{
			case WatcherChangeTypes.Created:
				Log($"'{e.FullPath}' created");
				break;
			case WatcherChangeTypes.Deleted:
				Log($"'{e.FullPath}' deleted");
				break;
			case WatcherChangeTypes.Changed:
				Log($"'{e.FullPath}' changed");
				break;
			case WatcherChangeTypes.Renamed:
				Log($"'{e.FullPath}' renamed");
				break;
		}
	}
	void LogFailure(FileSystemEventArgs e, Exception ex)
	{
		Log($"'{e.FullPath}' {(e.ChangeType == WatcherChangeTypes.Deleted ? "deleting" : "copying")} failed:");
		Log(ex.Message);
		if (ex.StackTrace != null)
			Log(ex.StackTrace);
	}
	void LogSuccess(FileSystemEventArgs e)
	{
		if (e.ChangeType == WatcherChangeTypes.Deleted)
			Log($"'{e.FullPath}' deleted successfully");
		else
			Log($"'{e.FullPath}' copied successfully");
	}
}

public class FileLogger : ILogger
{
	private readonly string path;
	public FileLogger(string path)
	{
		if (!Path.IsPathRooted(path))
			throw new ArgumentException("Full path must be provided", nameof(path));

		this.path = path;
	}
	public void Log(string s)
	{
		string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH-mm:ss.fff - ");
		Directory.CreateDirectory(Path.GetDirectoryName(this.path)!);
		File.AppendAllLines(this.path, new[] { timestamp + s });
	}
}

public class StdoutLogger : ILogger
{
	public void Log(string s) => Console.WriteLine(s);
}
