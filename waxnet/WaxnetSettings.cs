using Space150.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waxnet.Models;

namespace Waxnet
{
	public class WaxnetSettings
	{
		private const string FILE_EXTENSION_DATA = ".json";
		private const string FILE_EXTENSION_VIEW = ".mustache";

		public string RootPath { get; set; }
		public string RelativeTemplatePath { get; set; }
		public string RelativeDataPath { get; set; }
		public string RelativeViewsPath { get; set; }

		private List<string> _symlinks;
		private List<Page> _pages;

		public WaxnetSettings(string rootPath)
		{
			RootPath = FileSystemSlashes.EnsureFileSystemTrailingSlash(rootPath);
			RelativeTemplatePath = @"wax\templates\";
			RelativeDataPath = @"wax\data\";
			RelativeViewsPath = @"Views\";

			_symlinks = new List<string>();
			_pages = new List<Page>();
		}

		public string AbsoluteTemplatePath
		{
			get { return RootPath + RelativeTemplatePath; }
		}

		public string AbsoluteDataPath
		{
			get { return RootPath + RelativeDataPath; }
		}

		public string AbsoluteViewsPath
		{
			get { return RootPath + RelativeViewsPath; }
		}

		public void AddSymLink(string path)
		{
			if (_symlinks.Contains(path))
			{
				throw new FormattedException("Path \"{0}\" is already a Symlink and should not be added twice.", path);
			}
			_symlinks.Add(path);
		}

		public IEnumerable<string> GetSymLinks()
		{
			return _symlinks;
		}

		public void AddPage(Page page)
		{
			if (_pages.Contains(page))
			{
				throw new FormattedException("Page \"{0}\" is already a Symlink and should not be added twice.", page.Name);
			}

			_pages.Add(page);
		}

		public void AddPages(params Page[] pages)
		{
			foreach(Page page in pages)
			{
				AddPage(page);
			}
		}

		public IEnumerable<Page> GetPages()
		{
			return _pages;
		}

		public WaxnetSettings DeepClone()
		{
			WaxnetSettings settings = new WaxnetSettings(RootPath);
			settings.RelativeTemplatePath = AbsoluteTemplatePath;
			settings.RelativeDataPath = AbsoluteDataPath;
			settings.RelativeViewsPath = AbsoluteViewsPath;
			
			foreach(string symlink in _symlinks)
			{
				settings.AddSymLink(symlink);
			}

			foreach(Page page in _pages)
			{
				Page pageClone = page.DeepClone();
				settings.AddPage(pageClone);
			}

			return settings;
		}

		public bool FileAffectsWax(string filepath)
		{
			if (FileIsTheWaxDefinitionFile(filepath))
			{
				return true;
			}

			filepath = FileSystemSlashes.EnsureFilesystemSlashes(filepath);
			string relativePath = filepath.Replace(RootPath, string.Empty);
			bool isDataFile = IsDataFile(relativePath);
			bool isViewFile = IsViewFile(relativePath);

			return isDataFile || isViewFile;
		}

		private bool IsDataFile(string relativeFilepath)
		{
			if (!relativeFilepath.EndsWith(FILE_EXTENSION_DATA))
			{
				return false;
			}

			if (!relativeFilepath.StartsWith(RelativeDataPath))
			{
				return false;
			}

			string filepath = relativeFilepath.Replace(RelativeDataPath, string.Empty);
			bool affectsAnyPages = DataAffectsAnyPages(filepath);
			if (affectsAnyPages)
			{
				return true;
			}

			return true;
		}

		private bool IsViewFile(string relativeFilepath)
		{
			

			if (!relativeFilepath.EndsWith(FILE_EXTENSION_VIEW))
			{
				return false;
			}

			if (!relativeFilepath.StartsWith(RelativeViewsPath))
			{
				return false;
			}

			string viewPath = relativeFilepath.Replace(RelativeViewsPath, string.Empty);
			bool affectsAnyPages = ViewAffectsAnyPages(viewPath);
			if (affectsAnyPages)
			{
				return true;
			}

			bool affectsLayoutTemplates = FileAffectsLayoutTemplates(relativeFilepath);
			return affectsLayoutTemplates;
		}

		private bool ViewAffectsAnyPages(string relativeFilepath)
		{
			string waxViewFile = GetViewFilePath(relativeFilepath);
			foreach(Page page in _pages)
			{
				if (page.ContainsView(waxViewFile))
				{
					return true;
				}
			}
			return false;
		}

		private bool DataAffectsAnyPages(string relativeFilepath)
		{
			string waxDataFile = GetDataFilePath(relativeFilepath);
			foreach (Page page in _pages)
			{
				if (page.ContainsData(waxDataFile))
				{
					return true;
				}
			}
			return false;
		}

		private string GetDataFilePath(string relativeFilepath)
		{
			return relativeFilepath.Replace(FILE_EXTENSION_DATA, string.Empty);
		}

		private string GetViewFilePath(string relativeFilepath)
		{
			relativeFilepath = relativeFilepath.Replace(FILE_EXTENSION_VIEW, string.Empty);
			relativeFilepath = relativeFilepath.Replace(RelativeViewsPath, string.Empty);
			return relativeFilepath;
		}


		private bool FileAffectsLayoutTemplates(string relativeFilepath)
		{
			return relativeFilepath.StartsWith(RelativeTemplatePath);
		}

		public bool FileIsTheWaxDefinitionFile(string filepath)
		{
			if (filepath.EndsWith("Waxfile"))
			{
				return true;
			}

			return false;
		}

		public IEnumerable<Page> GetPagesAffectedByFile(string filepath)
		{
			filepath = FileSystemSlashes.EnsureFilesystemSlashes(filepath);
			string relativeFilepath = filepath.Replace(RootPath, string.Empty);

			bool fileIsWaxfile = FileIsTheWaxDefinitionFile(relativeFilepath);
			if (fileIsWaxfile)
			{
				// Regenerate the Settings.

			}

			IEnumerable<Page> allPages = GetPages();
			bool fileAffectsLayout = FileAffectsLayoutTemplates(relativeFilepath);
			if (fileAffectsLayout || fileIsWaxfile)
			{
				return allPages;
			}

			string cleanedFilename = relativeFilepath;
			bool isDataFile = IsDataFile(relativeFilepath);
			bool isViewFile = IsViewFile(relativeFilepath);
			if (isDataFile)
			{
				cleanedFilename = GetDataFilePath(relativeFilepath);
				cleanedFilename = cleanedFilename.Replace(RelativeDataPath, string.Empty);
			}
			else if (isViewFile)
			{
				cleanedFilename = GetViewFilePath(relativeFilepath);
			}

			List<Page> affectedPages = new List<Page>();
			foreach(Page page in allPages)
			{
				if (isDataFile)
				{
					if (page.ContainsData(cleanedFilename))
					{
						affectedPages.Add(page);
					}
				}
				else if (isViewFile)
				{
					if (page.ContainsView(cleanedFilename))
					{
						affectedPages.Add(page);
					}
				}
				else
				{
					if (page.ContainsFile(cleanedFilename)) // Shot in the dark, this will probably never match anything
					{
						affectedPages.Add(page);
					}
				}
			}

			return affectedPages;
		}
	}
}
