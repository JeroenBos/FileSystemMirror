using JBSnorro;
using static JBSnorro.Globals;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


// note on namespaces
// JBSnorro namespace is considered generic extensions
// Global namespace in in this project is the FileSystemMirror
// Doesn't matter that it's global because it'll be used in the project fsmirror only anyway

public class FileSystemHook
{
	/// <summary>
	/// Hooks a <see cref="FileSystemWatcher"/> to the specified path with the specified callbacks, and implements globbing filtering.
	/// </summary>
	/// <param name="sourcePatterns">Empty means everything. Patterns ending on a directory separator matches directories only and vice versa. </param>
	public static IFileSystemWatcher Hook(
			string sourcePath,
			IEnumerable<string> sourcePatterns,
			IEnumerable<string>? sourceIgnorePatterns = null,
			FileSystemEventHandler? onCreated = null,
			FileSystemEventHandler? onModified = null,
			FileSystemEventHandler? onDeleted = null,
			ErrorEventHandler? onError = null,
			CancellationToken cancellationToken = default,
			bool triggerOnCreatedOnExistingFiles = false)
	{
		var patterns = sourcePatterns?.ToList() ?? throw new ArgumentNullException(nameof(sourcePatterns));
		var ignorePatterns = sourceIgnorePatterns?.ToArray() ?? Array.Empty<string>();

		if (sourcePatterns.Any(patterns => patterns.ContainsMultiple("**")))
			throw new ArgumentException("Multiple ** aren't supported", nameof(sourcePatterns));


		bool includeSubdirectories = patterns.Count == 0 || sourcePatterns.Any(FileSystemWatcherWithEventMapping.RequiresSubdirectories);

		var watcher = new FileSystemWatcherWithEventMapping(patterns, ignorePatterns)
		{
			Path = sourcePath,
			IncludeSubdirectories = includeSubdirectories,
			NotifyFilter = NotifyFilters.Attributes
						 | NotifyFilters.CreationTime
						 | NotifyFilters.DirectoryName
						 | NotifyFilters.FileName
						 | NotifyFilters.LastAccess
						 | NotifyFilters.LastWrite
						 | NotifyFilters.Security
						 | NotifyFilters.Size
		};

		cancellationToken.Register(() =>
		{
			watcher.EnableRaisingEvents = false;
			watcher.Dispose();
		});


		watcher.Error += onError;
		watcher.Created += onCreated;
		watcher.Changed += onModified;
		watcher.Deleted += onDeleted;

		foreach (var pattern in sourcePatterns)
		{
			watcher.Filters.Add(FileSystemWatcherWithEventMapping.Convert(pattern));
		}

		if (!cancellationToken.IsCancellationRequested)
			watcher.EnableRaisingEvents = true;
		else
			watcher.Dispose();
		return watcher;
	}


	/// <param name="recoveryStrategy"> If you'd like to specify <see cref="RecoveryStrategy.None"/>, instead call <see cref="FileSystemHook.Hook(string, IEnumerable{string}, IEnumerable{string}?, FileSystemEventHandler?, FileSystemEventHandler?, FileSystemEventHandler?, ErrorEventHandler?, CancellationToken, bool)"/>. </param>
	public static IDisposable RecoverableHook(RecoveryStrategy recoveryStrategy,
											  string sourcePath,
											  IEnumerable<string> sourcePatterns,
											  IEnumerable<string>? sourceIgnorePatterns = null,
											  FileSystemEventHandler? onCreated = null,
											  FileSystemEventHandler? onModified = null,
											  FileSystemEventHandler? onDeleted = null,
											  OnError? errorObserver = null,
											  CancellationToken cancellationToken = default)
	{
		if (recoveryStrategy is null)
			throw new ArgumentNullException(nameof(recoveryStrategy));

		var patterns = sourcePatterns?.ToList() ?? throw new ArgumentNullException(nameof(sourcePatterns));
		var ignorePatterns = sourceIgnorePatterns?.ToArray() ?? Array.Empty<string>();

		var retryer = new RecoveringHookRetryer(sourcePath, recoveryStrategy, patterns, ignorePatterns, onCreated, onModified, onDeleted, cancellationToken);
		retryer.Start();
		return retryer.Task;
	}
	class RecoveringHookRetryer : Retryer
	{
		private readonly string sourcePath;
		private readonly IReadOnlyList<string> sourcePatterns;
		private readonly IReadOnlyList<string> sourceIgnorePatterns;
		private readonly FileSystemEventHandler? onCreated;
		private readonly FileSystemEventHandler? onModified;
		private readonly FileSystemEventHandler? onDeleted;

		private readonly List<IFileSystemWatcher> watchers = new List<IFileSystemWatcher>();

		internal RecoveringHookRetryer(string sourcePath,
									   RecoveryStrategy recoveryStrategy,
									   IReadOnlyList<string> sourcePatterns,
									   IReadOnlyList<string> sourceIgnorePatterns,
									   FileSystemEventHandler? onCreated,
									   FileSystemEventHandler? onModified,
									   FileSystemEventHandler? onDeleted,
									   CancellationToken cancellationToken)
			: base(recoveryStrategy, disposeOnError: false, cancellationToken)
		{
			this.sourcePath = sourcePath;
			this.sourcePatterns = sourcePatterns;
			this.sourceIgnorePatterns = sourceIgnorePatterns;
			this.onCreated = onCreated;
			this.onModified = onModified;
			this.onDeleted = onDeleted;
		}


		protected override void Action(IReadOnlyList<Exception> exceptions)
		{
			// this method will be called to attempt stuff
			// exceptions are the consecutive exceptions, both from starting the hook and executing it.
			// although every successful start of the hook clears the exceptions.

			IFileSystemWatcher? watcher;
			if (exceptions.Count == 0)
			{
				watcher = this.Hook();
			}
			else
			{
				watcher = this.HookAncestor(exceptions);
			}

			// TODO: refactor this.watchers to a field instead of list and expose a Task to await the last one
			foreach (var disposable in this.watchers)
				disposable.Dispose();
			this.watchers.Clear();

			if (watcher != null)
			{
				this.watchers.Add(watcher);
			}
		}

		public void Dispose()
		{
			foreach (var disposable in this.watchers)
				disposable.Dispose();


			// disposal in OnCanceled is not necessary because each FileSystemWatcher registers on the cancellation token itself
		}

		private IFileSystemWatcher Hook()
		{
			// a new exceptions list is ok because if this os ever called, that means that the hook was created succesfully
			return FileSystemHook.Hook(sourcePath, sourcePatterns, sourceIgnorePatterns, onCreated, onModified, onDeleted, CreateErrorHandler(new List<Exception>()));
		}

		private IFileSystemWatcher HookAncestor(IReadOnlyList<Exception> exceptions)
		{
			foreach (var (ancestorDir, childName) in recoveryStrategy.HookableParents(sourcePath))
			{
				Assert(childName?.EndsWith(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? true);
				if (ancestorDir.Exists)
				{
					if (childName == null)
					{
						// the sourcePath should exist now
						return Hook();
					}

					return FileSystemHook.Hook(
						sourcePath: ancestorDir.FullName,
						sourcePatterns: new[] { Path.Combine(childName, "**") },
						onCreated: (sender, e) => Start(),
						onError: CreateErrorHandler(exceptions),
						cancellationToken: cancellationToken,
						triggerOnCreatedOnExistingFiles: true);
				}
			}

			throw new Exception("No hookable ancestor exists");
		}
	}
}

