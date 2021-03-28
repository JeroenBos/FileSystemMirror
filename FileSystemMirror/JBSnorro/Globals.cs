using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JBSnorro
{
	static class Globals
	{
		public static bool IsFileSystemCaseSensitive
		{
			get
			{
				// should be true for linux
				return false;
			}
		}

		[DebuggerHidden]
		public static void Assert(bool condition)
		{
			if (!condition)
				throw new Exception();
		}

		[DebuggerHidden]
		public static string ToRelativePath(string fullPath, string basePath)
		{
			if (!fullPath.StartsWith(basePath))
				throw new Exception($"!fullPath.StartsWith(sourcePath): '{fullPath}', '{basePath}'");

			int start = basePath.Length;
			if (basePath.EndsWith(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
			{

			}
			else if (basePath.Length != fullPath.Length)
			{
				if (!fullPath[start].IsDirectorySeparatorChar())
					throw new Exception($"Path separator expected at index {start} in '{fullPath}'");
				start++;
			}

			string relativePath = fullPath[start..];
			return relativePath;
		}

		/// <summary>
		/// Interprets a single string as a list (separated by <see cref="Path.PathSeparator"/>) of glob patterns and ignore glob patterns (those starting with !).
		/// </summary>
		public static (IReadOnlyList<string> Patterns, IReadOnlyList<string> IgnorePatterns) DecomposePatterns(string compositePatterns)
		{
			var patterns = compositePatterns.Split(Path.PathSeparator).Select(s => s.Trim()).ToList();
			var ignorepatterns = patterns.Where(pattern => pattern.StartsWith("!")).ToList();
			patterns = patterns.Where(pattern => !pattern.StartsWith("!")).ToList();
			return (patterns, ignorepatterns);
		}

		public static char[] DirectorySeparators => new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
	}
}
