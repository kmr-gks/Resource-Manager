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
		private readonly int coreCount = Environment.ProcessorCount;//コア数
		private readonly double[] cpuUsageArray;
		private readonly string cpuName;

		// OxyPlotでグラフを表示するときに必要
		private readonly PlotModel plotModelOverall = new();
		private readonly LineSeries lineSeriesOverall = new();

		private readonly PlotModel[] coreGraphPlotModel;
		private readonly PlotView[] corePlotView;
		private readonly LineSeries[] coreLineSeries;

		private readonly Label[] usageLabel;

		public MainWindow()
		{
			InitializeComponent();
			// タイマーの設定
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(100)
			};
			timer.Tick += ResourceUsageMonitor_Tick;
			timer.Start();

			SizeChanged += OnWindowSizeChanged;

			//グラフの初期化
			cpuUsageArray = new double[coreCount + 1];
			coreGraphPlotModel = new PlotModel[coreCount];
			corePlotView = new PlotView[coreCount];
			coreLineSeries = new LineSeries[coreCount];
			usageLabel = new Label[coreCount];
			cpuName = GetCpuName();
			GraphSetup();
			using var tokenSource = new CancellationTokenSource();

			for (int i = 0; i < coreCount; i++)
			{
				//コアごとのグラフを動的に追加
				coreGraphPlotModel[i] = new PlotModel();
				// X軸とY軸の設定
				coreGraphPlotModel[i].Axes.Add(new LinearAxis()
				{
					Position = AxisPosition.Bottom,
					Minimum = 0,
					Maximum = 60,
					IsAxisVisible = false,
				});
				coreGraphPlotModel[i].Axes.Add(new LinearAxis()
				{
					Position = AxisPosition.Left,
					Minimum = 0,
					Maximum = 100,
					IsAxisVisible = false,
				});

				coreGraphPlotModel[i].Background = OxyColors.White;

				//折れ線グラフの設定
				coreLineSeries[i] = new LineSeries
				{
					StrokeThickness = 1.5,
					Color = OxyColor.FromRgb(0, 100, 205)
				};

				coreGraphPlotModel[i].Series.Add(coreLineSeries[i]);

				corePlotView[i] = new PlotView()
				{
					Name = "plotView" + "Core" + i,
					Width = 200,
					Height = 100,
					Model = coreGraphPlotModel[i],
				};
				TabGridPerCore.Children.Add(corePlotView[i]);


				//コアごとの使用率を表示するラベルを動的に追加
				usageLabel[i] = new Label()
				{
					Name = "LabelCore" + i,
					Width = 200,
					Height = 100,
					Content = "Core" + i + " " + cpuUsageArray[i] + "%",
					BorderBrush = System.Windows.Media.Brushes.Black,
					BorderThickness = new Thickness(1),
				};

				TabGridPerCore.Children.Add(usageLabel[i]);
			}
		}

		private static string GetCpuName()
		{
			var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");

			foreach (var obj in searcher.Get())
			{
				return obj["Name"].ToString() ?? "";
			}
			return "";
		}

		//グラフの見た目をつくる
		private void GraphSetup()
		{
			// X軸とY軸の設定
			plotModelOverall.Axes.Add(new LinearAxis()
			{
				Position = AxisPosition.Bottom,
				Minimum = 0,
				Maximum = 60
			});
			plotModelOverall.Axes.Add(new LinearAxis()
			{
				Position = AxisPosition.Left,
				Minimum = 0,
				Maximum = 100
			});

			plotModelOverall.Background = OxyColors.White;

			//折れ線グラフの設定
			lineSeriesOverall.StrokeThickness = 1.5;
			lineSeriesOverall.Color = OxyColor.FromRgb(0, 100, 205);

			plotModelOverall.Series.Add(lineSeriesOverall);

			AllCpuPlotView.Model = plotModelOverall;
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			int cpuPercent = 0;
			GetCpuUsage(ref cpuPercent);
			LabelCpu.Content = "CPU " + cpuName + " " + cpuUsageArray[coreCount] + "%\n";
			DrawRefresh(cpuPercent);
		}

		private string GetCpuUsage_old1()
		{
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
		private static string GetCpuUsage_old2()
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
		private static string GetCpuUsage_old3()
		{
			string displayText = "";
			//すべてのリソース値を取得するにはこのように書く。
			//searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor where name=\"_Total\"");
			var searcher = new ManagementObjectSearcher("select * from Win32_Processor");

			using (var queryCollection = searcher.Get())
			{
				foreach (var m in queryCollection)
				{
					foreach (var prop in m.Properties)
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
					var coreNumber = int.Parse(name.ToString() ?? "");
					if (coreNumber < coreCount)
					{
						cpuUsageArray[coreNumber] = (ulong)usage;
					}
				}
			}

			cpuPercent = (int)cpuUsageArray[coreCount];
			if (cpuPercent > 100) cpuPercent = 100;
			return usageString;
		}

		void DrawRefresh(int cpuPercent)
		{
			sec++;
			//データ数が maxDataCount を超えたらデキューしていく
			if (lineSeriesOverall.Points.Count > maxDataCount)
			{
				lineSeriesOverall.Points.RemoveAt(0);
				plotModelOverall.Axes[0].Minimum++;
				plotModelOverall.Axes[0].Maximum++;
			}
			lineSeriesOverall.Points.Add(new DataPoint(sec, cpuPercent));
			plotModelOverall.InvalidatePlot(true);

			for (int i = 0; i < coreCount; i++)
			{
				if (coreLineSeries[i].Points.Count > maxDataCount)
				{
					coreLineSeries[i].Points.RemoveAt(0);
					coreGraphPlotModel[i].Axes[0].Minimum++;
					coreGraphPlotModel[i].Axes[0].Maximum++;
				}
				coreLineSeries[i].Points.Add(new DataPoint(sec, cpuUsageArray[i]));
				coreGraphPlotModel[i].InvalidatePlot(true);

				//使用率を表示するラベルを更新する
				usageLabel[i].Content = "Core" + i + " " + cpuUsageArray[i] + "%";
			}
		}
		private Thickness RightTopWidthHeight(double left, double top, double width = 100, double height = 100)
		{
			return new Thickness(left, top, TabGridPerCore.ActualWidth - left - width, TabGridPerCore.ActualHeight - top - height);
		}

		private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
		{

			//ウィンドウサイズが変更されたらグラフのサイズも変更する
			for (int i = 0; i < coreCount; i++)
			{
				usageLabel[i].Margin = RightTopWidthHeight(i * TabGridPerCore.ActualWidth / coreCount, 0, TabGridPerCore.ActualWidth / coreCount, 100);
				usageLabel[i].Width = TabGridPerCore.ActualWidth / coreCount;
				corePlotView[i].Margin = RightTopWidthHeight(i * TabGridPerCore.ActualWidth / coreCount, 0, TabGridPerCore.ActualWidth / coreCount, 100);
				corePlotView[i].Width = TabGridPerCore.ActualWidth / coreCount;
			}
		}
	}
}
