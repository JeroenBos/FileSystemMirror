using JBSnorro;
using NUnit.Framework;

namespace GlobPatternTests
{
	public class SimpleNamesTests
	{
		[Test]
		public void SimpleFilenameEqualityTest()
		{
			var pattern = new GlobPattern("a.txt");

			Assert.IsTrue(pattern.Matches("a.txt"));
			Assert.IsFalse(pattern.Matches("b.txt"));
		}

		[Test]
		public void SimpleFilenameMatchesWithLeadingDirectorySeparator()
		{
			var pattern = new GlobPattern("a.txt");

			Assert.IsTrue(pattern.Matches("/a.txt"));
		}


		[Test]
		public void SimpleFilenameDoesNotMatchNested()
		{
			var pattern = new GlobPattern("a.txt");

			Assert.IsFalse(pattern.Matches("dir/a.txt"));
		}

		[Test]
		public void SimpleNestedFileMatchesNested()
		{
			var pattern = new GlobPattern("dir/a.txt");

			Assert.IsTrue(pattern.Matches("dir/a.txt"));
		}

		[Test]
		public void SimpleNestedFileMatchesNestedWithLeadingDirectorySeparator()
		{
			var pattern = new GlobPattern("dir/a.txt");

			Assert.IsTrue(pattern.Matches("/dir/a.txt"));


			var patternWithLeading = new GlobPattern("/dir/a.txt");
			Assert.IsTrue(patternWithLeading.Matches("dir/a.txt"));
			Assert.IsTrue(patternWithLeading.Matches("/dir/a.txt"));
		}


		[Test]
		public void SimpleNestedFileInOtherDirectoryDoesNotMatch()
		{
			var pattern = new GlobPattern("dir/a.txt");

			Assert.IsFalse(pattern.Matches("other_dir/a.txt"));
		}
	}

	class SimpleDirectoryTests
	{
		[Test]
		public void SimpleDirectoryEqualityTest()
		{
			var pattern = new GlobPattern("a/");

			Assert.IsTrue(pattern.Matches("a/"));
			Assert.IsFalse(pattern.Matches("b/"));
			Assert.IsFalse(pattern.Matches("a")); // is a file
		}

		[Test]
		public void SimpleDirectoryMatchesWithLeadingDirectorySeparator()
		{
			var pattern = new GlobPattern("a/");

			Assert.IsTrue(pattern.Matches("/a/"));
			Assert.IsFalse(pattern.Matches("/a")); // is a file
		}


		[Test]
		public void SimpleDirectoryDoesNotMatchNested()
		{
			var pattern = new GlobPattern("a/");

			Assert.IsFalse(pattern.Matches("dir/a/"));
			Assert.IsFalse(pattern.Matches("dir/a")); // is a file
		}

		[Test]
		public void SimpleNestedDirectoryMatchesNested()
		{
			var pattern = new GlobPattern("dir/a/");

			Assert.IsTrue(pattern.Matches("dir/a/"));
			Assert.IsFalse(pattern.Matches("dir/a")); // is a file
		}

		[Test]
		public void SimpleNestedDirMatchesNestedWithLeadingDirectorySeparator()
		{
			var pattern = new GlobPattern("dir/a/");

			Assert.IsTrue(pattern.Matches("/dir/a/"));


			var patternWithLeading = new GlobPattern("/dir/a/");
			Assert.IsTrue(patternWithLeading.Matches("dir/a/"));
			Assert.IsTrue(patternWithLeading.Matches("/dir/a/"));
		}


		[Test]
		public void SimpleNestedFileInOtherDirectoryDoesNotMatch()
		{
			var pattern = new GlobPattern("dir/a/");

			Assert.IsFalse(pattern.Matches("other_dir/a/"));
		}
	}


	class SimpleWildcardTests
	{
		[Test]
		public void WildcardInFilename()
		{
			var pattern = new GlobPattern("*.txt");

			Assert.IsTrue(pattern.Matches("abcde.txt"));


			var patternLeading = new GlobPattern("a*.txt");

			Assert.IsTrue(patternLeading.Matches("abcde.txt"));
			Assert.IsFalse(patternLeading.Matches("bcde.txt"));
		}

		[Test]
		public void WildcardInNestedFilename()
		{
			var pattern = new GlobPattern("dir/*.txt");

			Assert.IsTrue(pattern.Matches("dir/abcde.txt"));


			var patternLeading = new GlobPattern("dir/a*.txt");

			Assert.IsTrue(patternLeading.Matches("dir/abcde.txt"));
			Assert.IsFalse(patternLeading.Matches("dir/bcde.txt"));
		}

		[Test]
		public void AllWildcardDoesntMatchNested()
		{
			var pattern = new GlobPattern("*");

			Assert.IsFalse(pattern.Matches("dir/a.txt"));

			var pattern2 = new GlobPattern("*.*");

			Assert.IsFalse(pattern2.Matches("dir/a.txt"));
		}
	}

	/// <summary>
	/// Tests involving **
	/// </summary>
	class DirectoryWildcardTests
	{
		[Test]
		public void Wildcard()
		{
			var pattern = new GlobPattern("**.txt");

			Assert.IsTrue(pattern.Matches("abcde.txt"));
			Assert.IsTrue(pattern.Matches("dir/abcde.txt"));
			Assert.IsTrue(pattern.Matches("dir/abcde.txt"));


			var leadingPattern = new GlobPattern("/**.txt");

			Assert.IsTrue(leadingPattern.Matches("/abcde.txt"));
			Assert.IsTrue(leadingPattern .Matches("/dir/abcde.txt"));
			Assert.IsTrue(leadingPattern .Matches("/dir/abcde.txt"));
		}

		[Test]
		public void WildcardWithWildcardInFilename()
		{
			var pattern = new GlobPattern("**/*.txt");

			Assert.IsTrue(pattern.Matches("abcde.txt"));
			Assert.IsTrue(pattern.Matches("dir/abcde.txt"));
			Assert.IsTrue(pattern.Matches("dir/abcde.txt"));


			var leadingPattern = new GlobPattern("/**/*.txt");

			Assert.IsTrue(leadingPattern.Matches("/abcde.txt"));
			Assert.IsTrue(leadingPattern.Matches("/dir/abcde.txt"));
			Assert.IsTrue(leadingPattern.Matches("/dir/abcde.txt"));
		}
		
		[Test]
		public void VolumeIsMatchedByDirectoryWildcard()
		{
			// given that a pattern always has to be relative to something, I don't think is practical
			var pattern = new GlobPattern("**");

			// technically this shouldn't match because it's a directory and the pattern matches file
			Assert.IsTrue(pattern.Matches("C:"));
		}

		[Test]
		public void AllWildcardDoesntMatchFile()
		{
			var pattern = new GlobPattern("**/");

			Assert.IsTrue(pattern.Matches("a/"));
			Assert.IsFalse(pattern.Matches("a.txt"));
		}
	}

	class VolumeSeparatorTests
	{
		[Test]
		public void HandlesVolumeSeparator()
		{
			var pattern = new GlobPattern("C:/");

			Assert.IsTrue(pattern.Matches("C:/"));
			Assert.IsFalse(pattern.Matches("D:/"));
		}

		[Test]
		public void HandlesVolumeSeparatorWithoutTrailingSlash()
		{
			var pattern = new GlobPattern("C:");

			Assert.IsTrue(pattern.Matches("C:"));
		}


		[Test]
		public void HandlesPatternAfterVolumeSeparator()
		{
			var pattern = new GlobPattern("C:/*.txt");

			Assert.IsTrue(pattern.Matches("C:/a.txt"));
			Assert.IsFalse(pattern.Matches("D:/a.txt"));
		}

		[Test]
		public void AbsolutePathContainment()
		{
			var pattern = new GlobPattern("C:/ASDF/My dir/**/");

			Assert.IsTrue(pattern.Matches("C:/ASDF/My dir/deeper/"));
			Assert.IsFalse(pattern.Matches("C:/ASDF/My dir/deeper"));
			Assert.IsFalse(pattern.Matches("D:/ASDF/My dir/deeper/"));
		}
	}
}

