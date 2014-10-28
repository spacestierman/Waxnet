using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Actions
{
	class CoffeeCompileAction : BaseAction
	{
		public string WorkingDirectory { get; set; }
		public string CoffeeFile { get; set; }
		public string OutputFile { get; set; }

		public CoffeeCompileAction(string workingDirectory, string coffeeFile, string outputFilePath)
		{
			WorkingDirectory = workingDirectory;
			CoffeeFile = coffeeFile;
			OutputFile = outputFilePath;
		}

		public void CompileCoffee()
		{
			LogIfAvailable("Compiling coffee...");
			string browserifyPath = WorkingDirectory + @"\node_modules\.bin\browserify.cmd";
			string arguments = string.Format("--extension=\".coffee\" -t coffeeify -t stripify -t uglifyify {0} -o {1}", CoffeeFile, OutputFile);
			ExecuteProcess(browserifyPath, arguments, WorkingDirectory);
		}

		public override void Go()
		{
			CompileCoffee();
		}
	}
}
