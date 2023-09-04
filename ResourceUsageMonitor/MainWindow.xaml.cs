using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace ResourceUsageMonitor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private DispatcherTimer timer;
		private int sec = 0;
		private readonly int maxDataCount = 60; // 60秒分のデータを表示する
		private readonly int coreCount = Environment.ProcessorCount;//コア数
		private double[] cpuUsageArray;
		private readonly string cpuName;

		// OxyPlot のモデルとコントローラー
		private PlotModel plotModel { get; } = new PlotModel();
		private LineSeries lineSeries { get; } = new LineSeries();

		public MainWindow()
		{
			InitializeComponent();
			// タイマーの設定
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(1000)
			};
			timer.Tick += ResourceUsageMonitor_Tick;
			timer.Start();

			//グラフの初期化
			cpuUsageArray = new double[coreCount + 1];
			cpuName = GetCpuName();
			GraphSetup();
			using var tokenSource = new CancellationTokenSource();
		}

		private string GetCpuName()
		{
			var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");
			string cpuname = "";
			foreach (var obj in searcher.Get())
			{
				cpuname = obj["Name"].ToString();
			}
			return cpuname;
		}

		//グラフの見た目をつくる
		private void GraphSetup()
		{
			// X軸とY軸の設定
			var AxisX = new LinearAxis()
			{
				Position = AxisPosition.Bottom,
				TitleFontSize = 16,
				Title = "X軸"
			};

			var AxisY = new LinearAxis()
			{
				Position = AxisPosition.Left,
				TitleFontSize = 16,
				Title = "Y軸"
			};

			plotModel.Axes.Add(AxisX);
			plotModel.Axes.Add(AxisY);
			plotModel.Background = OxyColors.White;
			plotModel.Axes[0].Minimum = 0;
			plotModel.Axes[0].Maximum = 60;
			plotModel.Axes[1].Minimum = 0;
			plotModel.Axes[1].Maximum = 100;

			//折れ線グラフの設定
			lineSeries.StrokeThickness = 1.5;
			lineSeries.Color = OxyColor.FromRgb(0, 100, 205);

			plotModel.Series.Add(lineSeries);

			PlotView.Model = plotModel;
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			int cpuPercent = 0;
			var cpuUsage = GetCpuUsage(ref cpuPercent);
			Label_CpuStatus.Content = "CPU " + cpuName + " " + cpuUsageArray[coreCount] + "%\n" + cpuUsage;
			DrawReflesh(cpuPercent);
		}

		private string GetCpuUsage_old1()
		{
			var coreCount = Environment.ProcessorCount;
			var usageString = "Cores:" + coreCount;
			PerformanceCounter[] cpuCounters = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				cpuCounters[i] = new PerformanceCounter("Processor", "% Processor Time", "_Total");
				usageString += " " + cpuCounters[i].NextValue() + "%,";
			}
			double totalCpuUsage = 0;
			foreach (var counter in cpuCounters)
			{
				totalCpuUsage += (double)counter.NextValue();
			}
			return usageString;

		}
		private string GetCpuUsage_old2()
		{
			var searcher = new System.Management.ManagementObjectSearcher("select LoadPercentage from CIM_Processor");

			string usageString = "CPU: ";
			foreach (var obj in searcher.Get())
			{
				var usage = obj["LoadPercentage"];
				usageString += usage + "%, ";
			}
			return usageString;
		}
		private string GetCpuUsage_old3()
		{
			string displayText = "";
			//すべてのリソース値を取得するにはこのように書く。
			//searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor where name=\"_Total\"");
			var searcher = new ManagementObjectSearcher("select * from Win32_Processor");

			using (ManagementObjectCollection queryCollection = searcher.Get())
			{
				foreach (ManagementObject m in queryCollection)
				{
					foreach (PropertyData prop in m.Properties)
					{
						displayText += prop.Name + ", " + prop.Value + "\n";
					}
					m.Dispose();
				}
			}
			return displayText;
		}
		private string GetCpuUsage(ref int cpuPercent)
		{
			var coreCount = Environment.ProcessorCount;
			var searcher = new ManagementObjectSearcher("select PercentProcessorTime,Name from Win32_PerfFormattedData_PerfOS_Processor");
			var usageString = "CPU: ";
			try
			{
				var cpuCounter = new PerformanceCounter("Processor Information", "% Processor Performance", "_Total");
				var cpuMultiplier = cpuCounter.NextValue();//これが常に0になる
				usageString += cpuMultiplier + " ";
			}
			catch
			{
				usageString += "error";
			}

			foreach (var obj in searcher.Get())
			{
				var usage = obj["PercentProcessorTime"];
				var name = obj["Name"];
				if (name.ToString() == "_Total")
				{
					cpuUsageArray[coreCount] = (ulong)usage;
				}
				else
				{
					if (name.ToString() != null)
					{
						var coreNumber = int.Parse(name.ToString());
						cpuUsageArray[coreNumber] = (ulong)usage;
					}
				}
			}
			for (int i = 0; i < coreCount; i++)
			{
				usageString += cpuUsageArray[i] + "%, ";
			}
			usageString += "Total " + cpuUsageArray[coreCount] + "\n";

			cpuPercent = (int)cpuUsageArray[coreCount];
			if (cpuPercent > 100) cpuPercent = 100;
			return usageString;
		}

		void DrawReflesh(int cpuPercent)
		{
			sec++;
			//データ数が maxDataCount を超えたらデキューしていく
			if (lineSeries.Points.Count > maxDataCount)
			{
				lineSeries.Points.RemoveAt(0);
				plotModel.Axes[0].Minimum++;
				plotModel.Axes[0].Maximum++;
			}
			lineSeries.Points.Add(new DataPoint(sec, cpuPercent));
			plotModel.InvalidatePlot(true);
		}
	}
}
