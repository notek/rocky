
using JP.Notek.Udux;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Data;

namespace JP.Notek.Rocky.VideoPlayerAction
{
    public static class ActionCreator
    {
        public static void SendOnVideoError(this Dispatcher dispatcher, VideoError videoError)
        {
            var d = new DataDictionary();
            d["videoError"] = (int)videoError;
            dispatcher.Dispatch("VideoPlayer.OnVideoError", d);
        }
        public static void SendOnVideoReady(this Dispatcher dispatcher, float duration)
        {
            var d = new DataDictionary();
            d["duration"] = (float)duration;
            dispatcher.Dispatch("VideoPlayer.OnVideoReady", d);
        }
        public static void SendOnVideoEnd(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("VideoPlayer.OnVideoEnd");
        }

        public static void SendResponseTooLate(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("VideoPlayer.ResponseTooLate");
        }
    }
}