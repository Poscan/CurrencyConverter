using CurrencyConverter.ViewModel;
using Xamarin.Forms;

namespace CurrencyConverter.View
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new MainPageViewModel();
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            ((MainPageViewModel)BindingContext).UpdateDailyCurrencies(e.NewDate);
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(e.NewTextValue)) return;

            if (!double.TryParse(e.NewTextValue, out double value))
            {
                ((Entry)sender).Text = e.OldTextValue;
            }
        }
    }
}
