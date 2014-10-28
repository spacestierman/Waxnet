using Space150.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet
{
	public class WaxnetException : FormattedException
	{
		public WaxnetException(string message) : base(message)
		{

		}

		public WaxnetException(string format, params object[] args) : base(format, args)
		{
			
		}
	}
}
