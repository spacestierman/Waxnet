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
using Waxnet.FilesystemWatcher.Audio;

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

		private DateTime? _lastNotification;

		private AudioManager _audioManager;
		private string _actionCompletedSoundKey;
		private string _emptyQueueSoundKey;
		private string _errorSoundKey;

		public ApplicationWatcher(string directory)
		{
			_ignorePatterns = new List<string>();
			_actionsToRun = new Queue<FileUpdate>();

			_actionFactory = new ApplicationActionFactory(directory, OnActionLog, OnActionError);

			_watcher = CreateWatcher(directory);
			
			EnableWatcher(_watcher);

			_audioManager = new AudioManager();

			_actionCompletedSoundKey = _audioManager.GetKeyEndingWith("LoadScript.wav");
			if (string.IsNullOrEmpty(_actionCompletedSoundKey))
			{
				throw new Exception("Unable to find action complete sound key");
			}

			_emptyQueueSoundKey = _audioManager.GetKeyEndingWith("Hammer.wav");
			if (string.IsNullOrEmpty(_emptyQueueSoundKey))
			{
				throw new Exception("Unable to find empty queue sound key");
			}

			_errorSoundKey = _audioManager.GetKeyEndingWith("LoadScriptError.wav");
			if (string.IsNullOrEmpty(_errorSoundKey))
			{
				throw new Exception("Unable to find error sound key");
			}

			_timer = new Timer(OnQueueTimerElapsed, 5, 0, 250);
			Log("Watching {0}...", directory);
		}

		public void ManualBuild()
		{
			AddToQueue(CreateFileUpdateFor(string.Empty, _actionFactory.CreateWaxAction()));
			AddToQueue(CreateFileUpdateFor(string.Empty, _actionFactory.CreateCoffeeAction()));
			AddToQueue(CreateFileUpdateFor(string.Empty, _actionFactory.CreateSassAction()));
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
			Log(message);
		}

		private void OnActionError(string message)
		{
			Log(message);
		}

		private void Log(string message)
		{
			string timestamp = DateTime.Now.ToString("HH:mm:ss.ffff");
			string formattedMessage = string.Format("{0}: {1}", timestamp, message);
			Console.WriteLine(formattedMessage);
			_lastNotification = DateTime.Now;
		}

		private void Log(string format, params object[] args)
		{
			Log(string.Format(format, args));
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
				_runningUpdate = _actionsToRun.Dequeue();
				_runningUpdate.Action.Go();
				
				if (_actionsToRun.Count <= 0)
				{
					_audioManager.QueueSound(_emptyQueueSoundKey);
				}
				else if (!_runningUpdate.Action.ExitStatusCode.HasValue)
				{
					
				}
				else if (_runningUpdate.Action.ExitStatusCode.Value == BaseAction.EXIT_CODE_SUCCESS)
				{
					_audioManager.QueueSound(_actionCompletedSoundKey);
				}
				else
				{
					_audioManager.QueueSound(_errorSoundKey);
				}

				_runningUpdate = null;
			}
			else
			{
				if (_lastNotification.HasValue)
				{
					TimeSpan difference = DateTime.Now.Subtract(_lastNotification.Value);
					if (difference.TotalMinutes > 5)
					{
						Log("No changes...");
					}
				}
			}
		}
	}
}
