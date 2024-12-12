using System;
using System.Collections.Generic;
using System.Media;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.DataAccess
{
    public class Buzzer
    {
        #region public functions

        public BuzzerStateEnum BuzzerState
        {
            get => _buzzerState;
            set
            {
                if (_buzzerState != value)
                {
                    _buzzerState = value;
                    Refresh();
                }
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;                    
                    Refresh();
                }
            }
        }

        public void ClearCustomSoundConfiguration()
        {
            _customSoundsConfiguration.Clear();
            Refresh();
        }

        public void SetCustomSoundConfiguration(BuzzerStateEnum buzzerState, string soundFileName,
            bool playLooping = true)
        {
            _customSoundsConfiguration[buzzerState] = new SoundConfiguration
                {
                    SoundFileName = soundFileName,
                    PlayLooping = playLooping
                };
        }

        #endregion

        #region private functions

        private void Refresh()
        {
            // Stop previous player
            if (_soundPlayer is not null)
            {
                _soundPlayer.Stop();
                _soundPlayer.Dispose();
                _soundPlayer = null;
            }

            if (DsProject.Instance.NoAlarmsSound ||
                    _buzzerState == BuzzerStateEnum.Silent ||
                    !_isEnabled)
                return;            

            SoundConfiguration? config;
            if (_customSoundsConfiguration.TryGetValue(_buzzerState, out config))
            {
                if (string.IsNullOrEmpty(config.SoundFileName))
                {
                    // sound is disabled for particular alarm 
                    // do nothing
                }
                else
                {
                    // play custom sound
                    try
                    {
                        _soundPlayer = new SoundPlayer(config.SoundFileName);
                        if (config.PlayLooping)
                            _soundPlayer.PlayLooping();
                        else
                            _soundPlayer.Play();
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogWarning(ex, "Cannot play sound file: " + config.SoundFileName);
                        if (_soundPlayer is not null)
                        {
                            _soundPlayer.Stop();
                            _soundPlayer.Dispose();
                            _soundPlayer = null;
                        }
                    }
                }
            }            
        }

        #endregion

        #region private fields

        private SoundPlayer? _soundPlayer;
        private bool _isEnabled = true;
        private BuzzerStateEnum _buzzerState = BuzzerStateEnum.Silent;

        private class SoundConfiguration
        {
            public string SoundFileName { get; set; } = @"";
            public bool PlayLooping { get; set; }
        }

        private readonly Dictionary<BuzzerStateEnum, SoundConfiguration> _customSoundsConfiguration = new();

        #endregion
    }

    public enum BuzzerStateEnum
    {
        Silent,
        ProcessAlarmHighPriority,
        ProcessAlarmMediumPriority,
        ProcessAlarmRecover,
        Reconfirmation,
        MisToolkitOperation,
        ToolkitOperationGuide,
        UserDefined1,
        UserDefined2,
        UserDefined3
    }
}