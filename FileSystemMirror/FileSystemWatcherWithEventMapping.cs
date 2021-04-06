using JBSnorro;
using static JBSnorro.Globals;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class FileSystemWatcherWithEventMapping : IFileSystemWatcher, IDisposable
{
	/// <summary>
	/// Gets whether the pattern requires to listen for events in subdirectory.
	/// </summary>
	public static bool RequiresSubdirectories(string pattern)
	{
		return pattern.Contains("**")
			|| pattern.Contains(System.IO.Path.DirectorySeparatorChar)
			|| pattern.Contains(System.IO.Path.AltDirectorySeparatorChar);
	}
	/// <summary>
	/// Simplifies a pattern such that it is understood by the native implementation of the file system watcher.
	/// I'm too lazy to build a custom implementation that facades the current <see cref="Filters"/> to modify each entry upon insertion.
	/// Instead, before calling <see cref="Filters.Add(...)"/> just call this method.
	/// </summary>
	public static string Convert(string pattern)
	{
		return System.IO.Path.GetFileName(pattern)
							 .Replace("**", "*")
							 .Trim(DirectorySeparators);
	}

	private readonly FileSystemWatcher watcher = new FileSystemWatcher();
	public IReadOnlyList<string> Patterns { get; }
	public IReadOnlyList<string> IgnorePatterns { get; }
	public FileSystemWatcherWithEventMapping(IReadOnlyList<string> patterns, IReadOnlyList<string> ignorePatterns)
	{
		Patterns = patterns;
		IgnorePatterns = ignorePatterns;
	}
	/// <summary>
	/// 
	/// </summary>
	/// <param name="fullPath"></param>
	/// <param name="ignoreTrailingDirectorySeparator">
	/// Indicates whether the comparison is to ignore whether or not <paramref name="fullPath"/> ends on a directory separator character. 
	/// Null means, try, but if not possible, ignore it. </param>
	/// <returns></returns>
	private bool matches(string fullPath, bool? ignoreTrailingDirectorySeparator = null)
	{
		if (this.Patterns.Count == 0)
			return true;

		string relativePath = ToRelativePath(fullPath, this.Path);


		if (ignoreTrailingDirectorySeparator is false)
		{
			// we could also incorporate the NotifyFilter flags to determine if it could have been one or the other
			bool directory = Directory.Exists(fullPath);
			if (directory)
			{
				relativePath = relativePath.EnsureEndsWithPathSeparator();
			}
		}
		else if (ignoreTrailingDirectorySeparator is null)
		{
			ignoreTrailingDirectorySeparator = !this.watcher.NotifyFilter.HasFlag(NotifyFilters.DirectoryName);
		}


		if (ignoreTrailingDirectorySeparator.Value)
		{
			// TODO: optimize such that these lists don't have to be created
			return GlobPattern.Matches(relativePath.Trim(DirectorySeparators),
									   this.Patterns.Select(TrimDirectorySeparators).ToList(),
									   this.IgnorePatterns.Select(TrimDirectorySeparators).ToList());

			static string TrimDirectorySeparators(string s) => s.Trim(DirectorySeparators);
		}
		else
		{
			return GlobPattern.Matches(relativePath, this.Patterns, this.IgnorePatterns);
		}
	}

	protected FileSystemEventHandler? MapChangedEventHandler(FileSystemEventHandler handler)
	{
		return (sender, e) =>
		{
			if (matches(e.FullPath, ignoreTrailingDirectorySeparator: false))
				handler(sender, e);
		};
	}

	protected FileSystemEventHandler? MapCreatedEventHandler(FileSystemEventHandler handler)
	{
		return (sender, e) =>
		{
			if (matches(e.FullPath, ignoreTrailingDirectorySeparator: false))
				handler(sender, e);
		};
	}

	protected FileSystemEventHandler? MapDeletedEventHandler(FileSystemEventHandler handler)
	{
		return (sender, e) =>
		{
			// in case of a deletion it cannot be determined anymore whether it was a file or directory, hence the null
			if (matches(e.FullPath, ignoreTrailingDirectorySeparator: null))
				handler(sender, e);
		};
	}

	protected RenamedEventHandler? MapRenamedEventHandler(RenamedEventHandler handler)
	{
		throw new NotImplementedException();
	}


	//
	// Summary:
	//     Gets or sets the path of the directory to watch.
	//
	// Returns:
	//     The path to monitor. The default is an empty string ("").
	//
	// Exceptions:
	//   T:System.ArgumentException:
	//     The specified path does not exist or could not be found. -or- The specified path
	//     contains wildcard characters. -or- The specified path contains invalid path characters.
	public string Path
	{
		get => watcher.Path;
		set => watcher.Path = value;
	}

	//
	// Summary:
	//     Gets or sets the type of changes to watch for.
	//
	// Returns:
	//     One of the System.IO.NotifyFilters values. The default is the bitwise OR combination
	//     of LastWrite, FileName, and DirectoryName.
	//
	// Exceptions:
	//   T:System.ArgumentException:
	//     The value is not a valid bitwise OR combination of the System.IO.NotifyFilters
	//     values.
	//
	//   T:System.ComponentModel.InvalidEnumArgumentException:
	//     The value that is being set is not valid.
	public NotifyFilters NotifyFilter
	{
		get => watcher.NotifyFilter;
		set => watcher.NotifyFilter = value;
	}

	//
	// Summary:
	//     Gets or sets a value indicating whether subdirectories within the specified path
	//     should be monitored.
	//
	// Returns:
	//     true if you want to monitor subdirectories; otherwise, false. The default is
	//     false.
	public bool IncludeSubdirectories
	{
		get => watcher.IncludeSubdirectories;
		set => watcher.IncludeSubdirectories = value;
	}
	//
	// Summary:
	//     Gets the collection of all the filters used to determine what files are monitored
	//     in a directory.
	//
	// Returns:
	//     A filter collection.
	public Collection<string> Filters => watcher.Filters;
	//
	// Summary:
	//     Gets or sets the filter string used to determine what files are monitored in
	//     a directory.
	//
	// Returns:
	//     The filter string. The default is "*.*" (Watches all files.)
	public string Filter
	{
		get => watcher.Filter;
		set => watcher.Filter = value;
	}
	//
	// Summary:
	//     Gets or sets a value indicating whether the component is enabled.
	//
	// Returns:
	//     true if the component is enabled; otherwise, false. The default is false. If
	//     you are using the component on a designer in Visual Studio 2005, the default
	//     is true.
	//
	// Exceptions:
	//   T:System.ObjectDisposedException:
	//     The System.IO.FileSystemWatcher object has been disposed.
	//
	//   T:System.PlatformNotSupportedException:
	//     The current operating system is not Microsoft Windows NT or later.
	//
	//   T:System.IO.FileNotFoundException:
	//     The directory specified in System.IO.FileSystemWatcher.Path could not be found.
	//
	//   T:System.ArgumentException:
	//     System.IO.FileSystemWatcher.Path has not been set or is invalid.
	public bool EnableRaisingEvents
	{
		get => watcher.EnableRaisingEvents;
		set => watcher.EnableRaisingEvents = value;
	}

	//
	// Summary:
	//     Occurs when the instance of System.IO.FileSystemWatcher is unable to continue
	//     monitoring changes or when the internal buffer overflows.
	public event ErrorEventHandler? Error
	{
		add => watcher.Error += value;
		remove => watcher.Error -= value;
	}

	private readonly Dictionary<FileSystemEventHandler, FileSystemEventHandler> changedEventHandlers = new();
	//
	// Summary:
	//     Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path
	//     is changed.
	public event FileSystemEventHandler? Changed
	{
		add
		{
			if (value == null)
				return;

			var handler = MapChangedEventHandler(value);
			if (handler != null)
			{
				changedEventHandlers.Add(value, handler);
				watcher.Changed += handler;
			}
		}
		remove
		{
			if (value == null)
				return;

			if (changedEventHandlers.Remove(value, out var handler))
			{
				watcher.Changed -= handler;
			}
		}
	}

	private readonly Dictionary<FileSystemEventHandler, FileSystemEventHandler> createdEventHandlers = new();
	//
	// Summary:
	//     Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path
	//     is created.
	public event FileSystemEventHandler? Created
	{
		add
		{
			if (value == null)
				return;

			var handler = MapCreatedEventHandler(value);
			if (handler != null)
			{
				createdEventHandlers.Add(value, handler);
				watcher.Created += handler;
			}
		}
		remove
		{
			if (value == null)
				return;

			if (createdEventHandlers.Remove(value, out var handler))
			{
				watcher.Created -= handler;
			}
		}
	}

	private readonly Dictionary<FileSystemEventHandler, FileSystemEventHandler> deletedEventHandlers = new();
	//
	// Summary:
	//     Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path
	//     is deleted.
	public event FileSystemEventHandler? Deleted
	{
		add
		{
			if (value == null)
				return;

			var handler = MapDeletedEventHandler(value);
			if (handler != null)
			{
				deletedEventHandlers.Add(value, handler);
				watcher.Deleted += handler;
			}
		}
		remove
		{
			if (value == null)
				return;

			if (deletedEventHandlers.Remove(value, out var handler))
			{
				watcher.Deleted -= handler;
			}
		}
	}

	private readonly Dictionary<RenamedEventHandler, RenamedEventHandler> renamedEventHandlers = new();
	//
	// Summary:
	//     Occurs when a file or directory in the specified System.IO.FileSystemWatcher.Path
	//     is renamed.
	public event RenamedEventHandler? Renamed
	{
		add
		{
			if (value == null)
				return;

			var handler = MapRenamedEventHandler(value);
			if (handler != null)
			{
				renamedEventHandlers.Add(value, handler);
				watcher.Renamed += handler;
			}
		}
		remove
		{
			if (value == null)
				return;

			if (renamedEventHandlers.Remove(value, out var handler))
			{
				watcher.Renamed -= handler;
			}
		}
	}

	//
	// Summary:
	//     Begins the initialization of a System.IO.FileSystemWatcher used on a form or
	//     used by another component. The initialization occurs at run time.
	public void BeginInit() => watcher.BeginInit();
	//
	// Summary:
	//     Ends the initialization of a System.IO.FileSystemWatcher used on a form or used
	//     by another component. The initialization occurs at run time.
	public void EndInit() => watcher.EndInit();

	public void Dispose() => watcher.Dispose();
}
