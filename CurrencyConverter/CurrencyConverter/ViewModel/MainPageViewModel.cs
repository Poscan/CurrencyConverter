using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CurrencyConverter.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xamarin.Forms.Internals;

namespace CurrencyConverter.ViewModel
{
    internal class MainPageViewModel : INotifyPropertyChanged
    {
        private string _selectedDate;
        private ObservableCollection<Currency> _currencies;
        private Currency _selectedCurrencyLeft;
        private Currency _selectedCurrencyRight;
        private string _inputValue;
        private string _resultValue;
        private HttpClient Client { get; }

        public MainPageViewModel()
        {
            Client = new HttpClient();
            Currencies = new ObservableCollection<Currency>();
            InputValue = "0";
            ResultValue = "0";

            LoadCurrentCurrencies();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string SelectedDate
        {
            get => _selectedDate;
            set
            {
                _selectedDate = value;
                OnPropertyChanged();
            }
        }

        public string InputValue
        {
            get => _inputValue;
            set
            {
                if (_inputValue == value) return;

                _inputValue = value;

                CalculateResultValue(value);
                OnPropertyChanged();
            }
        }

        public string ResultValue
        {
            get => _resultValue;
            set
            {
                if (_resultValue == value) return;

                _resultValue = value;

                CalculateInputValue(value);
                OnPropertyChanged();
            }
        }

        private void CalculateInputValue(string value)
        {
            if (SelectedCurrencyLeft == null || SelectedCurrencyRight == null || string.IsNullOrWhiteSpace(value))
            {
                _inputValue = "0";
            }
            else
            {
                var res = Convert.ToDouble(value) * SelectedCurrencyLeft.Value / SelectedCurrencyLeft.Nominal /
                          (SelectedCurrencyRight.Value / SelectedCurrencyRight.Nominal);

                _inputValue = res.ToString(CultureInfo.InvariantCulture);
            }
            
            OnPropertyChanged(nameof(InputValue));
        }

        private void CalculateResultValue(string value)
        {
            if (SelectedCurrencyLeft == null || SelectedCurrencyRight == null || string.IsNullOrWhiteSpace(value))
            {
                _resultValue = "0";
            }
            else
            {
                var res = Convert.ToDouble(value) * SelectedCurrencyRight.Value / SelectedCurrencyRight.Nominal /
                          (SelectedCurrencyLeft.Value / SelectedCurrencyLeft.Nominal);

                _resultValue = res.ToString(CultureInfo.InvariantCulture);
            }

            OnPropertyChanged(nameof(ResultValue));
        }

        public Currency SelectedCurrencyLeft
        {
            get => _selectedCurrencyLeft;
            set
            {
                _selectedCurrencyLeft = value;

                CalculateResultValue(InputValue);
                OnPropertyChanged();
            }
        }

        public Currency SelectedCurrencyRight
        {
            get => _selectedCurrencyRight;
            set
            {
                _selectedCurrencyRight = value;

                CalculateResultValue(InputValue);
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Currency> Currencies
        {
            get => _currencies;
            set
            {
                _currencies = value;
                OnPropertyChanged();
            }
        }

        public async Task UpdateDailyCurrencies(DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                date = date.DayOfWeek == DayOfWeek.Sunday ? date.AddDays(-2) : date.AddDays(-1);

            SetNewDate(date);

            var dateString = date.ToString("yyyy/MM/dd");
            var s = $"https://www.cbr-xml-daily.ru/archive/{dateString}/daily_json.js";

            var response = await Client.GetAsync(s);
            var currenciesJson = await response.Content.ReadAsStringAsync();

            var jObject = JObject.Parse(currenciesJson);

            var currencies =
                (JsonConvert.DeserializeObject<Dictionary<string, Currency>>(jObject["Valute"]?.ToString() ?? "") ??
                 new Dictionary<string, Currency>()).Select(x => x.Value).ToList();

            if (currencies.Count + 1 == Currencies.Count)
                Currencies.ForEach(x => x.Value = currencies.First(y => y.Name == x.Name).Value);
            else
                Currencies = new ObservableCollection<Currency>(currencies) {new Currency("Рубли", 1, 1)};

            OnPropertyChanged();
        }

        private void SetNewDate(DateTime newDate)
        {
            SelectedDate = "Получилось получить данные на данную дату " + newDate.ToString("dd, MM, yyyy");
        }

        private async Task LoadCurrentCurrencies()
        {
            var dateResult = DateTime.Now;
            var url = "https://www.cbr-xml-daily.ru/daily_json.js";

            if (dateResult.DayOfWeek == DayOfWeek.Saturday || dateResult.DayOfWeek == DayOfWeek.Sunday)
            {
                dateResult = dateResult.DayOfWeek == DayOfWeek.Sunday ? dateResult.AddDays(-2) : dateResult.AddDays(-1);
                var dateString = dateResult.ToString("yyyy/MM/dd");
                url =
                    $"https://www.cbr-xml-daily.ru/archive/{dateString}/daily_json.js";
            }

            SetNewDate(dateResult);
            var response = await Client.GetAsync(url);

            var currenciesJson = await response.Content.ReadAsStringAsync();
            var jObject = JObject.Parse(currenciesJson);

            var currencies =
                JsonConvert.DeserializeObject<Dictionary<string, Currency>>(jObject["Valute"]?.ToString() ?? "");

            Currencies = new ObservableCollection<Currency>(currencies?.Select(x => x.Value) ?? new List<Currency>());
            Currencies.Add(new Currency("Рубли", 1, 1));

            SelectedCurrencyLeft = Currencies.Last();
            SelectedCurrencyRight = Currencies.Last();

            OnPropertyChanged();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}