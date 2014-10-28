using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waxnet.FilesystemWatcher.Actions;

namespace Waxnet.FilesystemWatcher
{
	class FileUpdate
	{
		public DateTime Timestamp { get; set; }
		public string Filepath { get; set; }
		public BaseAction Action { get; set; }
	}
}
