namespace JP.Notek.Rocky
{
    public class VideoPlayerNewState : VideoPlayer
    {
        public override void ReflectSyncState(VideoPlayerSyncState state)
        {
            this.VideoPlayingId = state.VideoPlayingId;
            this.VideoPlayingSyncTime = state.VideoPlayingSyncTime;
            this.VideoPlayingStatus = state.VideoPlayingStatus;
            this.VideoPlayingIndex = state.VideoPlayingIndex;
            this.VideoPlayingLastSyncedSeekTime = state.VideoPlayingLastSyncedSeekTime;

            this.VideoInfoStartIndex = state.VideoInfoStartIndex;
            this.VideoInfoEndIndex = state.VideoInfoEndIndex;

            this.OwnerTimeDifference = state.OwnerTimeDifference;
        }
    }
}