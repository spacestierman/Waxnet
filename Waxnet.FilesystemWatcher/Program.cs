using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher
{
	class Program
	{
		private static ApplicationWatcher _watcher;

		static void Main(string[] args)
		{
			string directory = args[0];
			_watcher = new ApplicationWatcher(directory);
			_watcher.IgnoreFilesMatchingPattern("~"); // Ignore Windows temporary files involved when creating/modifying files.
			_watcher.IgnoreFilesMatchingPatterns("[.]js$", "[.]css$"); // Ignore .js and .css files.  Wax doesn't care about those.

			Console.ReadKey();
		}
	}
}
