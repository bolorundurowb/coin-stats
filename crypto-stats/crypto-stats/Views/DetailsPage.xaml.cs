﻿using System.Globalization;
using System.Linq;
using crypto_stats.Models.Data;
using crypto_stats.Models.Extensions;
using crypto_stats.Services;
using crypto_stats.Utils;
using crypto_stats.Utils.Extensions;
using Microcharts;
using SkiaSharp;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace crypto_stats.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DetailsPage : ContentPage
    {
        private readonly CryptoStatsService _service = new CryptoStatsService();
        private readonly Crypto _crypto;

        public DetailsPage(Crypto crypto)
        {
            InitializeComponent();
            _crypto = crypto;
        }

        protected override async void OnAppearing()
        {
            BindingContext = _crypto;
            
            // pull and display trends
            var extendedHistory = await _service.GetExtendedHistoryAsync(_crypto.Id);
            var orderedHistory = extendedHistory.GetOrdered().ToList();
            var entries = orderedHistory
                .Select((x, index) =>
                {
                    float.TryParse(x.PriceUsd, out var currentPrice);
                    var colour = SKColor.Parse(Constants.PositiveColour);
            
                    if (index > 0)
                    {
                        float.TryParse(orderedHistory[index - 1].PriceUsd, out var previousPrice);
            
                        if (currentPrice < previousPrice)
                        {
                            colour = SKColor.Parse(Constants.NegativeColour);
                        }
                    }
            
                    return new ChartEntry(currentPrice)
                    {
                        Color = colour,
                        Label = x.Time.FromUnixTimeStamp().ToString("dd MMM, h:mm"),
                        ValueLabelColor = colour,
                        ValueLabel = currentPrice.ToString(CultureInfo.InvariantCulture)
                    };
                })
                .ToArray();

            var chart = new LineChart
            {
                Entries = entries,
                BackgroundColor =
                    SKColor.Parse(ThemeManager.CurrentTheme() == ThemeManager.Themes.Dark
                        ? Constants.DarkBackgroundColour
                        : Constants.LightBackgroundColour),
                LineSize = 5,
                PointSize = 7.5f,
                
            };
            chrtTrend.Chart = chart;
        }
    }
}
