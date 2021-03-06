using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using crypto_stats.Models.Data;
using crypto_stats.Services;
using crypto_stats.Utils;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace crypto_stats.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AggregatePage : ContentPage
    {
        private readonly CryptoStatsService _service = new CryptoStatsService();
        private List<Crypto> _cryptoStats = new List<Crypto>();
        private static bool _shouldContinue;

        public AggregatePage()
        {
            InitializeComponent();
        }

        #region LifeCycle Overrides

        protected override async void OnAppearing()
        {
            // load data
            await LoadData();

            // change UI state
            prgLoading.IsVisible = false;
            lstCryptoStats.IsVisible = true;

            // start background refresh
            StartBackgroundRefresh();
        }

        protected override void OnDisappearing()
        {
            // stop the background refresh
            StopBackgroundRefresh();
        }

        #endregion

        #region Event Handlers

        private async void OnRefresh(object sender, EventArgs e)
        {
            await LoadData();

            var search = txtSearch.Text?.ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(search))
            {
                SearchData(search);
            }

            lstCryptoStats.IsRefreshing = false;
        }

        private void SearchStats(object sender, TextChangedEventArgs e)
        {
            var search = e.NewTextValue.ToLowerInvariant();
            SearchData(search);
        }

        private async void ViewCoinDetails(object sender, ItemTappedEventArgs e)
        {
            var coin = e.Item as Crypto;
            await Navigation.PushAsync(new DetailsPage(coin));
        }

        private async void ViewSettingsPage(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new SettingsPage());
        }

        #endregion

        #region Helper Methods

        private async Task LoadData()
        {
            var coins = await _service.GetAllStatsAsync();
            _cryptoStats = coins.Data;
            BindDataToUi(coins.Data);
        }

        private void SearchData(string query)
        {
            var filteredCryptoStats = _cryptoStats
                .Where(x => x.Id.ToLowerInvariant().Contains(query)
                            || x.Symbol.ToLowerInvariant().Contains(query)
                            || x.Name.ToLowerInvariant().Contains(query))
                .ToList();
            BindDataToUi(filteredCryptoStats);
        }

        private void BindDataToUi(IEnumerable<Crypto> data)
        {
            lstCryptoStats.ItemsSource = data;
        }

        private void StartBackgroundRefresh()
        {
            _shouldContinue = true;
            var refreshInterval = SyncManager.GetSyncFrequencyInMinutes();
            Device.StartTimer(new TimeSpan(0, refreshInterval, 0), () =>
            {
                // if the process is to be cancelled then honour that
                if (!_shouldContinue)
                {
                    return _shouldContinue;
                }
                
                Task.Run(async () =>
                {
                    // pull latest data
                    var service = new CryptoStatsService();
                    var coins = await service.GetAllStatsAsync();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        BindDataToUi(coins.Data);
                        Toasts.DisplayInfo("Crypto data refreshed.");
                    });
                });

                // return true to keep the timer running.
                return _shouldContinue;
            });
        }

        internal static void StopBackgroundRefresh()
        {
            _shouldContinue = false;
        }

        #endregion
    }
}
