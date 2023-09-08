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

		//CPU使用率を取得するためのカウンタ
		private readonly PerformanceCounter cpuOverallUsageCounter;
		private readonly PerformanceCounter[] coreUsageCounter;

		//CPU周波数の倍率を取得するためのカウンタ
		private readonly PerformanceCounter cpuOverallPercentCounter;
		private readonly PerformanceCounter[] corePercentCounter;

		//CPUの基本周波数を取得するためのカウンタ
		private readonly PerformanceCounter cpuOverallBaseHzCounter;
		private readonly PerformanceCounter[] coreBaseHzCounter;

		//CPU名
		private readonly string cpuName;

		// OxyPlotでグラフを表示 全体の使用率
		private readonly PlotModel plotModelOverall = new();
		private readonly LineSeries lineSeriesOverall = new();
		// OxyPlotでグラフを表示 コアごとの使用率
		private readonly PlotModel[] coreGraphPlotModel;
		private readonly PlotView[] corePlotView;
		private readonly LineSeries[] coreLineSeries;
		// コアごとの使用率/周波数を表示するラベル
		private readonly Label[] usageLabel;

		public MainWindow()
		{
			InitializeComponent();
			// タイマーの設定
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromMilliseconds(500)
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

			//CPU使用率を取得するためのカウンタの準備
			cpuOverallUsageCounter = new("Processor Information", "% Processor Time", "_Total");
			cpuOverallUsageCounter.NextValue();
			coreUsageCounter = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				coreUsageCounter[i] = new("Processor Information", "% Processor Time", "0," + i.ToString());
				coreUsageCounter[i].NextValue();
			}

			//CPU周波数を取得するためのカウンタの準備
			cpuOverallPercentCounter = new("Processor Information", "% Processor Performance", "_Total");
			cpuOverallPercentCounter.NextValue();
			corePercentCounter = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				corePercentCounter[i] = new("Processor Information", "% Processor Performance", "0," + i.ToString());
				corePercentCounter[i].NextValue();
			}
			cpuOverallBaseHzCounter = new("Processor Information", "Processor Frequency", "_Total");
			cpuOverallBaseHzCounter.NextValue();
			coreBaseHzCounter = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				coreBaseHzCounter[i] = new("Processor Information", "Processor Frequency", "0," + i.ToString());
				coreBaseHzCounter[i].NextValue();
			}

			//CPU名を取得する
			cpuName = GetCpuName();
			GraphSetup();
			using var tokenSource = new CancellationTokenSource();


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
			//全体的な使用率のグラフ
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
					HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
				};

				TabGridPerCore.Children.Add(usageLabel[i]);
			}

			//最初のウィンドウの大きさに応じてグラフの大きさを変更する。(OnWindowSizeChangedと同じ内容)
			Loaded += new RoutedEventHandler((sender, e) =>
			{
				for (int i = 0; i < coreCount; i++)
				{
					usageLabel[i].Margin = RightTopWidthHeight(i * TabGridPerCore.ActualWidth / coreCount, 0, TabGridPerCore.ActualWidth / coreCount, 100);
					usageLabel[i].Width = TabGridPerCore.ActualWidth / coreCount;
					corePlotView[i].Margin = RightTopWidthHeight(i * TabGridPerCore.ActualWidth / coreCount, 0, TabGridPerCore.ActualWidth / coreCount, 100);
					corePlotView[i].Width = TabGridPerCore.ActualWidth / coreCount;
				}
			});
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			//全体,コアごとの使用率を取得する
			cpuUsageArray[coreCount] = cpuOverallUsageCounter.NextValue();
			for (int i = 0; i < coreCount; i++)
			{
				cpuUsageArray[i] = coreUsageCounter[i].NextValue();
			}
			//全体の周波数を取得する。
			var ghz = cpuOverallPercentCounter.NextValue() * cpuOverallBaseHzCounter.NextValue() / 100 / 1000;
			LabelCpu.Content = "CPU " + cpuName + " " + cpuUsageArray[coreCount].ToString("F2") + "% " + ghz.ToString("F2") + "GHz\n";
			DrawRefresh();
		}

		void DrawRefresh()
		{
			sec++;
			//データ数が maxDataCount を超えたらデキューしていく
			if (lineSeriesOverall.Points.Count > maxDataCount)
			{
				lineSeriesOverall.Points.RemoveAt(0);
				plotModelOverall.Axes[0].Minimum++;
				plotModelOverall.Axes[0].Maximum++;
			}
			lineSeriesOverall.Points.Add(new DataPoint(sec, cpuUsageArray[coreCount]));
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
				var ghz = corePercentCounter[i].NextValue() * coreBaseHzCounter[i].NextValue() / 100 / 1000;
				usageLabel[i].Content = "コア" + i + "\n" + cpuUsageArray[i].ToString("F2") + "%\n" + ghz.ToString("F2") + "GHz";
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
