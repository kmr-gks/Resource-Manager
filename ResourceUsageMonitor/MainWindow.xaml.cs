using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;
using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
		int sec = 0;
		const int maxDataCount = 60; // 60秒分のデータを表示する

		// OxyPlot のモデルとコントローラー
		PlotModel plotModel { get; } = new PlotModel();
		LineSeries lineSeries { get; } = new LineSeries();

		public MainWindow()
		{
			InitializeComponent();
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(1000)
			};
			timer.Tick += ResourceUsageMonitor_Tick;
			timer.Start();
			GraphSetup();
			using var tokenSource = new CancellationTokenSource();
			//_ = Draw(tokenSource);
		}

		//グラフの見た目をつくる
		void GraphSetup()
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

			for (int i = 0; i < maxDataCount; i++)
			{
				lineSeries.Points.Add(new DataPoint(i, 0));
			}

			PlotView.Model = plotModel;
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			int cpuPercent = 0;
			var cpuUsage = GetCpuUsage(ref cpuPercent);
			LabelTest.Content = cpuUsage + "\n";
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
		private string GetCpuUsage(ref int cpuPercent)
		{
			var coreCount = Environment.ProcessorCount;
			var usageArray = new double[coreCount + 1];
			var searcher = new ManagementObjectSearcher("select PercentIdleTime,Name from Win32_PerfFormattedData_PerfOS_Processor");
			var usageString = "CPU: ";

			foreach (var obj in searcher.Get())
			{
				var usage = obj["PercentIdleTime"];
				var name = obj["Name"];
				if (name.ToString() == "_Total")
				{
					usageArray[coreCount] = 100 - (ulong)usage;
				}
				else
				{
					if (name.ToString() != null)
					{

						var coreNumber = int.Parse(name.ToString());
						usageArray[coreNumber] = 100 - (ulong)usage;
					}
				}
			}
			for (int i = 0; i < coreCount; i++)
			{
				usageString += usageArray[i] + "%, ";
			}
			usageString += "Total " + usageArray[coreCount] + "\n";

			/*
			//すべてのリソース値を取得するにはこのように書く。
			var searcher = new System.Management.ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor where name=\"_Total\"");
			using (ManagementObjectCollection queryCollection = searcher.Get())
			{
				foreach (ManagementObject m in queryCollection)
				{
					foreach (PropertyData prop in m.Properties)
					{
					usageString+= prop.Name+", "+ prop.Value+"\n";
					}
					m.Dispose();
				}
			}
			*/
			cpuPercent = (int)usageArray[coreCount];
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
			lineSeries.Points.Add(new DataPoint(sec + maxDataCount, cpuPercent));
			plotModel.InvalidatePlot(true);
		}
	}


}
