using JP.Notek.Udux;
using UnityEngine;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Data;
using Random = UnityEngine.Random;

namespace JP.Notek.Rocky
{
    [RequireComponent(typeof(VideoPlayerCurrentState))]
    [RequireComponent(typeof(VideoPlayerNewState))]
    public class VideoPlayerStore : ReduceStoreBase<VideoPlayer, VideoPlayerCurrentState, VideoPlayerNewState, VideoPlayerSyncState>
    {
        [SerializeField] bool _ShowDebugLog = false;
        protected override void Reset()
        {
            _CurrentState = GetComponent<VideoPlayerCurrentState>();
            NewState = GetComponent<VideoPlayerNewState>();
        }

        public void Start()
        {
            _SyncState.Subscribe(this);
        }

        public override void Update()
        {
            base.Update();

            if (_IsStateDistributing)
            {
                if (Views.Length <= _StateDistributingI)
                {
                    _CurrentState.UpdateState(NewState);
                    _IsStateDistributing = false;
                    _StateDistributingI = 0;
                }
                else
                {
                    Views[_StateDistributingI++].OnChange(_CurrentState, NewState);
                }
            }
        }

        public bool IsOwner()
        {
            return _SyncState.IsOwner;
        }

        public override void SubscribeOnChange(IStoreObservable<VideoPlayer> view)
        {
            Views = Views.Add(view);
        }

        public override void Reduce(string action, DataToken value)
        {
            if (_ShowDebugLog)
            {
                DataToken json;
                if (VRCJson.TrySerializeToJson(value, JsonExportType.Beautify, out json))
                    Debug.LogError($"{action}: {(string)json}");
                else
                    Debug.LogError($"{action}");
            }

            switch (action)
            {
                case _OnSyncStateChangedAction:
                    NewState.ReflectSyncState(_SyncState);
                    break;
                case _OnOwnershipTransferredAction:
                    _SyncState.SetOwnerTimeDifference();
                    NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                    break;
                case "UI.OnOwnershipRequest":
                    _SyncState.TakeOwnership();
                    _SyncState.SetOwnerTimeDifference();
                    var seek = (float)((DataDictionary)value)["seek"];
                    NewState.VideoPlayingSyncTime = Time.time;
                    NewState.VideoPlayingLastSyncedSeekTime = NewState.VideoInfoDurationProvided ? seek * NewState.VideoInfoDuration : 0;
                    _SyncState.ReflectLocalState(NewState);
                    NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                    break;
                case "UI.OnPowerOnButtonClicked":
                    NewState.Active = true;
                    break;
                case "UI.OnPowerOffButtonClicked":
                    NewState.Active = false;
                    break;
                case "UI.OnVolumeSliderChanged":
                    var volume = (float)((DataDictionary)value)["volume"];
                    NewState.AudioVolume = volume;
                    break;
                case "UI.OnSubWooferLevelSliderChanged":
                    var subWooferLevel = (float)((DataDictionary)value)["subWooferLevel"];
                    NewState.SubWooferRelativeVolume = subWooferLevel;
                    break;
                case "UI.OnVideoSeekSliderChanged":
                    if (!_SyncState.IsOwner)
                        return;

                    if (!NewState.VideoInfoDurationProvided || NewState.VideoInfoDuration <= 0)
                        return;

                    var _seek = (float)((DataDictionary)value)["seek"];
                    NewState.VideoPlayingSyncTime = Time.time;
                    NewState.VideoPlayingLastSyncedSeekTime = NewState.VideoInfoDurationProvided ? _seek * NewState.VideoInfoDuration : 0;
                    _SyncState.ReflectLocalState(NewState);
                    NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                    break;

                case "UI.OnPlayButtonClicked":
                    if (!_SyncState.IsOwner)
                        return;

                    var _time = Time.time;

                    NewState.VideoPaused = !_CurrentState.VideoPaused;
                    switch (NewState.VideoPlayingStatus)
                    {
                        case VideoPlayingStatusType.Play:
                            if (!NewState.VideoPaused)
                                return;
                            NewState.VideoPlayingStatus = VideoPlayingStatusType.Suspend;
                            NewState.VideoPlayingSyncTime = _time;
                            NewState.VideoPlayingLastSyncedSeekTime = NewState.VideoPlayingSyncTime - _CurrentState.VideoPlayingSyncTime + _CurrentState.VideoPlayingLastSyncedSeekTime;
                            _SyncState.ReflectLocalState(NewState);
                            NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                            break;
                        case VideoPlayingStatusType.Suspend:
                            if (NewState.VideoPaused)
                                return;
                            NewState.VideoPlayingStatus = VideoPlayingStatusType.Play;
                            NewState.VideoPlayingSyncTime = _time;
                            _SyncState.ReflectLocalState(NewState);
                            NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                            break;
                        default:
                            return;
                    }
                    break;

                case "VideoProxy.OnActivateReceived":
                    if (_SyncState.IsOwner)
                    {
                        var startIndex = (int)((DataDictionary)value)["startIndex"];
                        var endIndex = (int)((DataDictionary)value)["endIndex"];

                        NewState.VideoPlayingId = Random.value;
                        NewState.VideoPlayingSyncTime = Time.time;
                        NewState.VideoPlayingStatus = VideoPlayingStatusType.Suspend;
                        NewState.VideoPlayingIndex = startIndex;
                        NewState.VideoPlayingLastSyncedSeekTime = 0f;
                        NewState.VideoInfoDuration = 0f;

                        NewState.VideoInfoStartIndex = startIndex;
                        NewState.VideoInfoEndIndex = endIndex;
                        _SyncState.ReflectLocalState(NewState);
                        NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                    }
                    break;
                case "VideoProxy.OnActivateError":
                    break;
                case "VideoProxy.OnLoadVideo":
                    NewState.VideoInfoDuration = 0f;
                    NewState.VideoInfoDurationProvided = false;
                    break;

                case "VideoPlayer.OnVideoReady":
                    var duration = (float)((DataDictionary)value)["duration"];
                    NewState.VideoInfoDuration = duration;
                    NewState.VideoInfoDurationProvided = true;
                    if (_SyncState.IsOwner)
                    {
                        NewState.VideoPlayingSyncTime = Time.time;
                        NewState.VideoPlayingLastSyncedSeekTime = 0f;
                        if (!_CurrentState.VideoPaused)
                            NewState.VideoPlayingStatus = VideoPlayingStatusType.Play;
                        else
                            NewState.VideoPlayingStatus = VideoPlayingStatusType.Suspend;
                        _SyncState.ReflectLocalState(NewState);
                        NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                    }
                    break;
                case "VideoPlayer.OnVideoEnd":
                    if (_SyncState.IsOwner)
                    {
                        NewState.VideoPlayingId = Random.value;
                        NewState.VideoPlayingSyncTime = Time.time;
                        NewState.VideoPlayingStatus = VideoPlayingStatusType.Suspend;
                        NewState.VideoPlayingIndex = NewState.VideoPlayingIndex >= NewState.VideoInfoEndIndex ? NewState.VideoInfoStartIndex : NewState.VideoPlayingIndex + 1;
                        NewState.VideoPlayingLastSyncedSeekTime = 0f;
                        _SyncState.ReflectLocalState(NewState);
                        NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                    }
                    NewState.VideoInfoDuration = 0f;
                    NewState.VideoInfoDurationProvided = false;
                    break;
                case "VideoPlayer.OnVideoError":
                    if (_SyncState.IsOwner)
                    {
                        var videoError = (VideoError)(int)((DataDictionary)value)["videoError"];
                        switch (videoError)
                        {
                            case VideoError.RateLimited:
                                break;
                            case VideoError.AccessDenied:
                                break;
                            case VideoError.PlayerError:
                            case VideoError.InvalidURL:
                            case VideoError.Unknown:
                                NewState.VideoPlayingId = Random.value;
                                NewState.VideoPlayingSyncTime = Time.time;
                                NewState.VideoPlayingStatus = VideoPlayingStatusType.Suspend;
                                NewState.VideoPlayingIndex = NewState.VideoPlayingIndex >= NewState.VideoInfoEndIndex ? NewState.VideoInfoStartIndex : NewState.VideoPlayingIndex + 1;
                                NewState.VideoPlayingLastSyncedSeekTime = 0f;
                                _SyncState.ReflectLocalState(NewState);
                                NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                                break;
                        }
                    }
                    NewState.VideoInfoDuration = 0f;
                    NewState.VideoInfoDurationProvided = false;
                    break;
                case "VideoPlayer.ResponseTooLate":
                    NewState.VideoInfoDuration = 0f;
                    NewState.VideoInfoDurationProvided = false;
                    if (_SyncState.IsOwner)
                    {
                        NewState.VideoPlayingSyncTime = Time.time;
                        NewState.VideoPlayingStatus = VideoPlayingStatusType.Suspend;
                        NewState.VideoPlayingLastSyncedSeekTime = 0f;
                        _SyncState.ReflectLocalState(NewState);
                        NewState.OwnerTimeDifference = _SyncState.RequestSerialization();
                        break;
                    }
                    break;
                default:
                    return;
            }
        }
    }
}