using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JBSnorro
{
	public static class Extensions
	{
		private static readonly char[] DirectorySeparators = new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
		public static bool ContainsMultiple(this string s, string item)
		{
			int firstIndex = s.IndexOf(item);
			if (firstIndex == -1)
				return false;

			int secondIndex = s.IndexOf(item, firstIndex + 1);
			return secondIndex != -1;
		}
		public static int Translate(this Index index, ICollection collection)
		{
			return index.GetOffset(collection.Count);
		}
		public static void RemoveAt<T>(this List<T> list, Index i)
		{
			list.RemoveAt(i.GetOffset(list.Count));
		}
		/// <summary>
		/// Returns whether the string ends with any of the specified items.
		/// </summary>
		public static bool EndsWith(this string s, params char[] items)
		{
			foreach (var item in items)
			{
				if (s.EndsWith(item))
					return true;
			}
			return false;
		}
		public static IEnumerable<string> SplitByDirectorySeparators(this string s)
		{
			return s.Split(DirectorySeparators, StringSplitOptions.RemoveEmptyEntries);
		}

		public static T Last<T>(this IReadOnlyList<T> list)
		{
			return list[list.Count - 1];
		}
	}
}
