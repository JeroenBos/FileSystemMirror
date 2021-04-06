using static JBSnorro.Globals;
using JBSnorro;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


public static class FileSystemMirror
{

	public static IDisposable Mirror(string sourcePath,
									 string destPath,
									 string compositePatterns,
									 bool mirrorDeletions = true,
									 ILogger? logger = null,
									 RecoveryStrategy? recoveryStrategy = null,
									 CancellationToken cancellationToken = default)
	{
		var (sourcePatterns, sourceIgnorePatterns) = DecomposePatterns(compositePatterns);
		return Mirror(sourcePath, destPath, sourcePatterns, sourceIgnorePatterns, mirrorDeletions, recoveryStrategy, logger, cancellationToken);
	}
	/// <summary>
	/// Mirrors a globbed source file system to a destination file system.
	/// </summary>
	public static IDisposable Mirror(string sourcePath,
									 string destPath,
									 IEnumerable<string>? sourcePatterns = null,
									 IEnumerable<string>? sourceIgnorePatterns = null,
									 bool mirrorDeletions = true,
									 RecoveryStrategy? recoveryStrategy = null,
									 ILogger? logger = null,
									 CancellationToken cancellationToken = default)
	{
		if (Path.GetFullPath(sourcePath).StartsWith(Path.GetFullPath(destPath)))
			throw new ArgumentException("The destination path cannot be (non-strictly) nested in the source path");

		sourcePatterns ??= new string[] { "**", "**/" };

		if (recoveryStrategy is null)
		{
			return FileSystemHook.Hook(sourcePath,
						sourcePatterns,
						sourceIgnorePatterns,
						onCreated: createCallback(CopyFile),
						onDeleted: mirrorDeletions ? createCallback(DeleteFile) : null,
						onModified: createCallback(CopyFile),
						triggerOnCreatedOnExistingFiles: true,
						cancellationToken: cancellationToken);
		}
		else
		{
			return FileSystemHook.RecoverableHook(
						recoveryStrategy,
						sourcePath,
						sourcePatterns,
						sourceIgnorePatterns,
						onCreated: createCallback(CopyFile),
						onDeleted: mirrorDeletions ? createCallback(DeleteFile) : null,
						onModified: createCallback(CopyFile),
						cancellationToken: cancellationToken
				);
		}


		FileSystemEventHandler createCallback(Action<string> action)
		{
			void ErrorHandlerAndLogger(object sender, FileSystemEventArgs e)
			{
				logEntry(e);
				// don't await the task. Trigger asyncronously. We're on the "event loop thread"
				Task.Run(() =>
				{
					try
					{
						action(e.FullPath);
					}
					catch (Exception ex)
					{
						logFailure(e, ex);
						return;
					}
					logSuccess(e);
				});
			};

			return ErrorHandlerAndLogger;
		}

		void ensureDirectoryExists(string path)
		{
			string dir = Path.GetDirectoryName(path)!;
			Directory.CreateDirectory(dir);
		}

		void CopyFile(string fullPath)
		{
			string relativePath = ToRelativePath(fullPath, sourcePath);
			string dest = Path.Combine(destPath, relativePath);

			ensureDirectoryExists(dest);

			File.Copy(fullPath, dest, overwrite: true);
		}

		void DeleteFile(string fullPath)
		{
			string relativePath = ToRelativePath(fullPath, sourcePath);
			string dest = Path.Combine(destPath, relativePath);

			File.Delete(dest);
		}

		void logEntry(FileSystemEventArgs e)
		{
			try
			{
				logger?.LogEntry(e);
			}
			catch
			{
			}
		}
		void logFailure(FileSystemEventArgs e, Exception ex)
		{
			try
			{
				logger?.LogFailure(e, ex);
			}
			catch
			{
			}
		}
		void logSuccess(FileSystemEventArgs e)
		{
			try
			{
				logger?.LogSuccess(e);
			}
			catch
			{
			}
		}
	}
}
