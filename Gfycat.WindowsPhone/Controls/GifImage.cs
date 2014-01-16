using System;
using System.Threading;
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
    [TemplateVisualState(Name = "Paused", GroupName = "GifStates")]
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

        #region IsPaused Property

        public static readonly DependencyProperty IsPausedProperty =
            DependencyProperty.Register("IsPaused", typeof(bool), typeof(GifImage), new PropertyMetadata(IsPausedChanged));

        private static void IsPausedChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ((GifImage)sender).UpdatePlayback();
        }

        public bool IsPaused
        {
            get { return (bool)GetValue(IsPausedProperty); }
            set { SetValue(IsPausedProperty, value); }
        }

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
                if (IsPaused)
                {
                    GoToState("Paused");
                }
                else
                {
                    _media.Play();
                    GoToState("Playing");
                }
            };
            _media.MediaFailed += (sender, args) => GoToState("None");

            UpdateSource();
        }

        protected override void OnTap(GestureEventArgs e)
        {
            base.OnTap(e);

            if (_media == null) return;

            if (_media.CurrentState == MediaElementState.Playing)
            {
                IsPaused = true;
            }
            else if (_media.CurrentState == MediaElementState.Paused || _media.CurrentState == MediaElementState.Stopped)
            {
                IsPaused = false;
            }
        }

        private async void UpdateSource()
        {
            if (Source != null)
            {
                // cancel previous request
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }

                _cancellationTokenSource = new CancellationTokenSource();
                try
                {
                    _media.Source = null;

                    ConversionProgress = "waiting";
                    GoToState("Converting");
                    var gifUri = await GifConvert.ConvertAsync(Source, new Progress<string>(p =>
                    {
                        ConversionProgress = p;
                    }), _cancellationTokenSource.Token);

                    GoToState("Loading");
                    _media.Source = gifUri;

                }
                catch (OperationCanceledException)
                {
                }
            }
            else
            {
                GoToState("None");
            }
        }

        private void UpdatePlayback()
        {
            if (IsPaused)
            {
                _media.Pause();
                GoToState("Paused");
            }
            else
            {
                _media.Play();
                GoToState("Playing");
            }
        }

        private void GoToState(string state)
        {
            VisualStateManager.GoToState(this, state, true);
        }
    }
}