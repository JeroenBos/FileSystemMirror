#if false
// if the new version of CommandLine comes out this may help
using System.CommandLine;
using System.CommandLine.Parsing;
using Resources = System.CommandLine.ValidationMessages;

namespace fsmirror
{
	class MyResources : Resources
	{
		public static void Initialize()
		{
			Resources.Instance = new MyResources();
		}

		private const string CommandRequiredArgumentMissing = "Mandatory argument missing: {1}";
		private const string OptionRequiredArgumentMissing = "Mandatory option missing: {1}";
		public override string RequiredArgumentMissing(SymbolResult symbolResult, IArgument argument) =>
			  symbolResult is CommandResult
				? string.Format(CommandRequiredArgumentMissing, symbolResult.ToString(), argument!.Name)
				: string.Format(OptionRequiredArgumentMissing, symbolResult.ToString(), argument!.Name);
	}
}
#else
class CommandLineBuilderExtensions
{
	public static string AssemblyVersion => "1.0.0.0";
}
#endif