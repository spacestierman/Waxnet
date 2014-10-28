using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Actions
{
	class ApplicationActionFactory
	{
		public string RootDirectory { get; set; }
		public BaseAction.Log LogHandler { get; set; }

		public ApplicationActionFactory(string rootDirectory, BaseAction.Log logHandler)
		{
			RootDirectory = rootDirectory;
			LogHandler = logHandler;
		}

		public CoffeeCompileAction CreateCoffeeAction()
		{
			CoffeeCompileAction action = new CoffeeCompileAction(RootDirectory, @"public/coffee/application.coffee", @"public/javascripts/application.js");
			AttachLogHandler(action);
			return action;
		}

		public WaxCompileAction CreateWaxAction()
		{
			WaxCompileAction action = new WaxCompileAction(RootDirectory);
			AttachLogHandler(action);
			return action;
		}

		public SassCompileAction CreateSassAction()
		{
			SassCompileAction action = new SassCompileAction(RootDirectory, "public/scss", "public/stylesheets");
			AttachLogHandler(action);
			return action;
		}

		private void AttachLogHandler(BaseAction action){
			if (LogHandler != null)
			{
				action.OnLog += LogHandler;
			}
		}
	}
}
