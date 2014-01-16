using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Gfycat.Samples.WindowsPhone
{
    public partial class MainPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

        }

        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            GifImage.PreviewSource = new BitmapImage(new Uri(PreviewTextBox.Text, UriKind.Absolute));
            GifImage.Source = new Uri(UrlTextBox.Text, UriKind.Absolute);
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            GifImage.PreviewSource = null;
            GifImage.Source = null;
        }
    }
}