using System;
using System.Diagnostics;
using System.Windows;

namespace ResourceUsageMonitor
{
	internal class ProcessInfo
	{
		private static readonly int coreCount = Environment.ProcessorCount;
		private MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

		public void OnAddProcess(object sender, RoutedEventArgs e, string processName)
		{
			mainWindow.Items.Add(new LowProcessListItem { Name = TrimExeExtension(processName) + ".exe" });
		}
		public void OnDeleteProcess(object sender, RoutedEventArgs e)
		{
			try
			{
				mainWindow.Items.RemoveAt(mainWindow.LowerPriorityProcessListView.SelectedIndex);
			}
			catch (Exception)
			{
				MessageBox.Show("削除するプロセスを選択してください。");
			}
		}

		public ProcessInfo()
		{
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
			if (string.IsNullOrEmpty(processName))
			{
				return "Please input process name.";
			}
			processName = TrimExeExtension(processName);
			//GetProcessesByNameは、拡張子を削除したプロセス名で検索する。
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
