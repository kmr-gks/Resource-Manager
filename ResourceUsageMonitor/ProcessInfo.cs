using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ResourceUsageMonitor
{
	internal class ProcessInfo
	{

		private string TrimExeExtension(string fileName)
		{
			var index = fileName.LastIndexOf(".exe");
			if (index > 0)
				fileName = fileName.Substring(0, index);
			return fileName;
		}

		public string SearchProcess(string processName)
		{
			processName = TrimExeExtension(processName);
			var processes = Process.GetProcessesByName(processName);
			var result = processes.Length switch
			{
				0 => "No process found.",
				1 => $"A process found(id:{processes[0].Id}).",
				_ => $"{processes.Length} processes found.",
			};
			return result;
		}
		public void SetPriorityLow(string processName)
		{
			processName = TrimExeExtension(processName);
			var processes = Process.GetProcessesByName(processName);
			foreach (var process in processes)
			{
				process.PriorityClass = ProcessPriorityClass.Idle;
				process.ProcessorAffinity = new IntPtr(1);
			}
		}

		public void Terminate(string processName)
		{
			processName = TrimExeExtension(processName);
			var processes = Process.GetProcessesByName(processName);
			foreach (var process in processes)
			{
				process.Kill();
			}
		}
	}
}
