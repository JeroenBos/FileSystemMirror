using JBSnorro;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public class MemoryLogger : ILogger
{
	private List<string> entries;
	public IReadOnlyList<string> Entries { get; }
	public MemoryLogger()
	{
		entries = new List<string>();
		Entries = new ReadOnlyCollection<string>(entries);
	}

	public void Log(string s)
	{
		this.entries.Add(s);
	}
}