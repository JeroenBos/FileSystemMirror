using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public abstract class RecoveryStrategy
{
	// this class is a Discriminated Union, enforced by the private constructor
	private RecoveryStrategy() { }

	public RecoveryStrategy None { get; } = new NoRecovery();

	public abstract int MaxAttempts { get; }


	public static RecoveryStrategy FromAbsolutePath(string highestAncestorPath)
	{
		throw new NotImplementedException();
	}

	class NoRecovery: RecoveryStrategy
	{
		public override int MaxAttempts => -1;
	}


	public IEnumerable<(DirectoryInfo, string? ChildName)> HookableParents(string sourcePath)
	{
		var info = new DirectoryInfo(sourcePath);
		yield return (info, null);
		// TODO: take into account recoveryStrategy
		while (info.Parent != null)
		{
			string childName = info.Name + "//";
			info = info.Parent;
			yield return (info, childName);
		}
	}
}
