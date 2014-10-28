using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.Example
{
	class ConsoleLogger : IWaxLogger
	{
		public void Log(string message)
		{
			Console.WriteLine(message);
		}
	}
}
