using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ResourceUsageMonitor
{
	internal class ProcessInfo
	{
		private static readonly int coreCount = Environment.ProcessorCount;
		private readonly List<string> processNamesToLowerPriority = new List<string>();
		private readonly ListView LowerPriorityProcessListView;

		public void OnAddProcess(object sender, RoutedEventArgs e, string processName)
		{
			processNamesToLowerPriority.Add(processName);
			LowerPriorityProcessListView.ItemsSource = processNamesToLowerPriority;
		}
		public void OnDeleteProcess(object sender, RoutedEventArgs e)
		{
			processNamesToLowerPriority.RemoveAt(LowerPriorityProcessListView.SelectedIndex);
			LowerPriorityProcessListView.ItemsSource = processNamesToLowerPriority;
		}

		public ProcessInfo(ListView LowerPriorityProcessListView)
		{
			this.LowerPriorityProcessListView = LowerPriorityProcessListView;
			processNamesToLowerPriority.Add("chrome.exe");
			processNamesToLowerPriority.Add("explorer.exe");

			LowerPriorityProcessListView.ItemsSource = processNamesToLowerPriority;
		}

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
				if (coreCount < 64)
				{
					//最後のコアのみ割り当てる
					process.ProcessorAffinity = new IntPtr(1 << (coreCount - 1));
				}
				else
				{
					//コア数が64以上のときは、最後のコアのみ割り当てる
					process.ProcessorAffinity = new IntPtr(1 << 63);
				}
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
