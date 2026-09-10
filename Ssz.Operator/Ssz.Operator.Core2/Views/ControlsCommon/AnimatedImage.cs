using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;

namespace Ssz.Operator.Core.ControlsCommon
{
    /// <summary>
    ///     An animated picture that draws itself the ordinary way.
    ///     Avalonia.Labs.Gif hands its frames to the compositor, so they never reach a rendering into a
    ///     bitmap - and the panorama draws its whole page into one, which left every animated hotspot of
    ///     it invisible. Here the frames are decoded up front and the current one is drawn in Render, so
    ///     the animation shows up wherever the control is drawn.
    /// </summary>
    public class AnimatedImage : Control
    {
        #region construction and destruction

        static AnimatedImage()
        {
            AffectsRender<AnimatedImage>(CurrentFrameProperty, StretchProperty, StretchDirectionProperty);
            AffectsMeasure<AnimatedImage>(CurrentFrameProperty, StretchProperty, StretchDirectionProperty);
        }

        #endregion

        #region public functions

        public static readonly StyledProperty<Stream?> SourceProperty =
            AvaloniaProperty.Register<AnimatedImage, Stream?>(nameof(Source));

        public static readonly StyledProperty<Stretch> StretchProperty =
            AvaloniaProperty.Register<AnimatedImage, Stretch>(nameof(Stretch), Stretch.Uniform);

        public static readonly StyledProperty<StretchDirection> StretchDirectionProperty =
            AvaloniaProperty.Register<AnimatedImage, StretchDirection>(nameof(StretchDirection),
                StretchDirection.Both);

        /// <summary>
        ///     The frame shown right now. It is a property of its own so that everything that follows the
        ///     changes of a page - the texture of the panorama above all - notices the animation.
        /// </summary>
        public static readonly StyledProperty<IImage?> CurrentFrameProperty =
            AvaloniaProperty.Register<AnimatedImage, IImage?>(nameof(CurrentFrame));

        public Stream? Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public Stretch Stretch
        {
            get => GetValue(StretchProperty);
            set => SetValue(StretchProperty, value);
        }

        public StretchDirection StretchDirection
        {
            get => GetValue(StretchDirectionProperty);
            set => SetValue(StretchDirectionProperty, value);
        }

        public IImage? CurrentFrame
        {
            get => GetValue(CurrentFrameProperty);
            private set => SetValue(CurrentFrameProperty, value);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var source = CurrentFrame;
            if (source is null) return;

            var sourceSize = source.Size;
            var viewPort = new Rect(Bounds.Size);
            if (sourceSize.Width <= 0 || sourceSize.Height <= 0 ||
                viewPort.Width <= 0 || viewPort.Height <= 0) return;

            var scale = Stretch.CalculateScaling(viewPort.Size, sourceSize, StretchDirection);
            var scaledSize = new Size(sourceSize.Width * scale.X, sourceSize.Height * scale.Y);
            var destinationRect = viewPort.CenterRect(new Rect(scaledSize)).Intersect(viewPort);
            var sourceRect = new Rect(sourceSize).CenterRect(
                new Rect(new Size(destinationRect.Width / scale.X, destinationRect.Height / scale.Y)));

            context.DrawImage(source, sourceRect, destinationRect);
        }

        #endregion

        #region protected functions

        protected override Size MeasureOverride(Size availableSize)
        {
            var source = CurrentFrame;
            return source is null
                ? new Size()
                : Stretch.CalculateSize(availableSize, source.Size, StretchDirection);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var source = CurrentFrame;
            return source is null ? new Size() : Stretch.CalculateSize(finalSize, source.Size);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SourceProperty)
                Load(change.GetNewValue<Stream?>());
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            StartAnimation();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            StopAnimation();
        }

        #endregion

        #region private functions

        private void Load(Stream? stream)
        {
            StopAnimation();

            foreach (var frame in _frames)
                frame.Image.Dispose();
            _frames.Clear();
            _frameIndex = 0;
            CurrentFrame = null;

            if (stream is null) return;

            try
            {
                Decode(stream);
            }
            catch (Exception)
            {
                // A picture that cannot be decoded is simply not shown, as it was with the old control.
            }

            if (_frames.Count == 0) return;

            CurrentFrame = _frames[0].Image;
            StartAnimation();
        }

        private void Decode(Stream stream)
        {
            if (stream.CanSeek) stream.Position = 0;

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            if (stream.CanSeek) stream.Position = 0;

            using var data = SKData.CreateCopy(memoryStream.GetBuffer(), (ulong) memoryStream.Length);
            using var codec = SKCodec.Create(data);
            if (codec is null) return;

            var imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Bgra8888,
                SKAlphaType.Premul);
            if (imageInfo.Width <= 0 || imageInfo.Height <= 0) return;

            using var workingBitmap = new SKBitmap(imageInfo);

            var frameCount = Math.Max(1, codec.FrameCount);
            for (var i = 0; i < frameCount; i += 1)
            {
                var result = codec.FrameCount == 0
                    ? codec.GetPixels(imageInfo, workingBitmap.GetPixels())
                    : codec.GetPixels(imageInfo, workingBitmap.GetPixels(), imageInfo.RowBytes,
                        // A frame of a GIF is usually only what changed since the one before it, so the
                        // buffer of the previous frame is where it is drawn on top of.
                        new SKCodecOptions(i, codec.FrameInfo[i].RequiredFrame == i - 1 ? i - 1 : -1));

                if (result != SKCodecResult.Success && result != SKCodecResult.IncompleteInput) break;

                _frames.Add((ToBitmap(workingBitmap, imageInfo),
                    codec.FrameCount == 0 ? 0 : GetDurationMs(codec.FrameInfo[i].Duration)));
            }
        }

        /// <summary>
        ///     What browsers do with the delays a GIF carries: nothing at all is taken as a tenth of a
        ///     second, which is what such a file was authored for.
        /// </summary>
        private static int GetDurationMs(int durationMs)
        {
            return durationMs < 20 ? 100 : durationMs;
        }

        private static unsafe Bitmap ToBitmap(SKBitmap skBitmap, SKImageInfo imageInfo)
        {
            var writeableBitmap = new WriteableBitmap(new PixelSize(imageInfo.Width, imageInfo.Height),
                new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

            using (var frameBuffer = writeableBitmap.Lock())
            {
                var source = skBitmap.GetPixels();
                var rowBytes = Math.Min(frameBuffer.RowBytes, imageInfo.RowBytes);

                for (var y = 0; y < imageInfo.Height; y += 1)
                    Buffer.MemoryCopy((void*) (source + y * imageInfo.RowBytes),
                        (void*) (frameBuffer.Address + y * frameBuffer.RowBytes),
                        frameBuffer.RowBytes, rowBytes);
            }

            return writeableBitmap;
        }

        private void StartAnimation()
        {
            if (_timer is not null || _frames.Count < 2) return;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(_frames[_frameIndex].DurationMs) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void StopAnimation()
        {
            if (_timer is null) return;

            _timer.Stop();
            _timer.Tick -= OnTimerTick;
            _timer = null;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (_frames.Count == 0) return;

            _frameIndex = (_frameIndex + 1) % _frames.Count;
            CurrentFrame = _frames[_frameIndex].Image;

            if (_timer is not null)
                _timer.Interval = TimeSpan.FromMilliseconds(_frames[_frameIndex].DurationMs);
        }

        #endregion

        #region private fields

        private readonly List<(Bitmap Image, int DurationMs)> _frames = new();
        private int _frameIndex;
        private DispatcherTimer? _timer;

        #endregion
    }
}
