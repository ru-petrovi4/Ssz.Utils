using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using Ssz.Utils.Wpf.WpfAnimatedGif.Decoding;

namespace Ssz.Utils.Wpf.WpfAnimatedGif
{
    /// <summary>
    ///     Provides attached properties that display animated GIFs in a standard Image control.
    /// </summary>
    public static class ImageBehavior
    {
        #region public functions

        /// <summary>
        ///     Gets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element from which to read the property value.</param>
        /// <returns>The currently displayed animated image.</returns>
        [AttachedPropertyBrowsableForType(typeof (Image))]
        public static ImageSource GetAnimatedSource(Image image)
        {
            return (ImageSource) image.GetValue(AnimatedSourceProperty);
        }

        /// <summary>
        ///     Sets the value of the <c>AnimatedSource</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element on which to set the property value.</param>
        /// <param name="value">The animated image to display.</param>
        public static void SetAnimatedSource(Image image, ImageSource value)
        {
            image.SetValue(AnimatedSourceProperty, value);
        }

        /// <summary>
        ///     Gets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element from which to read the property value.</param>
        /// <returns>The repeat behavior of the animated image.</returns>
        [AttachedPropertyBrowsableForType(typeof (Image))]
        public static RepeatBehavior GetRepeatBehavior(Image image)
        {
            return (RepeatBehavior) image.GetValue(RepeatBehaviorProperty);
        }

        /// <summary>
        ///     Sets the value of the <c>RepeatBehavior</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element on which to set the property value.</param>
        /// <param name="value">The repeat behavior of the animated image.</param>
        public static void SetRepeatBehavior(Image image, RepeatBehavior value)
        {
            image.SetValue(RepeatBehaviorProperty, value);
        }

        /// <summary>
        ///     Gets the value of the <c>AnimateInDesignMode</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>true if GIF animations are shown in design mode; false otherwise.</returns>
        public static bool GetAnimateInDesignMode(DependencyObject obj)
        {
            return (bool) obj.GetValue(AnimateInDesignModeProperty);
        }

        /// <summary>
        ///     Sets the value of the <c>AnimateInDesignMode</c> attached property for the specified object.
        /// </summary>
        /// <param name="obj">The element on which to set the property value.</param>
        /// <param name="value">true to show GIF animations in design mode; false otherwise.</param>
        public static void SetAnimateInDesignMode(DependencyObject obj, bool value)
        {
            obj.SetValue(AnimateInDesignModeProperty, value);
        }

        /// <summary>
        ///     Gets the value of the <c>AutoStart</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element from which to read the property value.</param>
        /// <returns>true if the animation should start immediately when loaded. Otherwise, false.</returns>
        [AttachedPropertyBrowsableForType(typeof (Image))]
        public static bool GetAutoStart(Image image)
        {
            return (bool) image.GetValue(AutoStartProperty);
        }

        /// <summary>
        ///     Sets the value of the <c>AutoStart</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element on which to set the property value.</param>
        /// <param name="value">true if the animation should start immediately when loaded. Otherwise, false.</param>
        /// <remarks>The default value is true.</remarks>
        public static void SetAutoStart(Image image, bool value)
        {
            image.SetValue(AutoStartProperty, value);
        }

        /// <summary>
        ///     Gets the animation controller for the specified <c>Image</c> control.
        /// </summary>
        /// <param name="imageControl"></param>
        /// <returns></returns>
        public static ImageAnimationController GetAnimationController(Image imageControl)
        {
            return (ImageAnimationController) imageControl.GetValue(AnimationControllerPropertyKey.DependencyProperty);
        }

        /// <summary>
        ///     Gets the value of the <c>SynchronizedBySource</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element from which to read the property value.</param>
        /// <returns>
        ///     true if the animation should be synchronized across all images that have the
        ///     same <c>AnimatedSource</c> and <c>RepeatBehavior</c>. Otherwise, false.
        /// </returns>
        [AttachedPropertyBrowsableForType(typeof (Image))]
        public static bool GetSynchronizedBySource(Image image)
        {
            return (bool) image.GetValue(SynchronizedBySourceProperty);
        }

        /// <summary>
        ///     Sets the value of the <c>SynchronizedBySource</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element on which to set the property value.</param>
        /// <param name="value">
        ///     true if the animation should be synchronized across all images that have the
        ///     same <c>AnimatedSource</c> and <c>RepeatBehavior</c>. Otherwise, false.
        /// </param>
        /// <remarks>The default value is true.</remarks>
        public static void SetSynchronizedBySource(Image image, bool value)
        {
            image.SetValue(SynchronizedBySourceProperty, value);
        }

        /*
        /// <summary>
        ///     Gets the value of the <c>IsAnimationLoaded</c> attached property for the specified object.
        /// </summary>
        /// <param name="image">The element from which to read the property value.</param>
        /// <returns>true if the animation is loaded. Otherwise, false.</returns>
        public static bool GetIsAnimationLoaded(Image image)
        {
            return (bool) image.GetValue(IsAnimationLoadedProperty);
        }
        */

        /// <summary>
        ///     Adds a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="image">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be added.</param>
        public static void AddAnimationCompletedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (handler == null)
                throw new ArgumentNullException("handler");
            image.AddHandler(AnimationCompletedEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the AnimationCompleted attached event.
        /// </summary>
        /// <param name="image">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be removed.</param>
        public static void RemoveAnimationCompletedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (handler == null)
                throw new ArgumentNullException("handler");
            image.RemoveHandler(AnimationCompletedEvent, handler);
        }

        /// <summary>
        ///     Adds a handler for the AnimationLoaded attached event.
        /// </summary>
        /// <param name="image">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be added.</param>
        public static void AddAnimationLoadedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (handler == null)
                throw new ArgumentNullException("handler");
            image.AddHandler(AnimationLoadedEvent, handler);
        }

        /// <summary>
        ///     Removes a handler for the AnimationLoaded attached event.
        /// </summary>
        /// <param name="image">The UIElement that listens to this event.</param>
        /// <param name="handler">The event handler to be removed.</param>
        public static void RemoveAnimationLoadedHandler(Image image, RoutedEventHandler handler)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (handler == null)
                throw new ArgumentNullException("handler");
            image.RemoveHandler(AnimationLoadedEvent, handler);
        }

        /// <summary>
        ///     Identifies the <c>AnimatedSource</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimatedSourceProperty =
            DependencyProperty.RegisterAttached(
                "AnimatedSource",
                typeof (ImageSource),
                typeof (ImageBehavior),
                new UIPropertyMetadata(
                    null,
                    AnimatedSourceChanged));

        /// <summary>
        ///     Identifies the <c>RepeatBehavior</c> attached property.
        /// </summary>
        public static readonly DependencyProperty RepeatBehaviorProperty =
            DependencyProperty.RegisterAttached(
                "RepeatBehavior",
                typeof (RepeatBehavior),
                typeof (ImageBehavior),
                new UIPropertyMetadata(
                    default(RepeatBehavior),
                    RepeatBehaviorChanged));

        /// <summary>
        ///     Identifies the <c>AnimateInDesignMode</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimateInDesignModeProperty =
            DependencyProperty.RegisterAttached(
                "AnimateInDesignMode",
                typeof (bool),
                typeof (ImageBehavior),
                new FrameworkPropertyMetadata(
                    false,
                    FrameworkPropertyMetadataOptions.Inherits,
                    AnimateInDesignModeChanged));

        /// <summary>
        ///     Identifies the <c>AutoStart</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AutoStartProperty =
            DependencyProperty.RegisterAttached("AutoStart", typeof (bool), typeof (ImageBehavior),
                new PropertyMetadata(true));

        /*
        /// <summary>
        ///     Identifies the <c>AnimationController</c> attached property.
        /// </summary>
        public static readonly DependencyProperty AnimationControllerProperty =
            AnimationControllerPropertyKey.DependencyProperty;
        */

        /// <summary>
        ///     Identifies the <c>SynchronizedBySource</c> attached property.
        /// </summary>
        public static readonly DependencyProperty SynchronizedBySourceProperty =
            DependencyProperty.RegisterAttached(
                "SynchronizedBySource",
                typeof (bool),
                typeof (ImageBehavior),
                new PropertyMetadata(true, SynchronizedBySourceChanged));

        /*
        /// <summary>
        ///     Identifies the <c>IsAnimationLoaded</c> attached property.
        /// </summary>
        public static readonly DependencyProperty IsAnimationLoadedProperty =
            IsAnimationLoadedPropertyKey.DependencyProperty;
        */

        /// <summary>
        ///     Identifies the <c>AnimationCompleted</c> attached event.
        /// </summary>
        public static readonly RoutedEvent AnimationCompletedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationCompleted",
                RoutingStrategy.Bubble,
                typeof (RoutedEventHandler),
                typeof (ImageBehavior));

        /// <summary>
        ///     Identifies the <c>AnimationLoaded</c> attached event.
        /// </summary>
        public static readonly RoutedEvent AnimationLoadedEvent =
            EventManager.RegisterRoutedEvent(
                "AnimationLoaded",
                RoutingStrategy.Bubble,
                typeof (RoutedEventHandler),
                typeof (ImageBehavior));

        #endregion

        #region private functions

        private static void SetAnimationController(DependencyObject obj, ImageAnimationController? value)
        {
            obj.SetValue(AnimationControllerPropertyKey, value);
        }

        private static void SetIsAnimationLoaded(Image image, bool value)
        {
            image.SetValue(IsAnimationLoadedPropertyKey, value);
        }

        private static void AnimatedSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs eventArgs)
        {
            var imageControl = obj as Image;
            if (imageControl == null)
                return;

            var oldImageSource = eventArgs.OldValue as ImageSource;
            var newImageSource = eventArgs.NewValue as ImageSource;
            if (oldImageSource != null)
            {
                CloseAnimationOrImage(imageControl);                
            }
            if (newImageSource != null)
            {
                imageControl.Loaded += (s, e) => InitAnimationOrImage(s as Image);
                if (imageControl.IsLoaded) InitAnimationOrImage(imageControl);
                imageControl.Unloaded += (s, e) => CloseAnimationOrImage(s as Image);
            }
        }

        private static void InitAnimationOrImage(Image? imageControl)
        {
            if (imageControl == null)
                return;

            var source = GetAnimatedSource(imageControl) as BitmapSource;
            bool isInDesignMode = DesignerProperties.GetIsInDesignMode(imageControl);
            bool animateInDesignMode = GetAnimateInDesignMode(imageControl);
            bool shouldAnimate = !isInDesignMode || animateInDesignMode;

            // For a BitmapImage with a relative UriSource, the loading is deferred until
            // BaseUri is set. This method will be called again when BaseUri is set.
            bool isLoadingDeferred = IsLoadingDeferred(source);

            if (source != null && shouldAnimate && !isLoadingDeferred)
            {
                // Case of image being downloaded: retry after download is complete
                if (source.IsDownloading)
                {
                    EventHandler? handler = null;
                    handler = (sender, args) =>
                    {
                        source.DownloadCompleted -= handler;
                        InitAnimationOrImage(imageControl);
                    };
                    source.DownloadCompleted += handler;
                    imageControl.Source = source;
                    return;
                }

                ObjectAnimationUsingKeyFrames? animation = GetAnimation(imageControl, source);
                if (animation != null)
                {
                    if (animation.KeyFrames.Count > 0)
                    {
                        // For some reason, it sometimes throws an exception the first time... the second time it works.
                        TryTwice(() => imageControl.Source = (ImageSource)animation.KeyFrames[0].Value);
                    }
                    else
                    {
                        imageControl.Source = source;
                    }

                    RepeatBehavior repeatBehavior = GetRepeatBehavior(imageControl);
                    bool synchronized = GetSynchronizedBySource(imageControl);
                    AnimationCache.IncrementReferenceCount(source, repeatBehavior);
                    AnimationClock? clock;
                    if (synchronized)
                    {
                        clock = AnimationCache.GetClock(source, repeatBehavior);
                        if (clock == null)
                        {
                            clock = animation.CreateClock();
                            AnimationCache.AddClock(source, repeatBehavior, clock);
                        }
                    }
                    else
                    {
                        clock = animation.CreateClock();
                    }
                    var controller = new ImageAnimationController(imageControl, animation, clock);
                    SetAnimationController(imageControl, controller);
                    SetIsAnimationLoaded(imageControl, true);
                    imageControl.RaiseEvent(new RoutedEventArgs(AnimationLoadedEvent, imageControl));
                    return;
                }
            }
            imageControl.Source = source;
        }

        private static void CloseAnimationOrImage(Image? imageControl)
        {
            if (imageControl == null)
                return;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null)
                AnimationCache.DecrementReferenceCount(source, GetRepeatBehavior(imageControl));

            ImageAnimationController controller = GetAnimationController(imageControl);
            if (controller != null)
                controller.Dispose();

            SetAnimationController(imageControl, null);
            SetIsAnimationLoaded(imageControl, false);
        }

        private static void RepeatBehaviorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var imageControl = o as Image;
            if (imageControl == null)
                return;
            
            if (imageControl.IsLoaded)
            {
                CloseAnimationOrImage(imageControl);
                InitAnimationOrImage(imageControl);
            }
        }

        private static void SynchronizedBySourceChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var imageControl = o as Image;
            if (imageControl == null)
                return;
            
            if (imageControl.IsLoaded)
            {
                CloseAnimationOrImage(imageControl);
                InitAnimationOrImage(imageControl);
            }
        }

        private static void AnimateInDesignModeChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var imageControl = o as Image;
            if (imageControl == null)
                return;

            var newValue = (bool) e.NewValue;

            ImageSource source = GetAnimatedSource(imageControl);
            if (source != null && imageControl.IsLoaded)
            {
                if (newValue)
                {
                    CloseAnimationOrImage(imageControl);
                    InitAnimationOrImage(imageControl);
                }
                else
                    imageControl.BeginAnimation(Image.SourceProperty, null);
            }
        }        

        private static ObjectAnimationUsingKeyFrames? GetAnimation(Image imageControl, BitmapSource source)
        {
            ObjectAnimationUsingKeyFrames? animation = AnimationCache.GetAnimation(source,
                GetRepeatBehavior(imageControl));
            if (animation != null)
                return animation;

            GifFile? gifMetadata;
            var decoder = GetDecoder(source, out gifMetadata) as GifBitmapDecoder;
            if (decoder != null && decoder.Frames.Count > 1)
            {
                Int32Size fullSize = GetFullSize(decoder, gifMetadata);
                int index = 0;
                animation = new ObjectAnimationUsingKeyFrames();
                TimeSpan totalDuration = TimeSpan.Zero;
                BitmapSource? baseFrame = null;
                foreach (BitmapFrame rawFrame in decoder.Frames)
                {
                    FrameMetadata metadata = GetFrameMetadata(decoder, gifMetadata, index);

                    BitmapSource frame = MakeFrame(fullSize, rawFrame, metadata, baseFrame);
                    var keyFrame = new DiscreteObjectKeyFrame(frame, totalDuration);
                    animation.KeyFrames.Add(keyFrame);

                    totalDuration += metadata.Delay;

                    switch (metadata.DisposalMethod)
                    {
                        case FrameDisposalMethod.None:
                        case FrameDisposalMethod.DoNotDispose:
                            baseFrame = frame;
                            break;
                        case FrameDisposalMethod.RestoreBackground:
                            if (IsFullFrame(metadata, fullSize))
                            {
                                baseFrame = null;
                            }
                            else
                            {
                                baseFrame = ClearArea(frame, metadata);
                            }
                            break;
                        case FrameDisposalMethod.RestorePrevious:
                            // Reuse same base frame
                            break;
                    }

                    index++;
                }
                animation.Duration = totalDuration;
                animation.RepeatBehavior = GetActualRepeatBehavior(imageControl, decoder, gifMetadata);

                AnimationCache.AddAnimation(source, GetRepeatBehavior(imageControl), animation);
                return animation;
            }
            return null;
        }

        private static BitmapSource ClearArea(BitmapSource frame, FrameMetadata metadata)
        {
            var visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                var fullRect = new Rect(0, 0, frame.PixelWidth, frame.PixelHeight);
                var clearRect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                PathGeometry clip = Geometry.Combine(
                    new RectangleGeometry(fullRect),
                    new RectangleGeometry(clearRect),
                    GeometryCombineMode.Exclude,
                    null);
                context.PushClip(clip);
                context.DrawImage(frame, fullRect);
            }

            var bitmap = new RenderTargetBitmap(
                frame.PixelWidth, frame.PixelHeight,
                frame.DpiX, frame.DpiY,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            if (bitmap.CanFreeze && !bitmap.IsFrozen)
                bitmap.Freeze();
            return bitmap;
        }

        private static void TryTwice(Action action)
        {
            try
            {
                action();
            }
            catch (Exception)
            {
                action();
            }
        }

        private static bool IsLoadingDeferred(BitmapSource? source)
        {
            var bmp = source as BitmapImage;
            if (bmp == null)
                return false;
            if (bmp.UriSource != null && !bmp.UriSource.IsAbsoluteUri)
                return bmp.BaseUri == null;
            return false;
        }

        private static BitmapDecoder? GetDecoder(BitmapSource image, out GifFile? gifFile)
        {
            gifFile = null;
            BitmapDecoder? decoder = null;
            Stream? stream = null;
            Uri? uri = null;
            var createOptions = BitmapCreateOptions.None;

            var bmp = image as BitmapImage;
            if (bmp != null)
            {
                createOptions = bmp.CreateOptions;
                if (bmp.StreamSource != null)
                {
                    stream = bmp.StreamSource;
                }
                else if (bmp.UriSource != null)
                {
                    uri = bmp.UriSource;
                    if (bmp.BaseUri != null && !uri.IsAbsoluteUri)
                        uri = new Uri(bmp.BaseUri, uri);
                }
            }
            else
            {
                var frame = image as BitmapFrame;
                if (frame != null)
                {
                    decoder = frame.Decoder;
                    Uri.TryCreate(frame.BaseUri, frame.ToString(), out uri);
                }
            }

            if (decoder == null)
            {
                if (stream != null)
                {
                    stream.Position = 0;
                    decoder = BitmapDecoder.Create(stream, createOptions, BitmapCacheOption.OnLoad);
                }
                else if (uri != null && uri.IsAbsoluteUri)
                {
                    decoder = BitmapDecoder.Create(uri, createOptions, BitmapCacheOption.OnLoad);
                }
            }

            if (decoder is GifBitmapDecoder && !CanReadNativeMetadata(decoder))
            {
                if (stream != null)
                {
                    stream.Position = 0;
                    gifFile = GifFile.ReadGifFile(stream, true);
                }
                else if (uri != null)
                {
                    gifFile = DecodeGifFile(uri);
                }
            }
            return decoder;
        }

        private static bool CanReadNativeMetadata(BitmapDecoder decoder)
        {
            try
            {
                BitmapMetadata m = decoder.Metadata;
                return m != null;
            }
            catch
            {
                return false;
            }
        }

        private static GifFile? DecodeGifFile(Uri uri)
        {
            Stream? stream = null;
            if (uri.Scheme == PackUriHelper.UriSchemePack)
            {
                StreamResourceInfo sri;
                if (uri.Authority == "siteoforigin:,,,")
                    sri = Application.GetRemoteStream(uri);
                else
                    sri = Application.GetResourceStream(uri);

                if (sri != null)
                    stream = sri.Stream;
                if (stream != null)
                {
                    using (stream)
                    {
                        var gif = GifFile.ReadGifFile(stream, true);
                        stream.Close();
                        return gif;
                    }
                }

            }
            else
            {
				//using (var wc = new WebClient())
				//{
				//	stream = wc.OpenRead(uri);
                //                if (stream != null)
                //                {
                //                    using (stream)
                //                    {
                //                        var gif = GifFile.ReadGifFile(stream, true);
                //                        stream.Close();
                //                        return gif;
                //                    }
                //                }
                //            }

                // TODO: implement using HttpClient
            }
            return null;
        }

        private static bool IsFullFrame(FrameMetadata metadata, Int32Size fullSize)
        {
            return metadata.Left == 0
                   && metadata.Top == 0
                   && metadata.Width == fullSize.Width
                   && metadata.Height == fullSize.Height;
        }

        private static BitmapSource MakeFrame(
            Int32Size fullSize,
            BitmapSource rawFrame, FrameMetadata metadata,
            BitmapSource? baseFrame)
        {
            if (baseFrame == null && IsFullFrame(metadata, fullSize))
            {
                // No previous image to combine with, and same size as the full image
                // Just return the frame as is
                return rawFrame;
            }

            var visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                if (baseFrame != null)
                {
                    var fullRect = new Rect(0, 0, fullSize.Width, fullSize.Height);
                    context.DrawImage(baseFrame, fullRect);
                }

                var rect = new Rect(metadata.Left, metadata.Top, metadata.Width, metadata.Height);
                context.DrawImage(rawFrame, rect);
            }
            var bitmap = new RenderTargetBitmap(
                fullSize.Width, fullSize.Height,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);

            if (bitmap.CanFreeze && !bitmap.IsFrozen)
                bitmap.Freeze();
            return bitmap;
        }

        private static RepeatBehavior GetActualRepeatBehavior(Image imageControl, BitmapDecoder decoder,
            GifFile? gifMetadata)
        {
            // If specified explicitly, use this value
            RepeatBehavior repeatBehavior = GetRepeatBehavior(imageControl);
            if (repeatBehavior != default(RepeatBehavior))
                return repeatBehavior;

            int repeatCount;
            if (gifMetadata != null)
            {
                repeatCount = gifMetadata.RepeatCount;
            }
            else
            {
                repeatCount = GetRepeatCount(decoder);
            }
            if (repeatCount == 0)
                return RepeatBehavior.Forever;
            return new RepeatBehavior(repeatCount);
        }

        private static int GetRepeatCount(BitmapDecoder decoder)
        {
            BitmapMetadata? ext = GetApplicationExtension(decoder, "NETSCAPE2.0");
            if (ext != null)
            {
                var bytes = ext.GetQueryOrNull<byte[]>("/Data");
                if (bytes != null && bytes.Length >= 4)
                    return BitConverter.ToUInt16(bytes, 2);
            }
            return 1;
        }

        private static BitmapMetadata? GetApplicationExtension(BitmapDecoder decoder, string application)
        {
            int count = 0;
            string query = "/appext";
            var extension = decoder.Metadata.GetQueryOrNull<BitmapMetadata>(query);
            while (extension != null)
            {
                var bytes = extension.GetQueryOrNull<byte[]>("/Application");
                if (bytes != null)
                {
                    string extApplication = Encoding.ASCII.GetString(bytes);
                    if (extApplication == application)
                        return extension;
                }
                query = string.Format("/[{0}]appext", ++count);
                extension = decoder.Metadata.GetQueryOrNull<BitmapMetadata>(query);
            }
            return null;
        }

        private static FrameMetadata GetFrameMetadata(BitmapDecoder decoder, GifFile? gifMetadata, int frameIndex)
        {
            if (gifMetadata != null && gifMetadata.Frames.Count > frameIndex)
            {
                return GetFrameMetadata(gifMetadata.Frames[frameIndex]);
            }

            return GetFrameMetadata(decoder.Frames[frameIndex]);
        }

        private static FrameMetadata GetFrameMetadata(BitmapFrame frame)
        {
            var metadata = (BitmapMetadata) frame.Metadata;
            TimeSpan delay = TimeSpan.FromMilliseconds(100);
            int metadataDelay = metadata.GetQueryOrDefault("/grctlext/Delay", 10);
            if (metadataDelay != 0)
                delay = TimeSpan.FromMilliseconds(metadataDelay*10);
            var disposalMethod = (FrameDisposalMethod) metadata.GetQueryOrDefault("/grctlext/Disposal", 0);
            var frameMetadata = new FrameMetadata
                                {
                                    Left = metadata.GetQueryOrDefault("/imgdesc/Left", 0),
                                    Top = metadata.GetQueryOrDefault("/imgdesc/Top", 0),
                                    Width = metadata.GetQueryOrDefault("/imgdesc/Width", frame.PixelWidth),
                                    Height = metadata.GetQueryOrDefault("/imgdesc/Height", frame.PixelHeight),
                                    Delay = delay,
                                    DisposalMethod = disposalMethod
                                };
            return frameMetadata;
        }

        private static FrameMetadata GetFrameMetadata(GifFrame gifMetadata)
        {
            GifImageDescriptor d = gifMetadata.Descriptor;
            var frameMetadata = new FrameMetadata
                                {
                                    Left = d.Left,
                                    Top = d.Top,
                                    Width = d.Width,
                                    Height = d.Height,
                                    Delay = TimeSpan.FromMilliseconds(100),
                                    DisposalMethod = FrameDisposalMethod.None
                                };

            GifGraphicControlExtension? gce =
                gifMetadata.Extensions.OfType<GifGraphicControlExtension>().FirstOrDefault();
            if (gce != null)
            {
                if (gce.Delay != 0)
                    frameMetadata.Delay = TimeSpan.FromMilliseconds(gce.Delay);
                frameMetadata.DisposalMethod = (FrameDisposalMethod) gce.DisposalMethod;
            }
            return frameMetadata;
        }

        private static Int32Size GetFullSize(BitmapDecoder decoder, GifFile? gifMetadata)
        {
            if (gifMetadata != null)
            {
                GifLogicalScreenDescriptor lsd = gifMetadata.Header.LogicalScreenDescriptor;
                return new Int32Size(lsd.Width, lsd.Height);
            }
            int width = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Width", 0);
            int height = decoder.Metadata.GetQueryOrDefault("/logscrdesc/Height", 0);
            return new Int32Size(width, height);
        }
        
        private static T GetQueryOrDefault<T>(this BitmapMetadata metadata, string query, T defaultValue)
        {
            if (metadata.ContainsQuery(query))
                return (T) Convert.ChangeType(metadata.GetQuery(query), typeof (T));
            return defaultValue;
        }

        private static T? GetQueryOrNull<T>(this BitmapMetadata metadata, string query)
            where T : class
        {
            if (metadata.ContainsQuery(query))
                return metadata.GetQuery(query) as T;
            return null;
        }

        #endregion

        #region private fields

        private static readonly DependencyPropertyKey AnimationControllerPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("AnimationController", typeof (ImageAnimationController),
                typeof (ImageBehavior), new PropertyMetadata(null));

        private static readonly DependencyPropertyKey IsAnimationLoadedPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("IsAnimationLoaded", typeof (bool), typeof (ImageBehavior),
                new PropertyMetadata(false));

        #endregion

        private struct Int32Size
        {
            public Int32Size(int width, int height) : this()
            {
                Width = width;
                Height = height;
            }

            public int Width { get; private set; }
            public int Height { get; private set; }
        }

        private class FrameMetadata
        {
            #region public functions

            public int Left { get; set; }
            public int Top { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public TimeSpan Delay { get; set; }
            public FrameDisposalMethod DisposalMethod { get; set; }

            #endregion
        }

        private enum FrameDisposalMethod
        {
            None = 0,
            DoNotDispose = 1,
            RestoreBackground = 2,
            RestorePrevious = 3
        }
    }
}