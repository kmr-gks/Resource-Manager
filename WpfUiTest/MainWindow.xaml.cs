using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace WpfUiTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			this.DataContext = new MainWindowViewModel();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			((MainWindowViewModel)DataContext).Input = "ボタンをクリックしました";
		}
	}

	public class MainWindowViewModel : INotifyPropertyChanged
	{
		private string m_input = "初期化";

		// INotifyPropertyChanged を実装するためのイベントハンドラ
		public event PropertyChangedEventHandler? PropertyChanged;

		// プロパティ名によって自動的にセットされる
		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// 入力テキスト用のプロパティ
		public string Input
		{
			get { return m_input; }
			set
			{
				if (m_input != value)
				{
					m_input = value;
					// 値をセットした後、画面側でも値が反映されるように通知する
					OnPropertyChanged();
				}
			}
		}

		public IEnumerable<string> TestItems { get; } = new string[] { "aaa", "bbb", "ccc" };
	}
}