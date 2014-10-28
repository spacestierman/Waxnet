using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waxnet.Models;

namespace Waxnet.Example
{
	class Program
	{
		private static Waxnet _waxnet;

		static void Main(string[] args)
		{
			string waxFilePath = @"Your Wax path here";
			string outputPath = @"Your output path here";

			_waxnet = new Waxnet(outputPath, new ConsoleLogger());
			_waxnet.IgnoreFilesMatchingPattern("~"); // Ignore Windows temporary files involved when creating/modifying files.
			_waxnet.IgnoreFilesMatchingPatterns(".js$", ".css$"); // Ignore .js and .css files.  Wax doesn't care about those.

			_waxnet.OnWaxFileModified += OnWaxFileModified;
			_waxnet.OnSourceFileModified += OnSourceFileModified;
			_waxnet.OnSourceFileIgnored += OnSourceFileIgnored;
			_waxnet.OnWaxBuildError += OnWaxBuildError;
			_waxnet.OnWaxBuildStarted += OnWaxBuildStarted;
			_waxnet.OnWaxBuildFinished += OnWaxBuildFinished;
			_waxnet.OnWaxFullBuildStarted += OnWaxFullBuildStarted;
			_waxnet.OnWaxFullBuildFinished += OnWaxFullBuildFinished;
			_waxnet.OnWaxDefinitionChanged += OnWaxDefinitionChanged;
			_waxnet.OnWaxParseError += OnWaxParseError;

			_waxnet.LoadWaxFile(waxFilePath);
			_waxnet.ManualBuild();
			_waxnet.StartWatching();

			Console.ReadKey();
		}

		static void OnWaxFileModified(string filepath)
		{
			Log("OnWaxFileModified(\"{0}\")", filepath);
		}

		static void OnSourceFileModified(string filepath)
		{
			Log("OnSourceFileModified(\"{0}\")", filepath);

			if (filepath.EndsWith(".coffee"))
			{
				WaxnetSettings settings = _waxnet.GetSettings();

				ProcessStartInfo info = new ProcessStartInfo();
				info.FileName = @".\node_modules\.bin\bundle";
				info.Arguments = "exec rake js:dev";
				info.WorkingDirectory = settings.RootPath;
				info.UseShellExecute = false;
				info.RedirectStandardOutput = true;
				
				using (Process process = Process.Start(info))
				{
					process.WaitForExit();
					string processResult = process.StandardOutput.ReadToEnd();
				}
			}
		}

		private static void OnSourceFileIgnored(string filepath)
		{
			Log("OnSourceFileIgnored(\"{0}\")", filepath);
		}

		private static void OnWaxBuildError(string errorMessage)
		{
			Log("OnWaxBuildError(\"{0}\")", errorMessage);
		}

		private static void OnWaxFullBuildStarted(IEnumerable<Page> affectedPages)
		{
			Log("OnWaxFullBuildStarted()");
			foreach(Page page in affectedPages)
			{
				Log("\t" + page.Name + " rebuilding...");
			}
		}

		private static void OnWaxFullBuildFinished()
		{
			Log("OnWaxFullBuildFinished()");
		}

		private static void OnWaxBuildStarted(IEnumerable<Page> affectedPages)
		{
			Log("OnWaxBuildStarted()");
			foreach (Page page in affectedPages)
			{
				Log("\t" + page.Name + " rebuilding...");
			}
		}

		private static void OnWaxBuildFinished()
		{
			Log("OnWaxBuildFinished()");
		}

		private static void OnWaxDefinitionChanged(WaxnetSettings newSettings)
		{
			Log("OnWaxDefinitionChanged(newSettings)");
		}

		private static void OnWaxParseError(WaxParseError error)
		{
			Log("OnWaxParseError(error)");
			Log("Message: " + error.Message);
		}

		static void Log(string message)
		{
			Console.WriteLine(message);
		}

		static void Log(string format, params object[] args)
		{
			Log(string.Format(format, args));
		}
	}
}