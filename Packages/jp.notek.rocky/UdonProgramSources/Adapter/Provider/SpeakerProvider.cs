
using System;
using JP.Notek.Udux;
using UnityEngine;

namespace JP.Notek.Rocky
{
    public class SpeakerProvider : IStoreObservable<VideoPlayer>
    {
        [SerializeField] VideoPlayerStore _Store;
        [SerializeField] AudioSource _LeftOut;
        [SerializeField] AudioSource _RightOut;
        [SerializeField] AudioSource _SubWooferOut;

        [SerializeField] int _EffectBufferLength = 1024 * 4;
        [SerializeField] AudioSource _InternalStereoIn;
        [SerializeField] AudioSource _InternalLeftOutDirect;
        [SerializeField] AudioSource _InternalRightOutDirect;
        [SerializeField] float _BypassVolumeMagnification = 0.5f;
        [SerializeField] float _SubWooferMagnification = 2.0f;

        AudioClip _SubWooferOutputClip;
        AudioClip _LeftOutputClip;
        AudioClip _RightOutputClip;

        float[][] _ReadBuffer;
        float[] _MonoWriteBuffer;
        long _PreviousDspTimeSample = -1;
        int _SubWooferOutputClipWriteHead;
        int _LeftOutputClipWriteHead;
        int _RightOutputClipWriteHead;
        int _OutputClipFrames;
        bool _EffectEnable = true;

        void Start()
        {
            _Store.SubscribeOnChange(this);
            InitializeEffector();
            OnChange(null, _Store.NewState);
        }

        void FixedUpdate()
        {
            if (_EffectEnable)
            {
                UpdateAudioClip();
            }
        }

        public override void OnChange(VideoPlayer currentState, VideoPlayer newState)
        {
            if (currentState == null || currentState.Active != newState.Active)
            {
                if (currentState != null && newState.Active)
                {
                    //再起動した場合状態を強制更新
                    OnChange(null, newState);
                }
                _LeftOut.mute = !newState.Active;
                _RightOut.mute = !newState.Active;
                _SubWooferOut.mute = !newState.Active;
                _InternalLeftOutDirect.mute = !newState.Active;
                _InternalRightOutDirect.mute = !newState.Active;
            }

            if (currentState == null
            || currentState.BypassAudioEffect != newState.BypassAudioEffect
            || currentState.Active != newState.Active
            )
            {
                _EffectEnable = !newState.BypassAudioEffect && newState.Active;
            }

            if (currentState == null || currentState.AudioVolume != newState.AudioVolume)
            {
                _LeftOut.volume = newState.AudioVolume;
                _RightOut.volume = newState.AudioVolume;
                _InternalLeftOutDirect.volume = newState.AudioVolume * _BypassVolumeMagnification;
                _InternalRightOutDirect.volume = newState.AudioVolume * _BypassVolumeMagnification;
            }

            if (currentState == null
            || currentState.SubWooferRelativeVolume != newState.SubWooferRelativeVolume
            || currentState.AudioVolume != newState.AudioVolume
            )
            {
                _SubWooferOut.volume = newState.AudioVolume * newState.SubWooferRelativeVolume * _SubWooferMagnification;
            }

            if (
                currentState == null
            || currentState.BypassAudioEffect != newState.BypassAudioEffect
            || currentState.AudioMuted != newState.AudioMuted
            )
            {
                _LeftOut.mute = newState.BypassAudioEffect || newState.AudioMuted;
                _RightOut.mute = newState.BypassAudioEffect || newState.AudioMuted;
                _SubWooferOut.mute = newState.BypassAudioEffect || newState.AudioMuted;
                _InternalLeftOutDirect.mute = !newState.BypassAudioEffect || newState.AudioMuted;
                _InternalRightOutDirect.mute = !newState.BypassAudioEffect || newState.AudioMuted;
            }
        }

        void InitializeEffector()
        {
            _OutputClipFrames = _EffectBufferLength * 4;
            _SubWooferOutputClip = AudioClip.Create("SubWoofer Output Clip", _OutputClipFrames, 1, AudioSettings.outputSampleRate, false);
            if (_SubWooferOut != null)
            {
                _SubWooferOut.loop = true;
                _SubWooferOut.clip = _SubWooferOutputClip;
            }

            _LeftOutputClip = AudioClip.Create("Left Output Clip", _OutputClipFrames, 1, AudioSettings.outputSampleRate, false);
            if (_LeftOut != null)
            {
                _LeftOut.loop = true;
                _LeftOut.clip = _LeftOutputClip;
            }

            _RightOutputClip = AudioClip.Create("Right Output Clip", _OutputClipFrames, 1, AudioSettings.outputSampleRate, false);
            if (_RightOut != null)
            {
                _RightOut.loop = true;
                _RightOut.clip = _RightOutputClip;
            }

            _ReadBuffer = new[] { new float[_EffectBufferLength], new float[_EffectBufferLength] };
            _MonoWriteBuffer = new float[_EffectBufferLength];
        }

        void ResetAudioClip(long currentDspTimeSample = -1)
        {
            _PreviousDspTimeSample = currentDspTimeSample;
            _SubWooferOutputClipWriteHead = 0;
            if (_SubWooferOut) _SubWooferOut.Stop();

            _LeftOutputClipWriteHead = 0;
            if (_LeftOut) _LeftOut.Stop();

            _RightOutputClipWriteHead = 0;
            if (_RightOut) _RightOut.Stop();
        }

        void StopEffect()
        {
            _LeftOutputClipWriteHead = 0;
            _RightOutputClipWriteHead = 0;
            _SubWooferOutputClipWriteHead = 0;
            _RightOut.Stop();
            _LeftOut.Stop();
            _SubWooferOut.Stop();
        }

        void UpdateAudioClip()
        {
            var currentDspTimeSample = (long)Math.Floor(AudioSettings.dspTime * AudioSettings.outputSampleRate);
            if (_PreviousDspTimeSample < 0)
            {
                _PreviousDspTimeSample = currentDspTimeSample;
                return;
            }

            var freshDataFrames = (int)(currentDspTimeSample - _PreviousDspTimeSample);
            if (freshDataFrames <= 0) return;
            if (freshDataFrames > _EffectBufferLength)
            {
                // Main thread stopped too much. Clear buffer and restart.
                ResetAudioClip(currentDspTimeSample);
                return;
            }

            var readBeginIndex = _EffectBufferLength - freshDataFrames;

            _PreviousDspTimeSample = currentDspTimeSample;

            _InternalStereoIn.GetOutputData(_ReadBuffer[0], 0);
            _InternalStereoIn.GetOutputData(_ReadBuffer[1], 1);

            // mono left
            if (_LeftOut)
            {
                if (!_LeftOut.gameObject.activeInHierarchy)
                {
                    _LeftOutputClipWriteHead = 0;
                }
                else
                {
                    var timeSamples = _LeftOut.timeSamples;
                    if (_LeftOut.isPlaying &&
                        (
                            (_LeftOutputClipWriteHead <= timeSamples && timeSamples < _LeftOutputClipWriteHead + _EffectBufferLength)
                            || (timeSamples < _LeftOutputClipWriteHead && timeSamples + _OutputClipFrames < _LeftOutputClipWriteHead + _EffectBufferLength)
                         )
                        )
                    {
                        // ring buffer exhausted
                        StopEffect();
                        return;
                    }

                    Array.Copy(_ReadBuffer[0], readBeginIndex, _MonoWriteBuffer, 0, freshDataFrames);
                    _LeftOutputClip.SetData(_MonoWriteBuffer, _LeftOutputClipWriteHead);

                    _LeftOutputClipWriteHead += freshDataFrames;

                    if (!_LeftOut.isPlaying && _LeftOutputClipWriteHead >= _EffectBufferLength)
                    {
                        _LeftOut.Play();
                    }

                    if (_LeftOutputClipWriteHead >= _OutputClipFrames)
                    {
                        _LeftOutputClipWriteHead -= _OutputClipFrames;
                    }
                }
            }

            // mono right
            if (_RightOut)
            {
                if (!_RightOut.gameObject.activeInHierarchy)
                {
                    _RightOutputClipWriteHead = 0;
                }
                else
                {
                    var timeSamples = _RightOut.timeSamples;
                    if (_RightOut.isPlaying &&
                        (
                            (_RightOutputClipWriteHead <= timeSamples && timeSamples < _RightOutputClipWriteHead + _EffectBufferLength)
                            || (timeSamples < _RightOutputClipWriteHead && timeSamples + _OutputClipFrames < _RightOutputClipWriteHead + _EffectBufferLength)
                        )
                       )
                    {
                        // ring buffer exhausted
                        StopEffect();
                        return;
                    }

                    Array.Copy(_ReadBuffer[1], readBeginIndex, _MonoWriteBuffer, 0, freshDataFrames);
                    _RightOutputClip.SetData(_MonoWriteBuffer, _RightOutputClipWriteHead);

                    _RightOutputClipWriteHead += freshDataFrames;

                    if (!_RightOut.isPlaying && _RightOutputClipWriteHead >= _EffectBufferLength)
                    {
                        _RightOut.Play();
                    }

                    if (_RightOutputClipWriteHead >= _OutputClipFrames)
                    {
                        _RightOutputClipWriteHead -= _OutputClipFrames;
                    }
                }
            }

            // stereo interleave
            if (_SubWooferOut)
            {
                if (!_SubWooferOut.gameObject.activeInHierarchy)
                {
                    _SubWooferOutputClipWriteHead = 0;
                }
                else
                {
                    var timeSamples = _SubWooferOut.timeSamples;
                    if (_SubWooferOut.isPlaying &&
                        (
                            (_SubWooferOutputClipWriteHead <= timeSamples && timeSamples < _SubWooferOutputClipWriteHead + _EffectBufferLength)
                            || (timeSamples < _SubWooferOutputClipWriteHead && timeSamples + _OutputClipFrames < _SubWooferOutputClipWriteHead + _EffectBufferLength)
                        )
                       )
                    {
                        // ring buffer exhausted
                        StopEffect();
                        return;
                    }

                    for (var frame = 0; frame < freshDataFrames; frame++)
                    {
                        _MonoWriteBuffer[frame] = _ReadBuffer[0][readBeginIndex + frame] + _ReadBuffer[1][readBeginIndex + frame];
                    }

                    _SubWooferOutputClip.SetData(_MonoWriteBuffer, _SubWooferOutputClipWriteHead);
                    _SubWooferOutputClipWriteHead += freshDataFrames;

                    if (!_SubWooferOut.isPlaying && _SubWooferOutputClipWriteHead >= _EffectBufferLength)
                    {
                        _SubWooferOut.Play();
                    }

                    if (_SubWooferOutputClipWriteHead >= _OutputClipFrames)
                    {
                        _SubWooferOutputClipWriteHead -= _OutputClipFrames;
                    }
                }
            }

        }
    }
}