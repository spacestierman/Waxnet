using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;

namespace Waxnet
{
	public class WaxParseError
	{
		public SyntaxErrorException YamlException { get; set; }


		public WaxParseError(SyntaxErrorException yamlParseException)
		{
			YamlException = yamlParseException;
		}

		public string Message
		{
			get
			{
				return YamlException.Message;
			}
		}
	}
}
