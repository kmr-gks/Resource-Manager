using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
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
		public MainWindow()
		{
			InitializeComponent();
			timer = new DispatcherTimer
			{
				Interval = TimeSpan.FromSeconds(1)
			};
			timer.Tick += ResourceUsageMonitor_Tick;
			timer.Start();
			LabelTest.Content = "Hello World";
		}

		private void ResourceUsageMonitor_Tick(object? sender, EventArgs e)
		{
			var cpuUsage = GetCpuUsage();
			LabelTest.Content = cpuUsage + "\n";
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
		private string GetCpuUsage()
		{
			var coreCount = Environment.ProcessorCount;
			var usageArray = new double[coreCount+1];
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
					var coreNumber = int.Parse(name.ToString());
					usageArray[coreNumber] = 100 - (ulong)usage;
				}
			}
			for (int i = 0; i < coreCount; i++)
			{
				usageString += usageArray[i] + "%, ";
			}
			usageString += "Total "+usageArray[coreCount] +"\n";

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
			return usageString;
		}
	}


}
