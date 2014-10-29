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
		private const int EXIT_CODE_SUCCESS = 0;

		public event Log OnLog;
		public delegate void Log(string message);

		public event Error OnError;
		public delegate void Error(string message);

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
				startInfo.RedirectStandardError = true;
				startInfo.CreateNoWindow = false;

				try
				{
					using (Process process = new Process())
					{
						process.OutputDataReceived += OnProcessOutputDataReceived;
						process.ErrorDataReceived += OnProcessErrorDataReceived;
						process.StartInfo = startInfo;
						process.Start();
						process.BeginOutputReadLine();
						process.WaitForExit();

						if (process.ExitCode != EXIT_CODE_SUCCESS)
						{
							string errorMessage = process.StandardError.ReadToEnd();
							HandleError(errorMessage);
						}
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

		virtual protected void OnConsoleDataReceived(string data)
		{
			// For sub-classes to implement, if they care about their output
		}

		virtual protected void OnConsoleErrorReceived(string data)
		{
			// For sub-classes to implement, if they care about their errors
		}

		private void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				OnConsoleDataReceived(e.Data);
				LogIfAvailable(e.Data);
			}
		}

		private void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				HandleError(e.Data);
			}
		}

		private void HandleError(string error)
		{
			OnConsoleErrorReceived(error);
			ErrorIfAvailable(error);
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

		protected void ErrorIfAvailable(string message)
		{
			if (OnError != null)
			{
				OnError(message);
			}
		}
	}
}
