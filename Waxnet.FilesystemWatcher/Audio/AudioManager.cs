using Space150.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Audio
{
	class AudioManager
	{
		SoundPlayer _player;

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

		public void PlaySound(string soundKey)
		{
			if (string.IsNullOrEmpty(soundKey))
			{
				throw new ArgumentRequiredException("soundKey");
			}

			Assembly assembly = Assembly.GetExecutingAssembly();
			Stream resourceStream = assembly.GetManifestResourceStream(soundKey);
			if (resourceStream != null)
			{
				if (_player == null)
				{
					_player = new SoundPlayer();
				}

				_player.Stream = resourceStream;
				_player.Play();
			}
		}
	}
}
