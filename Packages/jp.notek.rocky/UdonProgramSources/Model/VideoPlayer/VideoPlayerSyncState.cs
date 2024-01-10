using JP.Notek.Udux;
using UdonSharp;

namespace JP.Notek.Rocky
{
    public class VideoPlayerSyncState : SyncStateBase
    {

        [UdonSynced] public float VideoPlayingId = -1;
        [UdonSynced] public float VideoPlayingSyncTime = -1;
        [UdonSynced] public VideoPlayingStatusType VideoPlayingStatus = VideoPlayingStatusType.Terminate;
        [UdonSynced] public int VideoPlayingIndex = -1;
        [UdonSynced] public float VideoPlayingLastSyncedSeekTime = -1;

        [UdonSynced] public int VideoInfoStartIndex = -1;
        [UdonSynced] public int VideoInfoEndIndex = -1;

        [UdonSynced] public float OwnerTime = -1;


        public void ReflectLocalState(VideoPlayer state)
        {
            this.VideoPlayingId = state.VideoPlayingId;
            this.VideoPlayingSyncTime = state.VideoPlayingSyncTime;
            this.VideoPlayingStatus = state.VideoPlayingStatus;
            this.VideoPlayingIndex = state.VideoPlayingIndex;
            this.VideoPlayingLastSyncedSeekTime = state.VideoPlayingLastSyncedSeekTime;

            this.VideoInfoStartIndex = state.VideoInfoStartIndex;
            this.VideoInfoEndIndex = state.VideoInfoEndIndex;
        }
    }
}
