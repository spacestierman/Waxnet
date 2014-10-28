using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Actions
{
	class WaxCompileAction : BaseAction
	{
		public string WaxRootDirectory { get; set; }

		public WaxCompileAction(string waxRootDirectory)
		{
			WaxRootDirectory = waxRootDirectory;
		}

		public void CompileWax()
		{
			LogIfAvailable("Compiling wax...");
			ExecuteProcess(@"C:\Ruby21-x64\bin\bundle.bat", "exec rake wax:build", WaxRootDirectory);
		}

		public override void Go()
		{
			CompileWax();
		}
	}
}
