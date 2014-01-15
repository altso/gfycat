using System;
using System.Windows;

namespace Gfycat.Samples.WindowsPhone
{
    public partial class MainPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

        }

        private async void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            var mp4Uri = await GifConvert.ConvertAsync(new Uri(UrlTextBox.Text, UriKind.Absolute));
            GifImage.Source = mp4Uri;
        }
    }
}