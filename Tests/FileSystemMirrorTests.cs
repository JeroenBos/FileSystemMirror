using JBSnorro;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileSystemMirrorTests
{
	public class Tests
	{
		protected const NotifyFilters allNotifyFilters = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
		protected static int dt = Debugger.IsAttached ? 10000 : 1000;
		protected static readonly Random random = new Random();
		protected static string randomAlphaNumeric(int length)
		{
			var chars = new char[length];
			for (int i = 0; i < chars.Length; i++)
			{
				chars[i] = (char)('a' + random.Next(0, 26));
			}
			return new string(chars);
		}
		protected static string GetTempDirectory()
		{
			string dir = Path.GetTempPath() + random.Next(0, 100000).ToString();
			Directory.CreateDirectory(dir);
			return dir;
		}

		protected static void CreateFile(params string[] paths)
		{
			string path = Path.Combine(paths);
			File.WriteAllBytes(path, Array.Empty<byte>());
		}

		protected static FileSystemEventHandler RecordTrigger(out Task task)
		{
			var tcs = new TaskCompletionSource();
			task = tcs.Task;
			return onCreated;

			void onCreated(object sender, FileSystemEventArgs e) => tcs.SetResult();
		}
		/// <summary>
		/// Waits for any of the tasks to finish, with a maximum time of <see cref="dt"/>.
		/// </summary>
		public static Task<Task> WhenAnyWithTimeout(params Task[] tasks)
		{
			var list = new List<Task>(tasks);
			list.Add(Task.Delay(dt));
			return Task.WhenAny(list);
		}
		/// <summary>
		/// Waits for the task to finish, while executing the action, with a maximum time of <see cref="dt"/>.
		/// </summary>
		protected static Task<Task> WaitWithTimeout(Task task, Func<Task> action)
		{
			var actionTask = Task.Run(async () =>
			{
				await action();
				await Task.Delay(-1);
			});

			return Tests.WhenAnyWithTimeout(task, actionTask);
		}
		/// <summary>
		/// Waits for task1 to finish, while executing the action, with a maximum time of <see cref="dt"/>.
		/// </summary>
		protected static Task<Task> WaitWithTimeout(Task task, Action action)
		{
			return WaitWithTimeout(task, () => Task.Run(action));
		}

	}

	/// <summary>
	/// Just trying to see how this thing works, and documenting the assumptions.
	/// </summary>
	class FileSystemWatcherAssumptions : Tests
	{
		[Test]
		public async Task Can_detect_directory_creation_without_filter()
		{
			// Arrange
			string dir = GetTempDirectory();
			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters
			};
			watcher.Created += RecordTrigger(out var trigger);
			watcher.EnableRaisingEvents = true;

			// Act
			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "subdir"));
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_directory_creation_with_specific_filter()
		{
			// Arrange
			string dir = GetTempDirectory();
			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters,
				Filter = "subdir"
			};

			watcher.Created += RecordTrigger(out var trigger);
			watcher.EnableRaisingEvents = true;

			// Act
			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "subdir"));
			});

			// Assert
			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Cannot_detect_directory_creation_with_specific_wrong_filter()
		{
			// Arrange
			string dir = GetTempDirectory();
			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters,
				Filter = "wrong"
			};

			watcher.Created += RecordTrigger(out var trigger);
			watcher.EnableRaisingEvents = true;

			// Act
			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "subdir"));
			});

			// Assert
			Assert.IsFalse(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_nested_directory_creation_without_filter()
		{
			// Arrange
			string dir = GetTempDirectory();
			Directory.CreateDirectory(Path.Combine(dir, "subdir"));
			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters
			};

			watcher.Created += RecordTrigger(out var trigger);
			watcher.EnableRaisingEvents = true;

			// Act
			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "subdir", "nested_dir"));
			});

			// Assert
			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_nested_directory_creation_with_specific_filter()
		{
			// Arrange
			string dir = GetTempDirectory();
			Directory.CreateDirectory(Path.Combine(dir, "subdir"));
			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters,
				Filter = "nested_dir"
			};

			watcher.Created += RecordTrigger(out var trigger);
			watcher.EnableRaisingEvents = true;

			// Act
			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "subdir", "nested_dir"));
			});

			// Assert
			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_directory_creation()
		{
			string dir = GetTempDirectory();

			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters,
				EnableRaisingEvents = true
			};
			watcher.Created += RecordTrigger(out var trigger);

			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "z"));
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Cannot_detect_nested_file_creation_with_nested_filter()
		{
			string dir = GetTempDirectory();

			using FileSystemWatcher watcher = new FileSystemWatcher()
			{
				Path = Path.Combine(dir),
				Filter = "subdir\\*",
				IncludeSubdirectories = true,
				NotifyFilter = allNotifyFilters
			};

			watcher.Created += RecordTrigger(out var trigger);
			watcher.EnableRaisingEvents = true;
			Assert.IsFalse(trigger.IsCompletedSuccessfully);

			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "subdir"));
				CreateFile(dir, "subdir", "b.txt");
			});

			Assert.IsFalse(trigger.IsCompletedSuccessfully);
		}
	}


	public class HookTests : Tests
	{
		[Test]
		public async Task DetectCreationFile()
		{
			string dir = GetTempDirectory();
			string filename = "a.txt";

			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "*" },
				onCreated: RecordTrigger(out var trigger)
			);


			await WaitWithTimeout(trigger, () =>
			{
				CreateFile(dir, filename);
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task DismissCreationNestedFile()
		{
			string dir = GetTempDirectory();
			string nestedDir = "a";
			string filename = "a";
			Directory.CreateDirectory(Path.Combine(dir, nestedDir));

			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "*" },
				onCreated: RecordTrigger(out var trigger)
			);

			await WaitWithTimeout(trigger, () =>
			{
				CreateFile(dir, nestedDir, filename);
			});

			Assert.IsFalse(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task DismissCreationAlreadyExistingDirectory()
		{
			// Arrange
			string dir = GetTempDirectory();
			string nestedDir = "a";
			Directory.CreateDirectory(Path.Combine(dir, nestedDir));

			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "*" },
				onCreated: RecordTrigger(out var trigger)
			);

			// Act			
			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, nestedDir));
			});

			// Assert
			Assert.IsFalse(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task DetectCreationNestedFile()
		{
			string dir = GetTempDirectory();
			string nestedDir = "a";
			string filename = "a";
			Directory.CreateDirectory(Path.Combine(dir, nestedDir));

			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "**" },
				onCreated: RecordTrigger(out var trigger)
			);

			await WaitWithTimeout(trigger, () =>
			{
				CreateFile(dir, nestedDir, filename);
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_directory_creation_with_wildcard()
		{
			string dir = GetTempDirectory();
			string nestedDir = Path.Combine(dir, "a");

			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "**" },
				onCreated: RecordTrigger(out var trigger)
			);

			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(nestedDir);
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_directory_creation_with_empty_filter()
		{
			string dir = GetTempDirectory();
			string nestedDir = Path.Combine(dir, "a");

			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: Array.Empty<string>(),
				onCreated: RecordTrigger(out var trigger)
			);

			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(nestedDir);
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task DisposalStopsDetection()
		{
			string dir = GetTempDirectory();
			string filename = "a.txt";

			FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "*" },
				onCreated: RecordTrigger(out var trigger)
			).Dispose();

			await WaitWithTimeout(trigger, () =>
			{
				CreateFile(dir, filename);
			});

			Assert.IsFalse(trigger.IsCompletedSuccessfully);
		}

	}
	class ExternalDirectoriesTests : Tests
	{
		[Test]
		public async Task Can_detect_nested_file_creation()
		{
			string dir = GetTempDirectory();
			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "**" },
				onCreated: RecordTrigger(out var trigger)
			);


			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "a"));
				CreateFile(dir, "a", "b.txt");
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_nested_file_creation_with_middle_directory_temporarily_deleted()
		{
			string dir = GetTempDirectory();
			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "**" },
				onCreated: RecordTrigger(out var trigger)
			);

			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "a"));
				Directory.Delete(Path.Combine(dir, "a"));
				Directory.CreateDirectory(Path.Combine(dir, "a"));
				CreateFile(dir, "a", "b.txt");
			});
			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_detect_nested_file_creation_with_nested_filter()
		{
			string dir = GetTempDirectory();
			Directory.CreateDirectory(Path.Combine(dir, "a"));
			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "a\\*" },
				onCreated: RecordTrigger(out var trigger)
			);
			Assert.IsTrue(hook.EnableRaisingEvents);
			Assert.IsTrue(hook.IncludeSubdirectories);

			await WaitWithTimeout(trigger, () =>
			{
				CreateFile(dir, "a", "b.txt");
			});

			Assert.IsTrue(trigger.IsCompletedSuccessfully);
			Assert.IsTrue(hook.EnableRaisingEvents);
		}
		[Test]
		public async Task Can_detect_nested_file_creation_with_nested_filter_with_dir_created_later()
		{
			string dir = GetTempDirectory();
			using var hook = FileSystemHook.Hook(dir,
				sourcePatterns: new[] { "a\\*" },
				onCreated: RecordTrigger(out var trigger)
			);

			Assert.IsTrue(hook.EnableRaisingEvents);

			await WaitWithTimeout(trigger, () =>
			{
				Directory.CreateDirectory(Path.Combine(dir, "a"));
				CreateFile(dir, "a", "b.txt");
			}
			);
			Assert.IsTrue(trigger.IsCompletedSuccessfully);
		}
		[Test]
		public async Task Can_repurpose_hook_on_parent_directory_on_directory_deletion()
		{
			// this test will hook on a directory, delete that, and the hook with the same hook on the parent directory
			string parentDir = GetTempDirectory();
			string dir = Path.Combine(parentDir, "a");
			Directory.CreateDirectory(dir);

			var erroredTcs = new TaskCompletionSource();
			var createdTcs = new TaskCompletionSource();
			IFileSystemWatcher hook = null!;
			using (hook = FileSystemHook.Hook(dir,
												sourcePatterns: new[] { "**" },
												onError: onError,
												onCreated: onCreated))
			{
				// Act 1
				var completedTask = await WaitWithTimeout(erroredTcs.Task, () =>
				{
					Directory.Delete(dir);
				});

				// Assert 1
				Assert.IsTrue(erroredTcs.Task.IsCompletedSuccessfully);
				Assert.IsTrue(hook.EnableRaisingEvents);
				Assert.AreEqual(hook.Path, parentDir);
				Assert.AreEqual(hook.Filter, "*");


				// Act 2
				await WaitWithTimeout(createdTcs.Task, () =>
				{
					Assert.IsTrue(hook.EnableRaisingEvents);

					Directory.CreateDirectory(dir);
				});

				// Assert 2
				Assert.IsTrue(hook.EnableRaisingEvents);
				Assert.IsTrue(createdTcs.Task.IsCompletedSuccessfully);
				Assert.IsTrue(hook.EnableRaisingEvents);


				// Arrange 3
				hook.Created -= onCreated;
				hook.Created += RecordTrigger(out var trigger);

				// Act 3
				await WaitWithTimeout(trigger, () =>
				{
					CreateFile(dir, "file");
				});

				// Assert 3
				Assert.IsTrue(trigger.IsCompletedSuccessfully);

			}

			void onError(object sender, ErrorEventArgs e)
			{
				Assert.IsFalse(erroredTcs.Task.IsCompleted);

				hook.BeginInit();
				hook.Path = parentDir;
				hook.NotifyFilter = allNotifyFilters;
				hook.EndInit();

				Assert.IsTrue(hook.EnableRaisingEvents);


				// the unfortunate reality is that EnabledRaisingEvents is set to false after triggering this method
				// if we threw an exception, that would still set the directoryHandle to null, which would still need
				// EnableRaisingEvents to be set to true again

				Task.Run(() =>
				{
					while (true)
					{
						if (!hook.EnableRaisingEvents)
						{
							hook.EnableRaisingEvents = true;
							erroredTcs.SetResult();
							return;
						}
					}
				});
			}
			void onCreated(object sender, FileSystemEventArgs e)
			{
				Assert.IsFalse(createdTcs.Task.IsCompleted);

				Thread.Sleep(100);
				hook.BeginInit();
				hook.Path = dir;
				hook.Filter = "";
				hook.NotifyFilter = allNotifyFilters;
				hook.EndInit();

				createdTcs.SetResult();
			}
		}
	}
	class PerennialHookTests : Tests
	{
		// [Test]
		//public async Task Recovers_if_source_directory_is_deleted()
		//{
		//	string tempdir = GetTempDirectory();
		//	string dir = Path.Combine(tempdir, "a");
		//	Directory.CreateDirectory(dir);


		//	bool detectedAtAll = false;
		//	FileSystemHook.Hook(dir, new string[] { "**" }, onCreated: (sender, e) => detectedAtAll = true);


		//	bool deleted = false;
		//	void onDeleted(object sender, FileSystemEventArgs e)
		//	{
		//		if (Path.GetDirectoryName(e.Name) == dir)
		//			deleted = true;
		//	}

		//	var disposable = (BinaryDisposableRef)FileSystemMirror.PerennialHook(dir,
		//		sourcePatterns: new[] { "*.txt" },
		//		onDeleted: onDeleted
		//		);

		//	var watcher = (FileSystemWatcher)disposable.Primary!;
		//	var watcherDisposed = false;
		//	watcher.Disposed += (_, _) => watcherDisposed = true;
		//	Assert.IsTrue(watcher.EnableRaisingEvents);
		//	Assert.IsFalse(watcherDisposed);
		//	Assert.IsNull(disposable.Secondary);

		//	// deleting a/ will dispose the current watcher
		//	await Task.Run(async () =>
		//	{
		//		Directory.Delete(dir);
		//		await Task.Delay(dt);
		//	});

		//	var parentWatcher = (FileSystemWatcher)disposable.Secondary!;
		//	var parentDisposed = false;
		//	parentWatcher.Disposed += (_, _) => parentDisposed = true;
		//	Assert.AreNotSame(parentWatcher, watcher);
		//	Assert.IsTrue(parentWatcher.EnableRaisingEvents);
		//	Assert.IsFalse(deleted, "Creating a directory was interpreted as creating a file");
		//	Assert.IsFalse(parentDisposed);

		//	// creating the directory doesn't trigger anything yet
		//	await Task.Run(async () =>
		//	{
		//		Directory.CreateDirectory(dir);
		//		await Task.Delay(dt);
		//	});

		//	Assert.IsTrue(parentWatcher.EnableRaisingEvents);
		//	Assert.IsFalse(parentDisposed);
		//	Assert.IsFalse(watcher.EnableRaisingEvents);
		//	detectedAtAll = false;
		//	// creating any file will trigger the parentwatcher to be swapped out for the watcher
		//	await Task.Run(async () =>
		//	{
		//		CreateFile(dir, "b.txt");
		//		await Task.Delay(dt);
		//	});

		//	await Task.Run(async () =>
		//	{
		//		string path = Path.Combine(parentWatcher.Path, "a", "b.txt");
		//		CreateFile(path);
		//		await Task.Delay(dt);
		//	});

		//	Assert.IsTrue(detectedAtAll);
		//	Assert.IsFalse(parentWatcher.EnableRaisingEvents);
		//	Assert.IsTrue(parentDisposed);
		//	Assert.IsTrue(watcher.EnableRaisingEvents);
		//	Assert.IsFalse(watcherDisposed);
		//}
	}

}