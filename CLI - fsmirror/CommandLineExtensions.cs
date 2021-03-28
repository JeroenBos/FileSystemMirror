using JBSnorro;

namespace System.CommandLine
{
	public static class CommandLineExtensions
	{
		public static RootCommand With(this RootCommand rootCommand, params Symbol[] symbols)
		{
			foreach (Symbol symbol in symbols)
				rootCommand.Add(symbol);
			return rootCommand;
		}
		public static Argument<T> With<T>(this Argument<T> argument,
										  Maybe<string> description = default,
										  Maybe<T> defaultValue = default,
										  Maybe<string> alias = default)
		{
			if (description.HasValue)
				argument.Description = description.Value;
			if (defaultValue.HasValue)
				argument.SetDefaultValue(defaultValue.Value);
			if (alias.HasValue)
				argument.AddAlias(alias.Value);

			return argument;
		}
	}
}
