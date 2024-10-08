﻿using System;
using System.Collections.ObjectModel;
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
		//優先度を下げるプロセスのリスト
		public readonly ObservableCollection<LowProcessListItem> Items;

		private readonly DispatcherTimer timer;
		private int sec = 0;
		private readonly int maxDataCount = 60; // 60秒分のデータを表示する
		private readonly CpuInfo cpuInfo;
		private readonly ProcessInfo processInfo;

		public MainWindow()
		{
			InitializeComponent();
			Items = new ObservableCollection<LowProcessListItem>
			{
				new("chrome.exe") ,
				new("explorer.exe"),
			};
			LowerPriorityProcessListView.ItemsSource = Items;

			//CPUタブ
			cpuInfo = new();
			// タイマーの設定
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1),
			};
			timer.Tick += (sender, e) =>
			{
				//タイマーで一秒経過したときの処理
				sec++;
				cpuInfo.Update(maxDataCount, sec);
			};
			timer.Start();

			SizeChanged += cpuInfo.OnWindowSizeChanged;

			//最初のウィンドウの大きさに応じてグラフの大きさを変更する。(OnWindowSizeChangedと同じ内容)
			Loaded += new RoutedEventHandler((sender, e) => { cpuInfo.OnWindowSizeChanged(sender, null); });

			//プロセスタブ
			processInfo = new();
			LowPriorityButton.Click += processInfo.SetPriorityLow;
			TerminateButton.Click += processInfo.Terminate;
			AddProcessButton.Click += processInfo.OnAddProcess;
			DeleteProcessButton.Click += processInfo.OnDeleteProcess;
			TextBoxOneProcess.TextChanged += processInfo.SearchProcess;

			using var tokenSource = new CancellationTokenSource();
		}
	}

	public class LowProcessListItem
	{
		public string Name { get; }
		public LowProcessListItem(string name)
		{
			Name = name;
		}
	}
}
