using Space150.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Audio
{
	class AudioManager
	{
		SoundPlayer _player;

		private Queue<string> _queued;
		private System.Threading.Timer _timer;

		public AudioManager()
		{
			_queued = new Queue<string>();
			_timer = new Timer(OnQueueTimerElapsed, 5, 0, 1);
		}

		public IEnumerable<string> GetSoundKeys()
		{
			Assembly assembly = GetAssembly();
			string[] names = assembly.GetManifestResourceNames();
			return names;
		}

		public string GetKeyEndingWith(string endsWith)
		{
			string endsWithLowered = endsWith.ToLower();
			IEnumerable<string> keys = GetSoundKeys();
			foreach (string key in keys)
			{
				if (key.ToLower().EndsWith(endsWithLowered))
				{
					return key;
				}
			}

			return null;
		}

		private Assembly GetAssembly()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			return assembly;
		}

		private void OnQueueTimerElapsed(object state)
		{
			if (_player == null && _queued.Count > 0)
			{
				string soundKey = _queued.Dequeue();
				PlaySoundImmediately(soundKey);
			}
		}

		public void PlaySoundImmediately(string soundKey)
		{
			if (string.IsNullOrEmpty(soundKey))
			{
				throw new ArgumentRequiredException("soundKey");
			}

			Assembly assembly = Assembly.GetExecutingAssembly();
			Stream resourceStream = assembly.GetManifestResourceStream(soundKey);
			if (resourceStream != null)
			{
				_player = new SoundPlayer(resourceStream);
				Action<SoundPlayer> play = p =>
				{
					_player.PlaySync();
					OnSoundPlayComplete();
				};
				play.BeginInvoke(_player, null, null);
			}
		}

		public void QueueSound(string soundKey)
		{
			_queued.Enqueue(soundKey);
		}

		private void OnSoundPlayComplete()
		{
			_player = null;
		}
	}
}
