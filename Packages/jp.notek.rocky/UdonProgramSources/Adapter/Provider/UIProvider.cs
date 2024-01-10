using UnityEngine;
using JP.Notek.Udux;
using JP.Notek.Rocky.UIAction;
using TMPro;
using UnityEngine.UI;
using VRC.SDK3.Components;
using VRC.SDK3.StringLoading;
using System;
using VRC.Udon.Common.Interfaces;

namespace JP.Notek.Rocky
{
    public class UIProvider : IStoreObservable<VideoPlayer>
    {
        [SerializeField] VideoPlayerStore _Store;
        [SerializeField] Dispatcher _Dispatcher;
        [SerializeField] AudioClip _ClickAudioClip;
        [SerializeField] AudioClip _CursorAudioClip;
        [SerializeField] AudioClip _EnterAudioClip;
        [SerializeField] AudioSource _UIAudioSource;
        [SerializeField] float _GlobalMenuOpenTimeThreshold = 1f;
        [SerializeField] float _ShowBackgroundTimeoutThreshould = 3f;
        [SerializeField] float _ShowTitleTimeoutThreshould = 6f;
        [SerializeField] GameObject _TitleHeader;
        [SerializeField] GameObject _MenuBackground;
        [SerializeField] GameObject _LocalMenu;
        [SerializeField] GameObject _GlobalMenu;
        [SerializeField] GameObject _InfoText;
        [SerializeField] GameObject _TakeOwnershipButton;
        [SerializeField] GameObject _PowerOnButton;
        [SerializeField] TextMeshProUGUI _TitleText;
        [SerializeField] TextMeshProUGUI _VideoSeekTimeText;
        [SerializeField] TextMeshProUGUI _AudioVolumeLabelText;
        [SerializeField] TextMeshProUGUI _AudioSubWooferLevelLabelText;
        [SerializeField] Slider _AudioVolumeSlider;
        [SerializeField] Slider _AudioSubWooferLevelSlider;
        [SerializeField] Slider _VideoSeekSlider;
        [SerializeField] VRCUrlInputField _UrlInputButton;
        [SerializeField] VideoProxyService _VideoProxyService;


        bool _PointerPressed = false;
        float _PointerPressedTime = 0f;
        float _VideoPlayingId = 0f;
        string _Title;
        float _ShowTitleTimeout = -1f;
        float _ShowBackgroundTimeout = -1f;
        bool _SeekChanging = false;
        bool _AudioVolumeSliderChanging = false;
        bool _AudioSubWooferSliderChanging = false;
        bool _GuideShowing = false;
        bool _OwnerControlActive = false;

        void Start()
        {
            _Store.SubscribeOnChange(this);
            OnChange(null, _Store.NewState);
        }

        public override void OnChange(VideoPlayer currentState, VideoPlayer newState)
        {
            if (_VideoPlayingId != newState.VideoPlayingId)
            {
                _VideoPlayingId = newState.VideoPlayingId;
                if (newState.VideoPlayingIndex >= 0)
                {
                    _VideoProxyService.RequestTitle(newState.VideoPlayingIndex, (IUdonEventReceiver)this);
                }
                _VideoSeekSlider.enabled = false;
            }
            _VideoSeekSlider.enabled = newState.VideoInfoDurationProvided && !float.IsNaN(newState.VideoInfoDuration) && !IsLive(newState.VideoInfoDuration);
        }

        private void Update()
        {
            if (_PointerPressed)
            {
                _ShowBackgroundTimeout = Time.time;
                if (_PointerPressedTime + _GlobalMenuOpenTimeThreshold < Time.time)
                {
                    _PointerPressed = false;
                    _PointerPressedTime = 0f;
                    _GlobalMenu.SetActive(true);
                    _UrlInputButton.SetUrl(_VideoProxyService.ActivateUrl);
                    _UIAudioSource.PlayOneShot(_EnterAudioClip);
                    if (!_Store.IsOwner())
                    {
                        _TakeOwnershipButton.SetActive(true);
                    }
                    else
                    {
                        _OwnerControlActive = true;
                    }
                    _InfoText.SetActive(false);
                    _ShowBackgroundTimeout = -1f;
                }
            }
            if (_SeekChanging)
            {
                if (!_VideoSeekSlider.enabled)
                {
                    _SeekChanging = false;
                }
                else if (_Store.NewState.VideoInfoDurationProvided)
                {
                    var durationSeconds = _Store.NewState.VideoInfoDuration;
                    var currentSeconds = _VideoSeekSlider.value * _Store.NewState.VideoInfoDuration;
                    _VideoSeekTimeText.text = GetSeekTimeText(durationSeconds, currentSeconds);
                }
            }
            else if ((Time.frameCount & 0x7) == 0)
            {
                if (_Store.NewState.VideoInfoDurationProvided && !float.IsNaN(_Store.NewState.VideoInfoDuration) && !IsLive(_Store.NewState.VideoInfoDuration))
                {
                    var seekTime = GetSyncSeekTime(_Store.NewState.VideoPlayingStatus, _Store.NewState.OwnerTimeDifference, _Store.NewState.VideoPlayingSyncTime, _Store.NewState.VideoPlayingLastSyncedSeekTime, _Store.NewState.VideoInfoDuration);
                    _VideoSeekSlider.enabled = true;
                    _VideoSeekSlider.value = seekTime / _Store.NewState.VideoInfoDuration;
                }
                else
                {
                    _VideoSeekSlider.enabled = false;
                    _VideoSeekSlider.value = 0;
                }
            }

            if ((Time.frameCount & 0xF) == 0)
            {
                if (_Store.NewState.Active && _Store.NewState.VideoPlayingStatus != VideoPlayingStatusType.Terminate)
                {
                    if (_Store.NewState.VideoInfoDurationProvided)
                    {
                        var durationSeconds = _Store.NewState.VideoInfoDuration;
                        var currentSeconds = GetSyncSeekTime(_Store.NewState.VideoPlayingStatus, _Store.NewState.OwnerTimeDifference, _Store.NewState.VideoPlayingSyncTime, _Store.NewState.VideoPlayingLastSyncedSeekTime, _Store.NewState.VideoInfoDuration);
                        _VideoSeekTimeText.text = GetSeekTimeText(durationSeconds, currentSeconds);
                    }
                }
                else
                {
                    _VideoSeekTimeText.text = "";
                }

                if (_ShowBackgroundTimeout >= 0 && _ShowBackgroundTimeout + _ShowBackgroundTimeoutThreshould < Time.time)
                {
                    _MenuBackground.SetActive(false);
                    _InfoText.SetActive(false);
                }
                if (_Store.NewState.Active && !_MenuBackground.activeSelf && _TitleHeader.activeSelf && _ShowTitleTimeout + _ShowTitleTimeoutThreshould < Time.time)
                {
                    _TitleHeader.SetActive(false);
                }

                if (_AudioVolumeSliderChanging && Mathf.Abs(_Store.NewState.AudioVolume - _AudioVolumeSlider.value) > 0.05f)
                    _Dispatcher.SendOnVolumeSliderChanged(_AudioVolumeSlider.value);
                if (_AudioSubWooferSliderChanging && Mathf.Abs(_Store.NewState.SubWooferRelativeVolume - _AudioSubWooferLevelSlider.value) > 0.05f)
                    _Dispatcher.SendOnSubWooferLevelSliderChanged(_AudioSubWooferLevelSlider.value);


                if (_OwnerControlActive)
                {
                    if (!_Store.IsOwner())
                    {
                        _OwnerControlActive = false;
                        _TakeOwnershipButton.SetActive(true);
                    }
                }
            }
        }

        public void OnPowerOnButtonClicked()
        {
            _TitleHeader.SetActive(true);
            _MenuBackground.SetActive(true);
            _InfoText.SetActive(true);
            _ShowBackgroundTimeout = Time.time;
            ReflectTitleLabel(true, _Store.NewState.VideoPlayingStatus);

            _PowerOnButton.SetActive(false);

            _Dispatcher.SendOnPowerOnButtonClicked();
        }

        public void OnPowerOffButtonClicked()
        {
            _TitleHeader.SetActive(true);
            _ShowBackgroundTimeout = -1f;
            ReflectTitleLabel(false, _Store.NewState.VideoPlayingStatus);

            _MenuBackground.SetActive(false);

            _InfoText.SetActive(false);
            _GlobalMenu.SetActive(false);
            _LocalMenu.SetActive(false);

            _PowerOnButton.SetActive(true);

            _Dispatcher.SendOnPowerOffButtonClicked();
        }

        public void OnHideButtonClicked()
        {
            _OwnerControlActive = false;
            _InfoText.SetActive(true);
            _ShowBackgroundTimeout = Time.time;
            _GlobalMenu.SetActive(false);
            _LocalMenu.SetActive(false);
        }

        public void OnVolumeSliderChanged()
        {
            _AudioVolumeLabelText.text = $"音量：{Math.Round(_AudioVolumeSlider.value * 50)}";
        }
        public void OnVolumeSliderPointerDown()
        {
            _AudioVolumeSliderChanging = true;
        }
        public void OnVolumeSliderPointerUp()
        {
            _AudioVolumeSliderChanging = false;
            _Dispatcher.SendOnVolumeSliderChanged(_AudioVolumeSlider.value);
        }

        public void OnSubWooferSliderChanged()
        {
            _AudioSubWooferLevelLabelText.text = $"サブウーファLv：{Math.Round(_AudioSubWooferLevelSlider.value * 50)}";
        }
        public void OnSubWooferSliderPointerDown()
        {
            _AudioSubWooferSliderChanging = true;
        }
        public void OnSubWooferSliderPointerUp()
        {
            _AudioSubWooferSliderChanging = false;
            _Dispatcher.SendOnSubWooferLevelSliderChanged(_AudioSubWooferLevelSlider.value);
        }


        public void OnRequestOwnership()
        {
            _OwnerControlActive = true;
            _TakeOwnershipButton.SetActive(false);
            _Dispatcher.SendOnOwnershipRequest(_VideoSeekSlider.value);
        }

        public void OnVideoSeekSliderPointerDown()
        {
            if (!_VideoSeekSlider.enabled)
            {
                _SeekChanging = false;
                return;
            }
            _SeekChanging = true;
        }

        public void OnVideoSeekSliderPointerUp()
        {
            if (!_VideoSeekSlider.enabled)
            {
                _SeekChanging = false;
                return;
            }
            _SeekChanging = false;
            _Dispatcher.SendOnVideoSeekSliderChanged(_VideoSeekSlider.value);
        }

        public void OnPlayButtonClicked()
        {
            _Dispatcher.SendOnPlayButtonClicked();
        }

        public void OnOpenUrlValueChanged()
        {
            if (!_UrlInputButton.GetUrl().ToString().StartsWith(_VideoProxyService.ActivateUrlBase))
                _UrlInputButton.SetUrl(_VideoProxyService.ActivateUrl);
        }

        public void OnOpenUrlConfirmed()
        {
            _VideoProxyService.RequestVideoProxyActivate(_UrlInputButton.GetUrl());
            _UrlInputButton.SetUrl(_VideoProxyService.ActivateUrl);
        }

        public void OnBackgroundPointerUp()
        {
            if (!_Store.NewState.Active)
                return;
            if (_GuideShowing)
            {
                _GuideShowing = false;
                return;
            }
            if (_PointerPressed)
            {
                _PointerPressed = false;
                _PointerPressedTime = 0f;
                _UIAudioSource.PlayOneShot(_ClickAudioClip);
                _LocalMenu.SetActive(true);
                _InfoText.SetActive(false);
                _ShowBackgroundTimeout = -1f;
            }
        }

        public void OnBackgroundPointerDown()
        {
            if (!_Store.NewState.Active)
                return;
            if (!_MenuBackground.activeSelf)
            {
                _GuideShowing = true;
                _TitleHeader.SetActive(true);
                ReflectTitleLabel(_Store.NewState.Active, _Store.NewState.VideoPlayingStatus);
                _MenuBackground.SetActive(true);
                _InfoText.SetActive(true);
                _ShowBackgroundTimeout = Time.time;
                _UIAudioSource.PlayOneShot(_CursorAudioClip);
                return;
            }
            if (_LocalMenu.activeSelf || _GlobalMenu.activeSelf)
                return;
            if (_GuideShowing)
                return;
            if (!_PointerPressed)
            {
                _PointerPressed = true;
                _PointerPressedTime = Time.time;
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            if (_VideoProxyService.TitleUrls[_Store.NewState.VideoPlayingIndex] == result.Url)
            {
                SetTitleText(null);
                ReflectTitleLabel(_Store.NewState.Active, _Store.NewState.VideoPlayingStatus);

                _TitleHeader.SetActive(true);
                _ShowTitleTimeout = Time.time;
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            if (_VideoProxyService.TitleUrls[_Store.NewState.VideoPlayingIndex] == result.Url)
            {
                var title = result.Result
                .Replace("\r", "")
                .Replace("\n", "");
                SetTitleText(title);
                ReflectTitleLabel(_Store.NewState.Active, _Store.NewState.VideoPlayingStatus);

                _TitleHeader.SetActive(true);
                _ShowTitleTimeout = Time.time;
            }
        }

        void SetTitleText(string title)
        {
            _Title = title;
        }

        void ReflectTitleLabel(bool isActive, VideoPlayingStatusType status)
        {
            if (status == VideoPlayingStatusType.Terminate)
                _TitleText.text = "";
            else
            {

                if (isActive)
                    _TitleText.text = _Title ?? "No Title";
                else
                    _TitleText.text = $"再生中: {_Title ?? "No Title"}";
            }
        }

        static string GetFormattedTime(TimeSpan time)
        {
            return ((int)time.TotalMinutes).ToString() + time.ToString(@"\:ss");
        }

        static float GetSyncSeekTime(VideoPlayingStatusType status, float latency, float syncTime, float syncedSeekTime, float duration)
        {
            switch (status)
            {
                case VideoPlayingStatusType.Play:
                    var syncTimeLocal = syncTime + latency;
                    return Mathf.Clamp(Time.time - syncTimeLocal + syncedSeekTime, 0, duration);
                case VideoPlayingStatusType.Suspend:
                    return syncedSeekTime;
                default:
                    return 0;
            }
        }

        static bool IsLive(float duration)
        {
            return !float.IsNaN(duration) && duration <= 0 || float.IsInfinity(duration);
        }

        static string GetSeekTimeText(float durationSeconds, float currentSeconds)
        {
            if (IsLive(durationSeconds))
                return "LIVE";
            if (float.IsNaN(durationSeconds) || float.IsNaN(currentSeconds) || IsLive(currentSeconds))
                return "";
            var durationTimespan = TimeSpan.FromSeconds(durationSeconds);
            var currentTimespan = TimeSpan.FromSeconds(currentSeconds);
            return $"{GetFormattedTime(currentTimespan)} / {GetFormattedTime(durationTimespan)}";
        }
    }
}