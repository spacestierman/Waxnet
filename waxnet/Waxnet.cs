using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nustache.Core;
using Space150.Common;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Waxnet.Models;
using YamlDotNet.Core;

namespace Waxnet
{
    public class Waxnet
    {
		/// <summary>
		/// Triggered when any Wax-relevant file (mock json, mustache) is changed within the Wax project.
		/// </summary>
        public event SourceFileModified OnWaxFileModified;

		/// <summary>
		/// Triggered when any file within the Wax project changes.
		/// </summary>
		public event SourceFileModified OnSourceFileModified;
		public delegate void SourceFileModified(string filepath);

		/// <summary>
		/// Triggered whenever the wax build process encounters an error.
		/// </summary>
		public event WaxBuildError OnWaxBuildError;
		public delegate void WaxBuildError(string errorMessage);

		public event WaxBuildStarted OnWaxFullBuildStarted;
		public event WaxBuildFinished OnWaxFullBuildFinished;
		public event WaxBuildStarted OnWaxBuildStarted;
		public event WaxBuildFinished OnWaxBuildFinished;
		public delegate void WaxBuildStarted(IEnumerable<Page> affectedPages);
		public delegate void WaxBuildFinished();

		public event SourceFileIgnored OnSourceFileIgnored;
		public delegate void SourceFileIgnored(string filepath);

		public event WaxSettingsChanged OnWaxDefinitionChanged;
		public delegate void WaxSettingsChanged(WaxnetSettings newSettings);

		public event WaxParseErrorEncountered OnWaxParseError;
		public delegate void WaxParseErrorEncountered(WaxParseError error);

		public IWaxLogger Logger { get; set; }

		private WaxnetSettings _settings;
		private FileSystemWatcher _watcher;
		public string OutputPath { get; private set; }
		private List<string> _ignorePatterns;

		public Waxnet(string outputPath, IWaxLogger logger = null)
        {
			OutputPath = FileSystemSlashes.EnsureFileSystemTrailingSlash(outputPath);
			Logger = logger;
			_ignorePatterns = new List<string>();
        }

		public Waxnet(WaxnetSettings settings, string outputPath, IWaxLogger logger = null)
        {
			if (settings == null)
			{
				throw new ArgumentRequiredException("settings");
			}

			_settings = settings;
			OutputPath = FileSystemSlashes.EnsureFileSystemTrailingSlash(outputPath);
			Logger = logger;
			_ignorePatterns = new List<string>();

			_watcher = CreateWatcher(_settings);
        }

		public void LoadWaxFile(string waxFilePath)
		{
			_settings = ParseWaxFile(waxFilePath);
			if (_settings == null)
			{
				throw new WaxnetException("Unable to parse Waxfile.");
			}
			else
			{
				DestroyWatcherIfExists(); // The wax file paths may have changed, so completely reload a new file system watcher.
				_watcher = CreateWatcher(_settings);
			}
		}

		public WaxnetSettings GetSettings()
		{
			if (_settings == null)
			{
				throw new WaxnetException("Can't get the settings until you've loaded some wax or supplied your own settings.");
			}

			WaxnetSettings settings = _settings.DeepClone(); // We don't want users to modify the settings from under us.
			return settings;
		}

		public void IgnoreFilesMatchingPattern(string pattern)
		{
			_ignorePatterns.Add(pattern);
		}

		public void IgnoreFilesMatchingPatterns(params string[] patterns)
		{
			foreach(string pattern in patterns)
			{
				IgnoreFilesMatchingPattern(pattern);
			}
		}

		public void StartWatching()
		{
			EnableWatcher(_watcher);
		}

		public void StopWatching()
		{
			DisableWatcher(_watcher);
		}

		public void ManualBuild()
		{
			RebuildAllMustaches(_settings);
		}

		private FileSystemWatcher CreateWatcher(WaxnetSettings settings)
		{
			FileSystemWatcher watcher = new FileSystemWatcher(settings.RootPath);
			watcher.IncludeSubdirectories = true;
			watcher.Changed += onWatcherChanged;
			watcher.Created += onWatcherCreated;
			watcher.Deleted += onWatcherDeleted;
			watcher.Renamed += onWatcherRenamed;
			
			return watcher;
		}

		private void DestroyWatcherIfExists()
		{
			if (_watcher != null)
			{
				_watcher.Changed -= onWatcherChanged;
				_watcher.Created -= onWatcherCreated;
				_watcher.Deleted -= onWatcherDeleted;
				_watcher.Renamed -= onWatcherRenamed;
				_watcher = null;
			}
		}

		private void EnableWatcher(FileSystemWatcher watcher)
		{
			watcher.EnableRaisingEvents = true;
			LogIfLoggerConfigured("Watching {0}", watcher.Path);
		}

		private void DisableWatcher(FileSystemWatcher watcher)
		{
			watcher.EnableRaisingEvents = false;
			LogIfLoggerConfigured("Ignoring {0}", watcher.Path);
		}

		private void onWatcherCreated(object sender, FileSystemEventArgs e)
		{
			LogIfLoggerConfigured("onWatcherCreated() " + e.Name + " | " + e.FullPath);
			HandleFileEvent(e.FullPath);
		}

		private void onWatcherChanged(object sender, FileSystemEventArgs e)
		{
			LogIfLoggerConfigured("onWatcherChanged() " + e.Name + " | " + e.FullPath);
			HandleFileEvent(e.FullPath);
		}

		private void onWatcherDeleted(object sender, FileSystemEventArgs e)
		{
			LogIfLoggerConfigured("onWatcherDeleted() " + e.Name + " | " + e.FullPath);
			HandleFileEvent(e.FullPath);
		}

		private void onWatcherRenamed(object sender, RenamedEventArgs e)
		{
			LogIfLoggerConfigured("onWatcherRenamed() " + e.Name + " | " + e.FullPath);
			HandleFileEvent(e.FullPath);
		}

		private void HandleFileEvent(string filepath)
		{
			if (OnSourceFileModified != null)
			{
				OnSourceFileModified(filepath);
			}

			bool ignoreFile = ShouldFileBeIgnored(filepath);
			if (ignoreFile)
			{
				if (OnSourceFileIgnored != null)
				{
					OnSourceFileIgnored(filepath);
				}
			}
			else
			{
				RebuildIfFileIsRelevantToWax(_settings, filepath);
			}
		}

		private bool ShouldFileBeIgnored(string filepath)
		{
			foreach(string pattern in _ignorePatterns)
			{
				Regex regex = new Regex(pattern);
				if (regex.IsMatch(filepath))
				{
					return true;
				}
			}

			return false;
		}

		private void LogIfLoggerConfigured(string message)
		{
			if (Logger != null)
			{
				Logger.Log(message);
			}
		}

		private void LogIfLoggerConfigured(string format, params object[] args)
		{
			LogIfLoggerConfigured(string.Format(format, args));
		}

		private WaxnetSettings ParseWaxFile(string filepath)
		{
			WaxnetSettings settings = null;
			WaxFileParser parser = new WaxFileParser();
			try
			{
				settings = parser.ParseFile(filepath);
			}
			catch (SyntaxErrorException syntaxError)
			{
				if (OnWaxParseError != null)
				{
					WaxParseError error = new WaxParseError(syntaxError);
					OnWaxParseError(error);
				}
			}
			catch (Exception e)
			{

			}

			return settings;
		}

		private void RebuildIfFileIsRelevantToWax(WaxnetSettings settings, string filepath)
		{
			bool isTheWaxFile = settings.FileIsTheWaxDefinitionFile(filepath);
			if (isTheWaxFile)
			{
				settings = ParseWaxFile(filepath);
				if (settings == null)
				{
					return; // Can't do anything else here if the wax file couldn't be parsed.  The above method handles notifying stakeholders on failures.
				}
				else 
				{
					if (OnWaxDefinitionChanged != null)
					{
						OnWaxDefinitionChanged(settings.DeepClone());
					}
				}
			}

			if (IsFileWaxRelevant(settings, filepath))
			{
				if (OnWaxFileModified != null)
				{
					OnWaxFileModified(filepath);
				}

				IEnumerable<Page> affectedPages = settings.GetPagesAffectedByFile(filepath);
				RebuildPages(_settings, affectedPages);
			}
		}

		private bool IsFileWaxRelevant(WaxnetSettings settings, string filepath)
		{
			bool fileAffectsPages = settings.FileAffectsWax(filepath);
			return fileAffectsPages;
		}

		private void RebuildAllMustaches(WaxnetSettings settings)
		{
			LogIfLoggerConfigured("rebuildAllMustaches");

			IEnumerable<Page> pages = settings.GetPages();

			if (OnWaxFullBuildStarted != null)
			{
				OnWaxFullBuildStarted(pages);
			}

			RebuildWaxIndexFile(settings, pages);
			RebuildPages(settings, pages);

			if (OnWaxFullBuildFinished != null)
			{
				OnWaxFullBuildFinished();
			}
		}

		private void RebuildWaxIndexFile(WaxnetSettings settings, IEnumerable<Page> pages)
		{
			EnsureDirectoryExists(OutputPath);

			string waxIndexFileContents = GetWaxPageListingContent(settings, pages);
			dynamic layoutData = new ExpandoObject();
			layoutData.content = waxIndexFileContents;

			string layoutTemplatePath = settings.AbsoluteTemplatePath + "layout.mustache";
			string layoutContents = File.ReadAllText(layoutTemplatePath);
			string pageContent = Render.FileToString(layoutTemplatePath, layoutData);

			string outputPath = OutputPath + "index.html";
			File.WriteAllText(outputPath, pageContent);
		}

		private string GetWaxPageListingContent(WaxnetSettings settings, IEnumerable<Page> pages)
		{
			List<ExpandoObject> pagesData = new List<ExpandoObject>();
			foreach (Page page in pages)
			{
				dynamic pageData = new ExpandoObject();
				pageData.url = FileSystemSlashes.EnsureWebSafeSlashes(GetRelativeWaxBuildFilePathForPage(page));
				pageData.name = page.Name;
				pagesData.Add(pageData);
			}

			dynamic data = new ExpandoObject();
			data.pages = pagesData;

			string indexViewPath = settings.AbsoluteTemplatePath + "index.mustache";
			string templateContents = File.ReadAllText(indexViewPath);
			string content = Render.StringToString(templateContents, data);
			return content;
		}

		private void RebuildPages(WaxnetSettings settings, IEnumerable<Page> pages)
		{
			if (OnWaxBuildStarted != null)
			{
				OnWaxBuildStarted(pages);
			}

			EnsureDirectoryExists(OutputPath);

			foreach(Page page in pages)
			{
				string outputDirectory = OutputPath + GetRelativeWaxBuildDirectoryForPage(page);
				EnsureDirectoryExists(outputDirectory);

				string content = CreateContentForPage(settings, page);

				string outputFile = OutputPath + GetRelativeWaxBuildFilePathForPage(page);
				File.WriteAllText(outputFile, content);
			}

			if (OnWaxBuildFinished != null)
			{
				OnWaxBuildFinished();
			}
		}

		private string GetRelativeWaxBuildDirectoryForPage(Page page)
		{
			string outputDirectory = CreateSafePageDirectoryName(page.Name) + @"\";
			return outputDirectory;
		}

		private string GetRelativeWaxBuildFilePathForPage(Page page)
		{
			string outputDirectory = CreateSafePageDirectoryName(page.Name) + @"\";
			string outputFile = "index.html";
			return outputDirectory + outputFile;
		}

		private void EnsureDirectoryExists(string directory)
		{
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}

		private string CreateSafePageDirectoryName(string pageName)
		{
			pageName = pageName.Replace(" ", string.Empty);
			return pageName;
		}

		private string CreateContentForPage(WaxnetSettings settings, Page page)
		{
			string layoutTemplatePath = settings.AbsoluteTemplatePath + "layout.mustache";
			string layoutContents = File.ReadAllText(layoutTemplatePath);

			Dictionary<string, object> layoutData = new Dictionary<string, object>();

			IEnumerable<string> layoutBlocks = page.GetLayoutKeys();
			foreach(string layoutBlock in layoutBlocks)
			{
				StringBuilder content = new StringBuilder();

				IEnumerable<PageContent> modules = page.GetModulesForKey(layoutBlock);
				foreach(PageContent module in modules)
				{
					string moduleViewPath = settings.AbsoluteViewsPath + FileSystemSlashes.EnsureFilesystemSlashes(module.ViewPath) + ".mustache";
					if (!File.Exists(moduleViewPath))
					{
						if (OnWaxBuildError != null)
						{
							string errorMessage = string.Format("Unable to find view file \"{0}\" for page \"{1}\" and data path \"{0}\".", module.ViewPath, page.Name, module.DataPath);
							OnWaxBuildError(errorMessage);
						}
						continue;
					}

					string dataPath = settings.AbsoluteDataPath + FileSystemSlashes.EnsureFilesystemSlashes(module.DataPath) + ".json";
					if (!File.Exists(dataPath))
					{
						if (OnWaxBuildError != null)
						{
							string errorMessage = string.Format("Unable to find data file \"{0}\" for page \"{1}\" and view path \"{0}\".", module.DataPath, page.Name, module.ViewPath);
							OnWaxBuildError(errorMessage);
						}
						continue;
					}

					string json = File.ReadAllText(dataPath);
					object moduleData = JsonConvert.DeserializeObject(json);
					string moduleView = File.ReadAllText(moduleViewPath);
					string moduleContent = Render.StringToString(moduleView, moduleData);

					content.AppendLine(moduleContent);
				}

				string layoutContent = content.ToString();
				layoutData[layoutBlock] = layoutContent;
			}

			string pageContent = Render.FileToString(layoutTemplatePath, layoutData);

			return pageContent;
		}
    }
}
