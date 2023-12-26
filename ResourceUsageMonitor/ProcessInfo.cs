using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace ResourceUsageMonitor
{
	internal class ProcessInfo
	{
		private static readonly int coreCount = Environment.ProcessorCount;
		private readonly MainWindow mainWindow = (MainWindow)Application.Current.MainWindow;

		public void OnAddProcess(object sender, RoutedEventArgs e)
		{
			mainWindow.Items.Add(new(TrimExeExtension(mainWindow.TextBoxAddProcessToList.Text) + ".exe"));
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

		private static string TrimExeExtension(string fileName)
		{
			var index = fileName.LastIndexOf(".exe");
			if (index > 0)
				fileName = fileName[..index];
			return fileName;
		}

		public void SearchProcess(object sender, TextChangedEventArgs e)
		{
			var processName = mainWindow.TextBoxOneProcess.Text;
			if (string.IsNullOrEmpty(processName))
			{
				mainWindow.LabelProcessSearchresult.Content = "Please input process name.";
				return;
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
			mainWindow.LabelProcessSearchresult.Content = result;
		}

		public void SetPriorityLow(object sender, RoutedEventArgs e)
		{
			var processName = TrimExeExtension(mainWindow.TextBoxOneProcess.Text);
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

		public void Terminate(object sender, RoutedEventArgs e)
		{
			var processName = TrimExeExtension(mainWindow.TextBoxOneProcess.Text);
			var processes = Process.GetProcessesByName(processName);
			foreach (var process in processes)
			{
				process.Kill();
			}
		}
	}
}
