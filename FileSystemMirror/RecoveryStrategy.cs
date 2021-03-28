using System;
using System.Collections.Generic;
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
}
