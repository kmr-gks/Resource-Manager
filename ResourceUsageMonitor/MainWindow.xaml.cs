using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ResourceUsageMonitor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly DispatcherTimer timer;
		private int sec = 0;
		private readonly int maxDataCount = 60; // 60秒分のデータを表示する
		private CpuInfo cpuInfo;

		public MainWindow()
		{
			InitializeComponent();
			cpuInfo = new(AllCpuPlotView, TabGridPerCore);

			// タイマーの設定
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			timer.Tick += ResourceUsageMonitor_Tick;
			timer.Start();


			SizeChanged += OnWindowSizeChanged;

			//最初のウィンドウの大きさに応じてグラフの大きさを変更する。(OnWindowSizeChangedと同じ内容)
			Loaded += new RoutedEventHandler((sender, e) => { OnWindowSizeChanged(null, null); });

			using var tokenSource = new CancellationTokenSource();
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			sec++;
			cpuInfo.Update(LabelCpu, maxDataCount, sec);
		}

		public Thickness LeftTopWidthHeightToMargin(double left, double top, double width = 100, double height = 100)
		{
			return new Thickness(left, top, TabGridPerCore.ActualWidth - left - width, TabGridPerCore.ActualHeight - top - height);
		}

		private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs? e)
		{
			cpuInfo.OnWindowSizeChanged((int)TabGridPerCore.ActualWidth, (int)TabGridPerCore.ActualHeight);
		}

		private void SetCpuAffinity()
		{
			//すべてのプロセス
			Process[] process;

			//プロセスの取得
			process = Process.GetProcesses();
			foreach (Process p in process)
			{
				p.ProcessorAffinity = 0x00000001;
			}
		}
	}
}
