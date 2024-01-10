using UdonSharp;
using UnityEngine;
using JP.Notek.Udux;
using VRC.SDK3.Video.Components.AVPro;
using VRC.SDK3.Components.Video;
using JP.Notek.Rocky.VideoPlayerAction;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace JP.Notek.Rocky
{
    [RequireComponent(typeof(VRCAVProVideoPlayer))]
    public class VideoPlayerProvider : IStoreObservable<VideoPlayer>
    {
        [SerializeField] VideoPlayerStore _Store;
        [SerializeField] Dispatcher _Dispatcher;
        [SerializeField] VideoProxyService _VideoProxyService;
        [SerializeField] VRCAVProVideoPlayer _VRCAVProVideoPlayer;
        [SerializeField] float _TooLateThreshold = 8;
        [SerializeField] float _TooLateErrorThreshold = 12;
        // [SerializeField] VRCUnityVideoPlayer _VRCUnityVideoPlayer;
        BaseVRCVideoPlayer _VideoPlayer;
        float _VideoRequestedId = -1;
        float _RetryTime = -1;
        float _RetryWithDuration = -1;
        float _RequestTime = -1;
        bool _TooLateSent = false;

        void Reset()
        {
            _VRCAVProVideoPlayer = GetComponent<VRCAVProVideoPlayer>();
        }
        void Start()
        {
            _VideoPlayer = _VRCAVProVideoPlayer;

            _Store.SubscribeOnChange(this);
            OnChange(null, _Store.NewState);
        }

        void Update()
        {
            if (
                _RetryWithDuration != -1
                && (Time.frameCount & 0x3F) == 0
                && _RetryTime + _RetryWithDuration < Time.time
            )
            {
                _RetryWithDuration = -1;
                RequestNewVideo(_Store.NewState.VideoPlayingIndex);
            }
            if (
                _RequestTime != -1
                && (Time.frameCount & 0xF) == 0
            )
            {
                if(!_TooLateSent && _RequestTime + _TooLateThreshold < Time.time)
                {
                    _TooLateSent = true;
                    _Dispatcher.SendResponseTooLate();
                }
                if(_RequestTime + _TooLateErrorThreshold < Time.time)
                {
                    _Dispatcher.SendOnVideoError(VideoError.Unknown);
                    ResetTooLateCount();
                }
            }
        }

        public override void OnChange(VideoPlayer currentState, VideoPlayer newState)
        {
            if (currentState == null || currentState.Active != newState.Active)
            {
                if (!newState.Active)
                {
                    if (_VideoPlayer.IsPlaying)
                        _VideoPlayer.Stop();
                    _VideoRequestedId = -1;
                    _RetryWithDuration = -1;
                }
            }
            if (!newState.Active)
                return;

            switch (newState.VideoPlayingStatus)
            {
                case VideoPlayingStatusType.Terminate:
                    if (_VideoPlayer.IsPlaying)
                        _VideoPlayer.Stop();
                    break;
                case VideoPlayingStatusType.Play:
                    if (newState.VideoPlayingId != _VideoRequestedId)
                    {
                        _VideoRequestedId = newState.VideoPlayingId;
                        RequestNewVideo(newState.VideoPlayingIndex);
                    }

                    if (_VideoPlayer.IsReady)
                    {
                        if (Seekable(_Store.NewState.VideoInfoDurationProvided, _Store.NewState.VideoInfoDuration))
                        {
                            var seekTime = GetSyncSeekTime(VideoPlayingStatusType.Play, newState.OwnerTimeDifference, newState.VideoPlayingSyncTime, newState.VideoPlayingLastSyncedSeekTime, _Store.NewState.VideoInfoDuration);

                            if (Mathf.Abs(_VideoPlayer.GetTime() - seekTime) > (_Store.IsOwner() ? 0.1f : 3f))
                            {
                                _VideoPlayer.SetTime(seekTime);
                            }
                        }
                        _VideoPlayer.Play();
                    }
                    break;
                case VideoPlayingStatusType.Suspend:
                    if (newState.VideoPlayingId != _VideoRequestedId)
                    {
                        _VideoRequestedId = newState.VideoPlayingId;
                        RequestNewVideo(newState.VideoPlayingIndex);
                    }

                    if (_VideoPlayer.IsReady)
                    {
                        if(Seekable(_Store.NewState.VideoInfoDurationProvided, _Store.NewState.VideoInfoDuration))
                        {
                            var seekTime = GetSyncSeekTime(VideoPlayingStatusType.Suspend, newState.OwnerTimeDifference, newState.VideoPlayingSyncTime, newState.VideoPlayingLastSyncedSeekTime, _Store.NewState.VideoInfoDuration);
                            if (_VideoPlayer.GetTime() != newState.VideoPlayingLastSyncedSeekTime)
                                _VideoPlayer.SetTime(seekTime);
                        }
                        _VideoPlayer.Pause();
                    }
                    break;
            }
        }

        public override void OnVideoReady()
        {
            ResetTooLateCount();
            if (!_Store.NewState.Active)
                return;

            _Dispatcher.SendOnVideoReady(_VideoPlayer.GetDuration());
        }
        public override void OnVideoEnd()
        {
            if (!_Store.NewState.Active)
                return;

            _Dispatcher.SendOnVideoEnd();
        }
        public override void OnVideoError(VideoError videoError)
        {
            ResetTooLateCount();
            if (!_Store.NewState.Active)
                return;

            switch (videoError)
            {
                case VideoError.RateLimited:
                    _RetryWithDuration = 5f;
                    _RetryTime = Time.time;
                    break;
                case VideoError.AccessDenied:
                    _RetryWithDuration = 10f;
                    _RetryTime = Time.time;
                    break;
                case VideoError.PlayerError:
                    break;
                case VideoError.InvalidURL:
                    break;
                case VideoError.Unknown:
                    break;
            }
            _Dispatcher.SendOnVideoError(videoError);
        }

        void RequestNewVideo(int index)
        {
            SetTooLateCount();
            _VideoProxyService.RequestVideo(_VideoPlayer, index);
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

        bool Seekable(bool isProvided, float duration)
        {
            return isProvided && duration > 0 && !float.IsInfinity(duration);
        }

        void SetTooLateCount()
        {
            _TooLateSent = false;
            _RequestTime = Time.time;
        }

        void ResetTooLateCount()
        {
            _TooLateSent = false;
            _RequestTime = -1;
        }
    }
}