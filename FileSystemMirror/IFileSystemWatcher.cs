using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IFileSystemWatcher : IDisposable
{
	bool EnableRaisingEvents { get; set; }
	bool IncludeSubdirectories { get; set; }

	string Filter { get; set; }
	string Path { get; set; }
	void BeginInit();
	void EndInit();

	NotifyFilters NotifyFilter { get; set; }

	event FileSystemEventHandler Created;
	event FileSystemEventHandler Changed;
	event FileSystemEventHandler Deleted;
	event RenamedEventHandler Renamed;
}
