using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Waxnet.FilesystemWatcher.Actions;

namespace Waxnet.FilesystemWatcher
{
	public class ApplicationWatcher
	{
		private List<string> _ignorePatterns;
		private FileSystemWatcher _watcher;

		private ApplicationActionFactory _actionFactory;

		private Queue<FileUpdate> _actionsToRun;
		private FileUpdate _runningUpdate;
		private System.Threading.Timer _timer;

		public ApplicationWatcher(string directory)
		{
			_ignorePatterns = new List<string>();
			_actionsToRun = new Queue<FileUpdate>();

			_actionFactory = new ApplicationActionFactory(directory, OnActionLog);

			_watcher = CreateWatcher(directory);
			
			EnableWatcher(_watcher);

			_timer = new Timer(OnQueueTimerElapsed, 5, 0, 250);
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

		private void RegisterCompileActions(params BaseAction[] actions)
		{
			foreach(BaseAction action in actions)
			{
				action.OnLog += OnActionLog;
			}
		}

		private void OnActionLog(string message)
		{
			Console.WriteLine(message);
		}

		private FileSystemWatcher CreateWatcher(string directory)
		{
			FileSystemWatcher watcher = new FileSystemWatcher(directory);
			watcher.IncludeSubdirectories = true;
			watcher.Changed += onWatcherChanged;
			watcher.Created += onWatcherCreated;
			watcher.Deleted += onWatcherDeleted;
			watcher.Renamed += onWatcherRenamed;

			return watcher;
		}

		private void EnableWatcher(FileSystemWatcher watcher)
		{
			watcher.EnableRaisingEvents = true;
		}

		private void DisableWatcher(FileSystemWatcher watcher)
		{
			watcher.EnableRaisingEvents = false;
		}

		private void onWatcherCreated(object sender, FileSystemEventArgs e)
		{
			HandleFileEvent(e.FullPath);
		}

		private void onWatcherChanged(object sender, FileSystemEventArgs e)
		{
			HandleFileEvent(e.FullPath);
		}

		private void onWatcherDeleted(object sender, FileSystemEventArgs e)
		{
			HandleFileEvent(e.FullPath);
		}

		private void onWatcherRenamed(object sender, RenamedEventArgs e)
		{
			HandleFileEvent(e.FullPath);
		}

		private void HandleFileEvent(string filepath)
		{
			bool ignoreFile = ShouldFileBeIgnored(filepath);
			if (!ignoreFile)
			{
				RebuildIfFileIsRelevant(filepath);
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

		private void RebuildIfFileIsRelevant(string filepath)
		{
			bool fileIsRelevantToWax = IsFileWaxRelevant(filepath);
			if (fileIsRelevantToWax)
			{
				AddToQueue(CreateFileUpdateFor(filepath, _actionFactory.CreateWaxAction()));
			}

			bool fileIsCoffeescript = IsFileCoffeescript(filepath);
			if (fileIsCoffeescript)
			{
				AddToQueue(CreateFileUpdateFor(filepath, _actionFactory.CreateCoffeeAction()));
			}

			bool fileIsSass = IsFileSass(filepath);
			if (fileIsSass)
			{
				AddToQueue(CreateFileUpdateFor(filepath, _actionFactory.CreateSassAction()));
			}
		}

		private FileUpdate CreateFileUpdateFor(string filepath, BaseAction action)
		{
			FileUpdate update = new FileUpdate()
			{
				Timestamp = DateTime.Now,
				Filepath = filepath,
				Action = action
			};
			return update;
		}

		private void AddToQueue(FileUpdate update)
		{
			if (!_actionsToRun.Contains(update))
			{
				if (!QueueContainsActionType(update.Action.GetType()))
				{
					_actionsToRun.Enqueue(update);
				}
			}
		}

		private bool QueueContainsActionType(Type type)
		{
			foreach(FileUpdate update in _actionsToRun)
			{
				if (update.Action.GetType() == type)
				{
					return true;
				}
			}
			return false;
		}

		private bool IsFileWaxRelevant(string filepath)
		{
			string filepathLowered = filepath.ToLower();
			bool isTheWaxFile = filepathLowered.EndsWith("waxfile");
			if (isTheWaxFile)
			{
				return true;
			}

			bool isWaxRelevant = filepathLowered.EndsWith(".json") || filepathLowered.EndsWith(".mustache");
			if (isWaxRelevant)
			{
				return true;
			}

			return false;
		}

		private bool IsFileCoffeescript(string filepath)
		{
			string filepathLowered = filepath.ToLower();
			return filepathLowered.EndsWith(".coffee");
		}

		private bool IsFileSass(string filepath)
		{
			string filepathLowered = filepath.ToLower();
			return filepathLowered.EndsWith(".scss");
		}

		private void OnQueueTimerElapsed(object state)
		{
			if (_runningUpdate == null && _actionsToRun.Count > 0)
			{
				_runningUpdate = _actionsToRun.Peek();
				_runningUpdate.Action.Go();
				_runningUpdate = null;
				_actionsToRun.Dequeue();
			}
		}
	}
}
