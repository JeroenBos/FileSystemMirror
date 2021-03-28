using FileSystemMirrorTests;
//var tests = new ExternalDirectoriesTests();
var tests = new ExternalDirectoriesTests();

tests.Can_repurpose_hook_on_parent_directory_on_directory_deletion().Wait();
