using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Waxnet.FilesystemWatcher.Actions
{
	class BaseAction
	{
		public event Log OnLog;
		public delegate void Log(string message);

		public bool IsRunning { get; private set; }
		public DateTime? LastRunTimestamp { get; private set; }

		public BaseAction()
		{
			IsRunning = false;
			LastRunTimestamp = null;
		}

		virtual public void Go()
		{
			// For sub-classes to override.
		}

		protected void ExecuteProcess(string absoluteFilePath, string arguments, string workingDirectory = null)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = absoluteFilePath;
			startInfo.Arguments = arguments;
			if (!string.IsNullOrEmpty(workingDirectory))
			{
				startInfo.WorkingDirectory = workingDirectory;
			}
			ExecuteProcess(startInfo);
		}

		protected void ExecuteProcess(ProcessStartInfo startInfo)
		{
			if (!IsRunning)
			{
				startInfo.UseShellExecute = false;
				startInfo.RedirectStandardInput = true;
				startInfo.RedirectStandardOutput = true;
				startInfo.CreateNoWindow = false;

				try
				{
					using (Process process = new Process())
					{
						process.OutputDataReceived += OnProcessOutputDataReceived;
						process.StartInfo = startInfo;
						process.Start();
						process.BeginOutputReadLine();
						process.WaitForExit();
					}
				}
				catch(Win32Exception e)
				{
					LogIfAvailable(e.Message);
				}

				LogIfAvailable("Done.");

				IsRunning = false;
				LastRunTimestamp = DateTime.Now;
			}
		}

		private void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				LogIfAvailable(e.Data);
			}
		}

		protected void LogIfAvailable(string lines)
		{
			string[] tokens = lines.Split(new string[] { "\r\n" }, StringSplitOptions.None);
			foreach(string token in tokens)
			{
				if (OnLog != null)
				{
					OnLog(token);
				}
			}
		}
	}
}
