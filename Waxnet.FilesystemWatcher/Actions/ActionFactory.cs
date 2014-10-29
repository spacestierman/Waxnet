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
		public BaseAction.Error ErrorHandler { get; set; }

		public ApplicationActionFactory(string rootDirectory, BaseAction.Log logHandler = null, BaseAction.Error errorHandler = null)
		{
			RootDirectory = rootDirectory;
			LogHandler = logHandler;
			ErrorHandler = errorHandler;
		}

		public CoffeeCompileAction CreateCoffeeAction()
		{
			CoffeeCompileAction action = new CoffeeCompileAction(RootDirectory, @"public/coffee/application.coffee", @"public/javascripts/application.js");
			AttachHandlers(action);
			return action;
		}

		public WaxCompileAction CreateWaxAction()
		{
			WaxCompileAction action = new WaxCompileAction(RootDirectory);
			AttachHandlers(action);
			return action;
		}

		public SassCompileAction CreateSassAction()
		{
			SassCompileAction action = new SassCompileAction(RootDirectory, "public/scss", "public/stylesheets");
			AttachHandlers(action);
			return action;
		}

		private void AttachHandlers(BaseAction action)
		{
			if (LogHandler != null)
			{
				action.OnLog += LogHandler;
			}

			if (ErrorHandler != null)
			{
				action.OnError += ErrorHandler;
			}
		}
	}
}
