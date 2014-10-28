using Space150.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.Models
{
	public class Page
	{
		public string Name { get; set; }
		private Dictionary<string, List<PageContent>> _content;

		public Page(string name)
		{
			Name = name;
			_content = new Dictionary<string, List<PageContent>>();
		}

		public void AddContent(string layoutKey, PageContent content)
		{
			List<PageContent> list = null;
			if (!_content.ContainsKey(layoutKey))
			{
				list = new List<PageContent>();
				_content[layoutKey] = list;
			}
			else
			{
				list = _content[layoutKey];
			}

			list.Add(content);
		}

		public IEnumerable<string> GetLayoutKeys()
		{
			return _content.Keys;
		}

		public IEnumerable<PageContent> GetModulesForKey(string layoutKey)
		{
			return _content[layoutKey];
		}

		public bool ContainsFile(string filePath)
		{
			bool containsView = ContainsView(filePath);
			if (containsView)
			{
				return true;
			}

			bool containsData = ContainsData(filePath);
			if (containsData)
			{
				return true;
			}

			return false;
		}

		public bool ContainsView(string viewPath)
		{
			viewPath = FileSystemSlashes.EnsureFilesystemSlashes(viewPath);

			foreach(KeyValuePair<string, List<PageContent>> kvp in _content)
			{
				foreach(PageContent module in kvp.Value)
				{
					string moduleViewPath = FileSystemSlashes.EnsureFilesystemSlashes(module.ViewPath);
					if (moduleViewPath == viewPath)
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool ContainsData(string dataPath)
		{
			dataPath = FileSystemSlashes.EnsureFilesystemSlashes(dataPath);

			foreach (KeyValuePair<string, List<PageContent>> kvp in _content)
			{
				foreach (PageContent module in kvp.Value)
				{
					string moduleDataPath = FileSystemSlashes.EnsureFilesystemSlashes(module.DataPath);
					if (moduleDataPath == dataPath)
					{
						return true;
					}
				}
			}
			return false;
		}

		public Page DeepClone()
		{
			Page clone = new Page(Name);
			foreach(KeyValuePair<string, List<PageContent>> kvp in _content)
			{
				foreach(PageContent content in kvp.Value)
				{
					PageContent contentClone = content.DeepClone();
					clone.AddContent(kvp.Key, contentClone);
				}
			}
			return clone;
		}
	}
}
