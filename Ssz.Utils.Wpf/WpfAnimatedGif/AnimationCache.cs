using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Ssz.Utils.Wpf.WpfAnimatedGif
{
    internal static class AnimationCache
    {
        #region public functions

        public static void IncrementReferenceCount(ImageSource source, RepeatBehavior repeatBehavior)
        {
            var cacheKey = new CacheKey(source, repeatBehavior);
            int count;
            _referenceCount.TryGetValue(cacheKey, out count);
            count++;
            _referenceCount[cacheKey] = count;
        }

        public static void DecrementReferenceCount(ImageSource source, RepeatBehavior repeatBehavior)
        {
            var cacheKey = new CacheKey(source, repeatBehavior);
            int count;
            _referenceCount.TryGetValue(cacheKey, out count);
            if (count > 0)
            {
                count--;
                _referenceCount[cacheKey] = count;
            }
            if (count == 0)
            {
                _animationCache.Remove(cacheKey);
                _referenceCount.Remove(cacheKey);
                _clockCache.Remove(cacheKey);
            }
        }

        public static void AddAnimation(ImageSource source, RepeatBehavior repeatBehavior,
            ObjectAnimationUsingKeyFrames animation)
        {
            var key = new CacheKey(source, repeatBehavior);
            _animationCache[key] = animation;
        }

        public static void RemoveAnimation(ImageSource source, RepeatBehavior repeatBehavior,
            ObjectAnimationUsingKeyFrames animation)
        {
            var key = new CacheKey(source, repeatBehavior);
            _animationCache.Remove(key);
        }

        public static ObjectAnimationUsingKeyFrames? GetAnimation(ImageSource source, RepeatBehavior repeatBehavior)
        {
            var key = new CacheKey(source, repeatBehavior);
            ObjectAnimationUsingKeyFrames? animation;
            _animationCache.TryGetValue(key, out animation);
            return animation;
        }

        public static void AddClock(ImageSource source, RepeatBehavior repeatBehavior, AnimationClock clock)
        {
            var key = new CacheKey(source, repeatBehavior);
            _clockCache[key] = clock;
        }

        public static AnimationClock? GetClock(ImageSource source, RepeatBehavior repeatBehavior)
        {
            var key = new CacheKey(source, repeatBehavior);
            AnimationClock? clock;
            _clockCache.TryGetValue(key, out clock);
            return clock;
        }

        #endregion

        #region private fields

        private static readonly Dictionary<CacheKey, ObjectAnimationUsingKeyFrames> _animationCache =
            new Dictionary<CacheKey, ObjectAnimationUsingKeyFrames>();

        private static readonly Dictionary<CacheKey, int> _referenceCount =
            new Dictionary<CacheKey, int>();

        private static readonly Dictionary<CacheKey, AnimationClock> _clockCache =
            new Dictionary<CacheKey, AnimationClock>();

        #endregion

        private class CacheKey
        {
            #region construction and destruction

            public CacheKey(ImageSource source, RepeatBehavior repeatBehavior)
            {
                _source = source;
                _repeatBehavior = repeatBehavior;
            }

            #endregion

            #region public functions

            public override bool Equals(object? obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((CacheKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (ImageGetHashCode(_source)*397) ^ _repeatBehavior.GetHashCode();
                }
            }

            #endregion

            #region private functions

            private static int ImageGetHashCode(ImageSource image)
            {
                if (image != null)
                {
                    Uri? uri = GetUri(image);
                    if (uri != null)
                        return uri.GetHashCode();
                }
                return 0;
            }

            private static bool ImageEquals(ImageSource? x, ImageSource? y)
            {
                if (Equals(x, y))
                    return true;
                if ((x == null) != (y == null))
                    return false;

                // They can't both be null or Equals would have returned true
                // and if any is null, the previous would have detected it
                // ReSharper disable PossibleNullReferenceException
                if (x!.GetType() != y!.GetType())
                    return false;
                // ReSharper restore PossibleNullReferenceException

                Uri? xUri = GetUri(x);
                Uri? yUri = GetUri(y);
                return xUri != null && xUri == yUri;
            }

            private static Uri? GetUri(ImageSource image)
            {
                var bmp = image as BitmapImage;
                if (bmp != null)
                {
                    if (bmp.BaseUri != null && bmp.UriSource != null && !bmp.UriSource.IsAbsoluteUri)
                        return new Uri(bmp.BaseUri, bmp.UriSource);
                }
                var frame = image as BitmapFrame;
                if (frame != null)
                {
                    string s = frame.ToString();
                    if (s != frame.GetType().FullName)
                    {
                        Uri? fUri;
                        if (Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out fUri))
                        {
                            if (fUri.IsAbsoluteUri)
                                return fUri;
                            if (frame.BaseUri != null)
                                return new Uri(frame.BaseUri, fUri);
                        }
                    }
                }
                return null;
            }

            private bool Equals(CacheKey other)
            {
                return ImageEquals(_source, other._source) && Equals(_repeatBehavior, other._repeatBehavior);
            }

            #endregion

            #region private fields

            private readonly ImageSource _source;
            private readonly RepeatBehavior _repeatBehavior;

            #endregion
        }
    }
}