using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.Models
{
	public class PageContent
	{
		public string ViewPath { get; set; }
		public string DataPath { get; set; }

		public PageContent(string viewPath, string dataPath)
		{
			ViewPath = viewPath;
			DataPath = dataPath;
		}

		public PageContent DeepClone()
		{
			PageContent clone = new PageContent(ViewPath, DataPath);
			return clone;
		}
	}
}
