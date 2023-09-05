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
		private DispatcherTimer timer;
		private int sec = 0;
		private readonly int maxDataCount = 60; // 60秒分のデータを表示する
		private readonly int coreCount = Environment.ProcessorCount;//コア数
		private double[] cpuUsageArray;
		private readonly string cpuName;

		// OxyPlotでグラフを表示するときに必要
		private PlotModel plotModel { get; } = new PlotModel();
		private LineSeries lineSeries { get; } = new LineSeries();

		private PlotModel []coreGraphPlotModel;
		private PlotView [] corePlotView;
		private LineSeries[] coreLineSeries;

		private Label[] usageLabel;

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

			SizeChanged += onWindowSizeChanged;

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
				coreLineSeries[i] = new LineSeries();
				coreLineSeries[i].StrokeThickness = 1.5;
				coreLineSeries[i].Color = OxyColor.FromRgb(0, 100, 205);

				coreGraphPlotModel[i].Series.Add(coreLineSeries[i]);

				corePlotView[i] = new PlotView()
				{
					Name = "plotView" + "Core" + i,
					Width = 200,
					Height = 100,
					Model = coreGraphPlotModel[i],
				};
				wrapPanelPerCore.Children.Add(corePlotView[i]);
				
				//コアごとの使用率を表示するラベルを動的に追加
				usageLabel[i] = new Label()
				{
					Name = "LabelCore" + i,
					Width = 200,
					Height = 100,
					Content = "Core" + i + " " + cpuUsageArray[i] + "%",
				};

				//wrapPanelPerCore.Children.Add(usageLabel[i]);
				TabGridPerCore.Children.Add(usageLabel[i]);
			}
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
			plotModel.Axes.Add(new LinearAxis()
			{
				Position = AxisPosition.Bottom,
				Minimum = 0,
				Maximum = 60
			});
			plotModel.Axes.Add(new LinearAxis()
			{
				Position = AxisPosition.Left,
				Minimum = 0,
				Maximum = 100
			});

			plotModel.Background = OxyColors.White;

			//折れ線グラフの設定
			lineSeries.StrokeThickness = 1.5;
			lineSeries.Color = OxyColor.FromRgb(0, 100, 205);

			plotModel.Series.Add(lineSeries);

			AllCpuPlotView.Model = plotModel;
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			int cpuPercent = 0;
			var cpuUsage = GetCpuUsage(ref cpuPercent);
			LabelCpu.Content = "CPU " + cpuName + " " + cpuUsageArray[coreCount] + "%\n";
			DrawRefresh(cpuPercent);
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

			cpuPercent = (int)cpuUsageArray[coreCount];
			if (cpuPercent > 100) cpuPercent = 100;
			return usageString;
		}

		void DrawRefresh(int cpuPercent)
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

			for(int i = 0; i < coreCount; i++)
			{
				//データ数が maxDataCount を超えたらデキューしていく
				if (coreLineSeries[i].Points.Count > maxDataCount)
				{
					coreLineSeries[i].Points.RemoveAt(0);
					coreGraphPlotModel[i].Axes[0].Minimum++;
					coreGraphPlotModel[i].Axes[0].Maximum++;
				}
				coreLineSeries[i].Points.Add(new DataPoint(sec, cpuUsageArray[i]));
				coreGraphPlotModel[i].InvalidatePlot(true);

				//使用率を表示するラベルを更新する
				usageLabel[i].Content = "Core" + i + " " + cpuUsageArray[i] + "%"+"\n"+usageLabel[i].Margin.Left+","+usageLabel[i].Margin.Top;
			}
		}

		private void onWindowSizeChanged(object sender, SizeChangedEventArgs e)
		{

			//ウィンドウサイズが変更されたらグラフのサイズも変更する
			for (int i = 0; i < coreCount; i++)
			{
				usageLabel[i].Margin=new Thickness(100,200,300,400);
			}
		}
	}
}
