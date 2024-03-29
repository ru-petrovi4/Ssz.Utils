using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ssz.Utils.Wpf.WpfAnimatedGif
{
    /// <summary>
    ///     Provides a way to pause, resume or seek a GIF animation.
    /// </summary>
    public class ImageAnimationController : IDisposable
    {
        #region construction and destruction

        static ImageAnimationController()
        {
            _sourceDescriptor = DependencyPropertyDescriptor.FromProperty(Image.SourceProperty, typeof (Image));
        }

        internal ImageAnimationController(Image image, ObjectAnimationUsingKeyFrames animation, AnimationClock clock)
        {
            _image = image;
            _animation = animation;
            _animation.Completed += AnimationCompleted;
            _clock = clock;
            _clockController = _clock.Controller;
            _sourceDescriptor.AddValueChanged(image, ImageSourceChanged);

            // ReSharper disable PossibleNullReferenceException
            _clockController.Pause();
            // ReSharper restore PossibleNullReferenceException

            _image.ApplyAnimationClock(Image.SourceProperty, _clock);

            if (ImageBehavior.GetAutoStart(image))
                _clockController.Resume();
        }

        /// <summary>
        ///     Disposes the current object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Disposes the current object
        /// </summary>
        /// <param name="disposing">true to dispose both managed an unmanaged resources, false to dispose only managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _image.BeginAnimation(Image.SourceProperty, null);
                _animation.Completed -= AnimationCompleted;
                _sourceDescriptor.RemoveValueChanged(_image, ImageSourceChanged);
            }
        }

        /// <summary>
        ///     Finalizes the current object.
        /// </summary>
        ~ImageAnimationController()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Seeks the animation to the specified frame index.
        /// </summary>
        /// <param name="index">The index of the frame to seek to</param>
        public void GotoFrame(int index)
        {
            ObjectKeyFrame frame = _animation.KeyFrames[index];
            _clockController.Seek(frame.KeyTime.TimeSpan, TimeSeekOrigin.BeginTime);
        }

        /// <summary>
        ///     Pauses the animation.
        /// </summary>
        public void Pause()
        {
            _clockController.Pause();
        }

        /// <summary>
        ///     Starts or resumes the animation. If the animation is complete, it restarts from the beginning.
        /// </summary>
        public void Play()
        {
            switch (_clock.CurrentState)
            {
                case ClockState.Active:
                    _clockController.Resume();
                    break;
                case ClockState.Filling:
                case ClockState.Stopped:
                    _clockController.Begin();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        ///     Raised when the current frame changes.
        /// </summary>
        public event EventHandler? CurrentFrameChanged;

        /// <summary>
        ///     Returns the number of frames in the image.
        /// </summary>
        public int FrameCount
        {
            get { return _animation.KeyFrames.Count; }
        }

        /// <summary>
        ///     Returns a value that indicates whether the animation is paused.
        /// </summary>
        public bool IsPaused
        {
            get { return _clock.IsPaused; }
        }

        /// <summary>
        ///     Returns a value that indicates whether the animation is complete.
        /// </summary>
        public bool IsComplete
        {
            get { return _clock.CurrentState == ClockState.Filling; }
        }

        /// <summary>
        ///     Returns the current frame index.
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                TimeSpan? time = _clock.CurrentTime;
                var frameAndIndex =
                    _animation.KeyFrames
                        .Cast<ObjectKeyFrame>()
                        .Select((f, i) => new {Time = f.KeyTime.TimeSpan, Index = i})
                        .FirstOrDefault(fi => fi.Time >= time);
                if (frameAndIndex != null)
                    return frameAndIndex.Index;
                return -1;
            }
        }

        #endregion

        #region private functions

        private void AnimationCompleted(object? sender, EventArgs e)
        {
            _image.RaiseEvent(new RoutedEventArgs(ImageBehavior.AnimationCompletedEvent, _image));
        }

        private void ImageSourceChanged(object? sender, EventArgs e)
        {
            OnCurrentFrameChanged();
        }

        private void OnCurrentFrameChanged()
        {
            EventHandler? handler = CurrentFrameChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        #endregion

        #region private fields

        private static readonly DependencyPropertyDescriptor _sourceDescriptor;
        private readonly Image _image;
        private readonly ObjectAnimationUsingKeyFrames _animation;
        private readonly AnimationClock _clock;
        private readonly ClockController _clockController;

        #endregion
    }
}