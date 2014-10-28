using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Space150.Common
{
    public class FormattedException : Exception
    {
		public FormattedException(string message) : base(message)
		{

		}

		public FormattedException(string format, params object[] args) : base(string.Format(format, args))
		{

		}
    }
}
