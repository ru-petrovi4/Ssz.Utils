using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core.DataAccess
{
    /// <summary>
    ///     Plays a sound once, or over and over until it is stopped.
    ///     <para>
    ///         The alarm sounds were written against WPF's System.Media.SoundPlayer, and that type is
    ///         on .NET for Windows alone: on Linux its constructor throws
    ///         PlatformNotSupportedException, which took the whole application down the moment the
    ///         first alarm arrived. Windows is served here by the same call SoundPlayer makes
    ///         underneath, and the other systems by whichever sound player they have.
    ///     </para>
    ///     <para>
    ///         Nothing here throws: an operator who cannot hear an alarm must still see it.
    ///     </para>
    /// </summary>
    public sealed class SoundPlayer : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     The sound as bytes, which is how the addons keep their alarm sounds - in resources.
        /// </summary>
        public SoundPlayer(Stream? stream)
        {
            _soundFileFullName = GetFileFullNameOrNull(stream);
        }

        public SoundPlayer(string? soundFileFullName)
        {
            _soundFileFullName = String.IsNullOrEmpty(soundFileFullName) ? null : soundFileFullName;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            Stop();
        }

        #endregion

        #region public functions

        public void Play()
        {
            StartPlaying(false);
        }

        public void PlayLooping()
        {
            StartPlaying(true);
        }

        public void Stop()
        {
            lock (_syncRoot)
            {
                _playLooping = false;

                if (OperatingSystem.IsWindows())
                {
                    if (!_isPlaying) return;
                    _isPlaying = false;

                    // As with SoundPlayer, this stops whatever this process is playing and not only
                    // this sound: winmm knows one sound per process.
                    TryPlaySoundWindows(null, SndPurge);
                    return;
                }

                _isPlaying = false;

                Process? process = _process;
                _process = null;
                if (process is null) return;

                try
                {
                    if (!process.HasExited) process.Kill(true);
                }
                catch (Exception)
                {
                    // It has gone on its own, which is what was wanted.
                }
                finally
                {
                    process.Dispose();
                }
            }
        }

        #endregion

        #region private functions

        private void StartPlaying(bool playLooping)
        {
            lock (_syncRoot)
            {
                if (_disposed || _soundFileFullName is null) return;

                _playLooping = playLooping;
                _isPlaying = true;

                if (OperatingSystem.IsWindows())
                {
                    var flags = SndFileName | SndAsync | SndNoDefault;
                    if (playLooping) flags |= SndLoop;
                    TryPlaySoundWindows(_soundFileFullName, flags);
                    return;
                }

                // A browser has no processes to start; its sounds would have to come from the page.
                if (OperatingSystem.IsBrowser()) return;

                StartPlayerProcess();
            }
        }

        /// <summary>
        ///     Hands the sound to the player of the desktop this runs on. Called while the lock is held.
        /// </summary>
        private void StartPlayerProcess()
        {
            foreach ((string fileName, string argumentsFormat) in PlayerCommands)
            {
                if (_playerFileName is not null && _playerFileName != fileName) continue;

                var startInfo = new ProcessStartInfo(fileName,
                    String.Format(CultureInfo.InvariantCulture, argumentsFormat, _soundFileFullName))
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    // What the player has to say about a sound card that is not there belongs in the
                    // log of this application, not in the middle of the operator's console.
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                Process? process;
                try
                {
                    process = Process.Start(startInfo);
                }
                catch (Exception)
                {
                    // This player is not installed; the next one is tried.
                    continue;
                }

                if (process is null) continue;

                // The one that worked is the one tried first from now on.
                _playerFileName = fileName;
                _process = process;
                _playStartedUtc = DateTime.UtcNow;

                try
                {
                    process.ErrorDataReceived += OnPlayerProcessErrorDataReceived;
                    process.OutputDataReceived += (sender, e) => { };
                    process.BeginErrorReadLine();
                    process.BeginOutputReadLine();
                }
                catch (Exception)
                {
                    // Reading what it says is a nicety; playing the sound is the point.
                }

                if (_playLooping)
                {
                    // The players here play the file once, so the sound is started again each time
                    // the player ends, until Stop is called.
                    process.EnableRaisingEvents = true;
                    process.Exited += (sender, e) => OnPlayerProcessExited(process);
                }

                return;
            }

            if (!_noPlayerReported)
            {
                _noPlayerReported = true;
                DsProject.LoggersSet?.Logger.LogWarning(
                    @"No sound player found, so alarms will be silent. One of these is needed: " +
                    String.Join(@", ", PlayerFileNames));
            }
        }

        private void OnPlayerProcessExited(Process process)
        {
            lock (_syncRoot)
            {
                // Stop, or another sound, has taken over in the meantime.
                if (_disposed || !_playLooping || !ReferenceEquals(_process, process)) return;

                _process = null;

                // A player that gives up at once, with an error, has nothing to play on - a machine
                // with no sound card, say. Starting it again would spin on the spot, so the sound is
                // given up on instead and said once.
                var exitCode = TryGetExitCode(process);
                if (exitCode != 0 && DateTime.UtcNow - _playStartedUtc < MinimumPlayingTime)
                {
                    _playLooping = false;
                    _isPlaying = false;

                    if (!_playerFailedReported)
                    {
                        _playerFailedReported = true;
                        DsProject.LoggersSet?.Logger.LogWarning(
                            @"The sound player {0} ended at once with code {1}, so alarms will be silent. {2}",
                            _playerFileName, exitCode, _playerFirstErrorLine ?? @"");
                    }

                    return;
                }
            }

            // Started from outside the handler of the player that has just ended: starting a process
            // from its own event, while holding the lock Stop needs, is a good way to stand still.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                lock (_syncRoot)
                {
                    if (_disposed || !_playLooping || _process is not null) return;

                    StartPlayerProcess();
                }
            });
        }

        private void OnPlayerProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null || _playerFirstErrorLine is not null) return;

            _playerFirstErrorLine = e.Data;
        }

        private static int TryGetExitCode(Process process)
        {
            try
            {
                return process.ExitCode;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        private static void TryPlaySoundWindows(string? soundFileFullName, uint flags)
        {
            try
            {
                PlaySoundW(soundFileFullName, IntPtr.Zero, flags);
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet?.Logger.LogWarning(ex, @"Cannot play sound: {0}", soundFileFullName);
            }
        }

        /// <summary>
        ///     Writes the sound where the player of the desktop can reach it. The same sound is written
        ///     once, however many times it is played.
        /// </summary>
        private static string? GetFileFullNameOrNull(Stream? stream)
        {
            if (stream is null) return null;

            try
            {
                if (stream.CanSeek) stream.Position = 0;

                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                byte[] bytes = memoryStream.ToArray();
                if (bytes.Length == 0) return null;

                var name = Convert.ToHexString(SHA256.HashData(bytes)).Substring(0, 16);

                lock (SoundFilesSyncRoot)
                {
                    if (SoundFiles.TryGetValue(name, out string? existingFileFullName) &&
                        File.Exists(existingFileFullName))
                        return existingFileFullName;

                    var directory = Path.Combine(Path.GetTempPath(), @"Ssz.Operator.Sounds");
                    Directory.CreateDirectory(directory);

                    var fileFullName = Path.Combine(directory, name + @".wav");
                    if (!File.Exists(fileFullName)) File.WriteAllBytes(fileFullName, bytes);

                    SoundFiles[name] = fileFullName;
                    return fileFullName;
                }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet?.Logger.LogWarning(ex, @"Cannot prepare a sound to play.");
                return null;
            }
        }

        [DllImport(@"winmm.dll", CharSet = CharSet.Unicode, EntryPoint = @"PlaySoundW")]
        private static extern bool PlaySoundW(string? soundFileFullName, IntPtr moduleHandle, uint flags);

        #endregion

        #region private fields

        private const uint SndAsync = 0x0001;
        private const uint SndNoDefault = 0x0002;
        private const uint SndLoop = 0x0008;
        private const uint SndPurge = 0x0040;
        private const uint SndFileName = 0x00020000;

        /// <summary>
        ///     What the desktops can play a wave file with, in the order they are tried. The first one
        ///     that starts is remembered, so the others are asked for only once.
        /// </summary>
        private static readonly (string FileName, string ArgumentsFormat)[] PlayerCommands =
        {
            (@"paplay", @"""{0}"""),
            (@"aplay", @"-q ""{0}"""),
            (@"play", @"-q ""{0}"""),
            (@"ffplay", @"-nodisp -autoexit -loglevel quiet ""{0}"""),
            (@"canberra-gtk-play", @"-f ""{0}"""),
            (@"afplay", @"""{0}""")
        };

        private static IEnumerable<string> PlayerFileNames
        {
            get
            {
                foreach ((string fileName, string _) in PlayerCommands) yield return fileName;
            }
        }

        /// <summary>
        ///     Shorter than this, and an ended player is taken to have failed rather than played.
        /// </summary>
        private static readonly TimeSpan MinimumPlayingTime = TimeSpan.FromSeconds(1);

        private static string? _playerFileName;
        private static bool _noPlayerReported;
        private static bool _playerFailedReported;
        private static string? _playerFirstErrorLine;

        private static readonly Dictionary<string, string> SoundFiles = new();
        private static readonly object SoundFilesSyncRoot = new();

        private readonly object _syncRoot = new();
        private readonly string? _soundFileFullName;

        private Process? _process;
        private DateTime _playStartedUtc;
        private bool _playLooping;
        private bool _isPlaying;
        private bool _disposed;

        #endregion
    }
}
