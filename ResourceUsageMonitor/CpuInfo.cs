using OxyPlot.Series;
using OxyPlot.Wpf;
using OxyPlot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using OxyPlot.Axes;
using System.Windows;
using System.Linq.Expressions;

namespace ResourceUsageMonitor
{
	internal class CpuInfo
	{
		//CPU名
		public readonly string cpuName;

		//コア数
		private readonly int coreCount = Environment.ProcessorCount;

		//一行、一列に表示するコアのグラフの数
		private readonly int columnCount, rowCount;

		//CPU使用率を取得するためのカウンタ
		public readonly PerformanceCounter cpuOverallUsageCounter;
		public readonly PerformanceCounter[] coreUsageCounter;

		//CPU周波数の倍率を取得するためのカウンタ
		public readonly PerformanceCounter cpuOverallPercentCounter;
		public readonly PerformanceCounter[] corePercentCounter;

		//CPUの基本周波数を取得するためのカウンタ
		public readonly PerformanceCounter cpuOverallBaseHzCounter;
		public readonly PerformanceCounter[] coreBaseHzCounter;

		//カウンタ名に使用する文字列
		private const string processorInformation = "Processor Information";
		private const string counterTotal = "_Total", counterCore = "0,";

		// OxyPlotでグラフを表示 全体の使用率
		private readonly PlotModel plotModelOverall = new();
		private readonly LineSeries lineSeriesOverall = new();
		// OxyPlotでグラフを表示 コアごとの使用率
		private readonly PlotModel[] coreGraphPlotModel;
		private readonly PlotView[] corePlotView;
		private readonly LineSeries[] coreLineSeries;
		// コアごとの使用率/周波数を表示するラベル
		private readonly Label[] usageLabel;

		private readonly double[] cpuUsageArray;


		public CpuInfo(PlotView allCpuPlotView, Grid tabGridPerCore)
		{
			columnCount = GetColumn(coreCount);
			rowCount = (int)Math.Ceiling((double)coreCount / columnCount);

			//CPU使用率を取得するためのカウンタの準備
			cpuOverallUsageCounter = getCpuUsageCounter();
			cpuOverallUsageCounter.NextValue();
			coreUsageCounter = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				coreUsageCounter[i] = getCpuUsageCounter(i);
				coreUsageCounter[i].NextValue();
			}

			//CPU周波数を取得するためのカウンタの準備
			cpuOverallPercentCounter = getCpuPercentCounter();
			cpuOverallPercentCounter.NextValue();
			corePercentCounter = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				corePercentCounter[i] = getCpuPercentCounter(i);
				corePercentCounter[i].NextValue();
			}

			//CPUの基本周波数を取得するためのカウンタの準備
			cpuOverallBaseHzCounter = getCpuBaseHzCounter();
			cpuOverallBaseHzCounter.NextValue();
			coreBaseHzCounter = new PerformanceCounter[coreCount];
			for (int i = 0; i < coreCount; i++)
			{
				coreBaseHzCounter[i] = getCpuBaseHzCounter(i);
				coreBaseHzCounter[i].NextValue();
			}

			//CPU名を取得する
			cpuName = GetCpuName();

			//グラフの初期化
			cpuUsageArray = new double[coreCount + 1];
			coreGraphPlotModel = new PlotModel[coreCount];
			corePlotView = new PlotView[coreCount];
			coreLineSeries = new LineSeries[coreCount];
			usageLabel = new Label[coreCount];

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

			allCpuPlotView.Model = plotModelOverall;

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
					Model = coreGraphPlotModel[i],
				};
				tabGridPerCore.Children.Add(corePlotView[i]);


				//コアごとの使用率を表示するラベルを動的に追加
				usageLabel[i] = new Label()
				{
					Name = "LabelCore" + i,
					Content = "Core" + i + " " + (int)cpuUsageArray[i] + "%",
					HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
				};

				tabGridPerCore.Children.Add(usageLabel[i]);
			}
		}

		//一行に表示するコアのグラフの数を返す
		private static int GetColumn(int items)
		{
			if (items <= 64)
			{
				int sq = (int)Math.Sqrt(items);
				return (int)Math.Ceiling((double)items / sq);
			}
			else return (int)Math.Ceiling((double)items / 10);
		}

		//CPU使用率を取得するためのカウンタを取得する。引数を省略すると全体の使用率のカウンタを返す。
		public static PerformanceCounter getCpuUsageCounter(int core = -1)
		{
			const string counterName = "% Processor Time";
			if (core == -1)
				return new(processorInformation, counterName, counterTotal);
			else
				return new(processorInformation, counterName, counterCore + core);
		}

		//CPU周波数(の倍率)を取得するためのカウンタを取得する。引数を省略すると全体の周波数のカウンタを返す。
		public static PerformanceCounter getCpuPercentCounter(int core = -1)
		{
			const string counterName = "% Processor Performance";
			if (core == -1)
				return new(processorInformation, counterName, counterTotal);
			else
				return new(processorInformation, counterName, counterCore + core);
		}

		//CPUの基本周波数を取得するカウンターを返す。引数を省略すると全体の基本周波数のカウンタを返す。
		public static PerformanceCounter getCpuBaseHzCounter(int core = -1)
		{
			const string counterName = "Processor Frequency";
			if (core == -1)
				return new(processorInformation, counterName, counterTotal);
			else
				return new(processorInformation, counterName, counterCore + core);
		}

		//CPU名を取得する
		public static string GetCpuName()
		{
			var searcher = new ManagementObjectSearcher("select Name from Win32_Processor");

			foreach (var obj in searcher.Get())
			{
				return obj["Name"].ToString() ?? "";
			}
			return "";
		}

		public void Update(Label LabelCpu, int maxDataCount, int sec)
		{
			//全体,コアごとの使用率を取得する
			cpuUsageArray[coreCount] = cpuOverallUsageCounter.NextValue();
			for (int i = 0; i < coreCount; i++)
			{
				cpuUsageArray[i] = coreUsageCounter[i].NextValue();
			}
			//全体の周波数を取得する。
			var ghz = cpuOverallPercentCounter.NextValue() * cpuOverallBaseHzCounter.NextValue() / 100 / 1000;
			LabelCpu.Content = "CPU " + cpuName + " " + (int)cpuUsageArray[coreCount] + "% " + ghz.ToString("F2") + "GHz";
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
				var ghz2 = corePercentCounter[i].NextValue() * coreBaseHzCounter[i].NextValue() / 100 / 1000;
				usageLabel[i].Content = "コア" + i + "\n" + (int)cpuUsageArray[i] + "%\n" + ghz2.ToString("F2") + "GHz";
			}
		}

		public void OnWindowSizeChanged(int tabGridWidth, int tabGridHeight)
		{
			//ラムダ式
			Func<double, double, double, double, Thickness> LeftTopWidthHeightToMargin = (left, top, width, height) => new Thickness(left, top, tabGridWidth - left - width, tabGridHeight - top - height);

			//ウィンドウサイズが変更されたらグラフのサイズも変更する
			for (int i = 0; i < coreCount; i++)
			{
				var left = (i % columnCount) * tabGridWidth / columnCount;
				var top = (i / columnCount) * tabGridHeight / rowCount;
				var width = tabGridWidth / columnCount;
				var height = tabGridHeight / rowCount;
				usageLabel[i].Margin = LeftTopWidthHeightToMargin(left, top, width, height);
				usageLabel[i].Width = width;
				usageLabel[i].Height = height;
				corePlotView[i].Margin = LeftTopWidthHeightToMargin(left, top, width, height);
				corePlotView[i].Width = width;
				corePlotView[i].Height = height;
			}
		}
	}
}
