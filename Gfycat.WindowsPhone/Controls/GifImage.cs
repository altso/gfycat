using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Gfycat.Controls
{
    [TemplatePart(Name = "GifMediaElement", Type = typeof(MediaElement))]
    [TemplateVisualState(Name = "None", GroupName = "GifStates")]
    [TemplateVisualState(Name = "Converting", GroupName = "GifStates")]
    [TemplateVisualState(Name = "Loading", GroupName = "GifStates")]
    [TemplateVisualState(Name = "Playing", GroupName = "GifStates")]
    [TemplateVisualState(Name = "Stopped", GroupName = "GifStates")]
    [TemplateVisualState(Name = "Error", GroupName = "GifStates")]
    public class GifImage : Control
    {
        #region Source Property

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(GifImage), new PropertyMetadata(SourceChanged));

        private static void SourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((GifImage)sender).UpdateSource();
        }

        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        #endregion

        #region ConversionProgress Property

        public static readonly DependencyProperty ConversionProgressProperty =
            DependencyProperty.Register("ConversionProgress", typeof(string), typeof(GifImage), new PropertyMetadata(null));

        public string ConversionProgress
        {
            get { return (string)GetValue(ConversionProgressProperty); }
            set { SetValue(ConversionProgressProperty, value); }
        }

        #endregion

        #region IsAnimating Property

        public static readonly DependencyProperty IsAnimatingProperty =
            DependencyProperty.Register("IsAnimating", typeof(bool), typeof(GifImage), new PropertyMetadata(IsAnimatingChanged));

        private static void IsAnimatingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((GifImage)sender).UpdatePlayback();
        }

        public bool IsAnimating
        {
            get { return (bool)GetValue(IsAnimatingProperty); }
            set { SetValue(IsAnimatingProperty, value); }
        }

        #endregion

        #region PreviewSource Property

        public ImageSource PreviewSource
        {
            get { return (ImageSource)GetValue(PreviewSourceProperty); }
            set { SetValue(PreviewSourceProperty, value); }
        }

        public static readonly DependencyProperty PreviewSourceProperty =
            DependencyProperty.Register("PreviewSource", typeof(ImageSource), typeof(GifImage), new PropertyMetadata(null));

        #endregion

        private MediaElement _media;
        private CancellationTokenSource _cancellationTokenSource;

        public GifImage()
        {
            DefaultStyleKey = typeof(GifImage);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _media = (MediaElement)GetTemplateChild("GifMediaElement");
            _media.MediaEnded += (sender, args) =>
            {
                _media.Position = TimeSpan.Zero;
                _media.Play();
            };
            _media.MediaOpened += (sender, args) =>
            {
                if (IsAnimating)
                {
                    _media.Play();
                    GoToState("Playing");
                }
                else
                {
                    GoToState("Stopped");
                }
            };
            _media.MediaFailed += (sender, args) => GoToState("None");

            UpdateSource();
        }

        protected override void OnTap(GestureEventArgs e)
        {
            base.OnTap(e);

            if (_media == null) return;

            IsAnimating = _media.CurrentState != MediaElementState.Playing;
        }

        private async void UpdateSource()
        {
            if (_media == null) return;

            // cancel previous request
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            if (Source != null && IsAnimating)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    _media.Source = null;

                    ConversionProgress = "waiting";
                    GoToState("Converting");
                    var gifUri = await ConvertAsync(Source, new Progress<string>(p =>
                    {
                        ConversionProgress = p;
                    }), _cancellationTokenSource.Token);

                    GoToState("Loading");
                    _media.Source = gifUri;

                }
                catch (OperationCanceledException)
                {
                    GoToState("None");
                }
                catch
                {
                    GoToState("Error");
                }
            }
            else
            {
                _media.Source = null;
                GoToState("None");
            }
        }

        private void UpdatePlayback()
        {
            if (_media == null) return;

            if (_media.Source != null)
            {
                if (IsAnimating)
                {
                    _media.Play();
                    GoToState("Playing");
                }
                else
                {
                    _media.Stop();
                    GoToState("Stopped");
                }
            }
            else
            {
                UpdateSource();
            }
        }

        private void GoToState(string state)
        {
            VisualStateManager.GoToState(this, state, true);
        }

        private static async Task<Uri> ConvertAsync(Uri gifUri, IProgress<string> progress, CancellationToken cancellationToken)
        {
            while (true)
            {
                GfycatRetryException retryException;
                try
                {
                    return await GifConvert.ConvertAsync(gifUri, progress, cancellationToken);
                }
                catch (GfycatRetryException e)
                {
                    retryException = e;
                }
                await TaskEx.Delay(retryException.RetryInterval, cancellationToken);
            }
        }
    }
}