using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Actions
{
	class SassCompileAction : BaseAction
	{
		public string RootDirectory { get; set; }
		public string SassDirectory { get; set; }
		public string OutputDirectory { get; set; }

		public SassCompileAction(string rootDirectory, string sassDirectory, string outputDirectory)
		{
			RootDirectory = rootDirectory;
			SassDirectory = sassDirectory;
			OutputDirectory = outputDirectory;
		}

		public void CompileSass()
		{
			LogIfAvailable("Compiling sass...");
			string filepath = @"C:\Ruby21-x64\bin\compass.bat";
			string arguments = string.Format("compile --sourcemap --sass-dir {0} --css-dir {1} --trace --time --debug-info --force", SassDirectory, OutputDirectory);
			ExecuteProcess(filepath, arguments, RootDirectory);
		}

		public override void Go()
		{
			CompileSass();
		}
	}
}
