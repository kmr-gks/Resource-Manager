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

			// コンストラクタ内でItemsSourceを積み込むとする。
			PrefecturesComboBox.ItemsSource = new List<Candidates>()
			{
				new(){ Name = "北海道", SecondName = "札幌市"},
				new(){ Name = "青森県", SecondName = "青森市"},
				new(){ Name = "鹿児島県", SecondName = "鹿児島市"},
				new(){ Name = "沖縄県", SecondName = "那覇市"},
			};
		}
	}
}