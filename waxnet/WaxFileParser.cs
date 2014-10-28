using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waxnet.Models;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace Waxnet
{
	public class WaxFileParser
	{
		public WaxFileParser()
		{

		}

		public WaxnetSettings ParseFile(string filepath)
		{
			if (!IsWellformedWaxFile(filepath))
			{
				throw new ArgumentException("The supplied filepath does not contain valid wax");
			}

			string directory = Path.GetDirectoryName(filepath);
			string contents = File.ReadAllText(filepath);
			
			return ParseWax(contents, directory);
		}

		public WaxnetSettings ParseWax(string waxfileContents, string waxRootDirectoryPath)
		{
			if (!IsWellformedWax(waxfileContents))
			{
				throw new ArgumentException("The supplied waxFileContents are not valid wax");
			}

			StringReader reader = new StringReader(waxfileContents);
			YamlStream stream = new YamlStream();
			stream.Load(reader);
			YamlMappingNode yaml = (YamlMappingNode)stream.Documents[0].RootNode;

			WaxnetSettings settings = ConvertYamlStructureToWaxSettings(yaml, waxRootDirectoryPath);
			return settings;
		}

		public bool IsWellformedWaxFile(string filepath)
		{
			return true;
			throw new NotImplementedException();
		}

		public bool IsWellformedWax(string waxfileContents)
		{
			return true;
			throw new NotImplementedException();
		}

		public object GetWaxErrors(string waxFileContents)
		{
			throw new NotImplementedException(); /// TODO: give friendly error messages back about invalid wax files
		}

		private WaxnetSettings ConvertYamlStructureToWaxSettings(YamlMappingNode yaml, string waxRootDirectoryPath)
		{
			WaxnetSettings settings = new WaxnetSettings(waxRootDirectoryPath);

			foreach (KeyValuePair<YamlNode, YamlNode> node in yaml.Children)
			{
				YamlScalarNode key = (YamlScalarNode)node.Key;
				switch (key.Value)
				{
					case "paths":
						ParsePathsIntoSettings((YamlMappingNode)node.Value, settings);
						break;
					case "pages":
						ParsePagesIntoSettings((YamlMappingNode)node.Value, settings);
						break;
				}
			}

			return settings;
		}

		private void ParsePathsIntoSettings(YamlMappingNode symLinkNode, WaxnetSettings settings)
		{
			IEnumerable<YamlNode> nodes = symLinkNode.Children.ElementAt(0).Value.AllNodes;
			foreach(YamlNode node in nodes)
			{
				if (node is YamlScalarNode)
				{
					YamlScalarNode scalar = (YamlScalarNode)node;
					settings.AddSymLink(scalar.Value);
				}
			}
		}

		private void ParsePagesIntoSettings(YamlMappingNode pagesNode, WaxnetSettings settings)
		{
			foreach (KeyValuePair<YamlNode, YamlNode> node in pagesNode.Children)
			{
				YamlScalarNode key = (YamlScalarNode)node.Key;
				Page page = new Page(key.Value);
				settings.AddPage(page);

				YamlMappingNode value = (YamlMappingNode)node.Value;
				foreach (KeyValuePair<YamlNode, YamlNode> contentNode in value.Children)
				{
					YamlScalarNode contentKey = (YamlScalarNode)contentNode.Key;
					if (contentKey.Value == "<<") // Handle page references
					{
						ParsePageReferenceIntoPage((YamlMappingNode)contentNode.Value, page);
					}
					else
					{
						string layoutPlaceholder = contentKey.Value;
						YamlSequenceNode contentModuleNodes = (YamlSequenceNode)contentNode.Value;
						IEnumerable<PageContent> contentModules = ParsePageContentsFromSequenceNode(contentModuleNodes);
						foreach(PageContent contentModule in contentModules)
						{
							page.AddContent(layoutPlaceholder, contentModule);
						}
					}
				}
			}
		}

		private IEnumerable<PageContent> ParsePageContentsFromSequenceNode(YamlSequenceNode node)
		{
			List<PageContent> pages = new List<PageContent>();
			foreach (YamlNode childNodes in node.Children)
			{
				YamlMappingNode mappingNode = (YamlMappingNode)childNodes;
				IEnumerable<PageContent> contents = ParsePageContentsFromYaml(mappingNode);
				foreach (PageContent pageContent in contents)
				{
					pages.Add(pageContent);
				}
			}

			return pages;
		}

		private IEnumerable<PageContent> ParsePageContentsFromYaml(YamlMappingNode node)
		{
			List<PageContent> pages = new List<PageContent>();

			foreach(KeyValuePair<YamlNode, YamlNode> child in node.Children)
			{
				if (child.Key is YamlScalarNode && child.Value is YamlScalarNode)
				{
					YamlScalarNode key = (YamlScalarNode)child.Key;
					YamlScalarNode value = (YamlScalarNode)child.Value;

					PageContent content = new PageContent(key.Value, value.Value);
					pages.Add(content);
				}
			}

			return pages;
		}

		private PageContent ParsePageContentFromYaml(YamlNode node)
		{
			PageContent content = new PageContent("", "");
			return content;
		}

		private void ParsePageReferenceIntoPage(YamlMappingNode pageReferenceNode, Page page)
		{
			foreach (KeyValuePair<YamlNode, YamlNode> childNode in pageReferenceNode.Children)
			{
				YamlScalarNode layoutKey = (YamlScalarNode)childNode.Key;
				IEnumerable<PageContent> contentModules = ParsePageContentsFromSequenceNode((YamlSequenceNode)childNode.Value);
				foreach(PageContent contentModule in contentModules)
				{
					page.AddContent(layoutKey.Value, contentModule);
				}
			}
		}
	}
}
