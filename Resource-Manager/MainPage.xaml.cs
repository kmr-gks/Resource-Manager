using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace Resource_Manager
{
	/// <summary>
	/// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
	/// </summary>

	public sealed partial class MainPage : Page
	{
		[DllImport("kernel32.dll")]
		extern static bool Beep(uint dwFreq, uint dwDuration);
		[DllImport("user32.dll")]
		private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

		private DispatcherTimer timer;
		private int cpuCount;
		private SystemCpuUsageReport prevCpuReport = SystemDiagnosticInfo.GetForCurrentSystem().CpuUsage.GetReport();

		public MainPage()
		{
			InitializeComponent();
			cpuUsageText.Text = "now:0";
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			timer.Tick += CpuTimer_Tick;
			timer.Start();
		}

		private void CpuTimer_Tick(object sender, object e)
		{
			cpuCount++;
			var CpuReport = SystemDiagnosticInfo.GetForCurrentSystem().CpuUsage.GetReport();
			var MemReport = SystemDiagnosticInfo.GetForCurrentSystem().MemoryUsage.GetReport();
			var kernel = CpuReport.KernelTime - prevCpuReport.KernelTime;
			var user = CpuReport.UserTime - prevCpuReport.UserTime;
			var idle = CpuReport.IdleTime - prevCpuReport.IdleTime;
			var cpuUsage = "user:" + user + " kernel:" + kernel + " idle:" + idle;
			prevCpuReport = CpuReport;
			var memUsage = "commited:" + FormatBytesToString(MemReport.CommittedSizeInBytes) + " avail:" + FormatBytesToString(MemReport.AvailableSizeInBytes) + " total:" + FormatBytesToString(MemReport.TotalPhysicalSizeInBytes);

			cpuUsageText.Text = "now:" + cpuCount.ToString() + "\nCPU:" + cpuUsage + "\nMEM:" + memUsage + "\nCPU%" + ((user + kernel) / (user + kernel + idle));
			Beep(1000, 100);
			//MessageBox((IntPtr)null, "test", "caption", 0);
		}

		string FormatBytesToString(ulong bytes)
		{
			string[] suffixes = { "B", "KB", "MB", "GB", "TB", "PB" };
			int counter = 0;
			decimal number = bytes;
			while (Math.Round(number / 1024) >= 1)
			{
				number = number / 1024;
				counter++;
			}

			return string.Format("{0:n1}{1}", number, suffixes[counter]);
		}
	}
}
