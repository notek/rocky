using JP.Notek.Udux;

namespace JP.Notek.Rocky
{
    public enum VideoPlayingStatusType
    {
        Terminate,
        Play,
        Suspend
    }
    public class VideoPlayer : IModel<VideoPlayerSyncState>
    {
        public bool Active = false;

        // Video Playing State
        public float VideoPlayingId = -1;
        public float VideoPlayingSyncTime = -1;
        public VideoPlayingStatusType VideoPlayingStatus = VideoPlayingStatusType.Terminate;
        public int VideoPlayingIndex = -1;
        public float VideoPlayingLastSyncedSeekTime = -1;

        public int VideoInfoStartIndex = -1;
        public int VideoInfoEndIndex = -1;
        public float VideoInfoDuration = 0;
        public bool VideoInfoDurationProvided = false;
        public bool VideoPaused = false;

        // Sync state owner time difference
        public float OwnerTimeDifference = float.PositiveInfinity;

        // Speaker
        public float AudioVolume = 0.5f;
        public bool AudioMuted = false;
        public float SubWooferRelativeVolume = 0.5f;
        public bool BypassAudioEffect = false;



        public void UpdateState(VideoPlayer state)
        {
            this.Active = state.Active;

            this.VideoPlayingId = state.VideoPlayingId;
            this.VideoPlayingSyncTime = state.VideoPlayingSyncTime;
            this.VideoPlayingStatus = state.VideoPlayingStatus;
            this.VideoPlayingIndex = state.VideoPlayingIndex;
            this.VideoPlayingLastSyncedSeekTime = state.VideoPlayingLastSyncedSeekTime;

            this.VideoInfoStartIndex = state.VideoInfoStartIndex;
            this.VideoInfoEndIndex = state.VideoInfoEndIndex;
            this.VideoInfoDuration = state.VideoInfoDuration;
            this.VideoInfoDurationProvided = state.VideoInfoDurationProvided;

            this.VideoPaused = state.VideoPaused;

            this.OwnerTimeDifference = state.OwnerTimeDifference;

            this.AudioVolume = state.AudioVolume;
            this.AudioMuted = state.AudioMuted;
            this.SubWooferRelativeVolume = state.SubWooferRelativeVolume;
            this.BypassAudioEffect = state.BypassAudioEffect;
        }
    }
}