
using JP.Notek.Udux;
using VRC.SDK3.Data;
using VRC.SDKBase;

namespace JP.Notek.Rocky.VideoProxyAction
{
    public static class ActionCreator
    {
        public static void SendOnActivateReceived(this Dispatcher dispatcher, int startIndex, int endIndex)
        {
            var d = new DataDictionary();
            d["startIndex"] = startIndex;
            d["endIndex"] = endIndex;
            dispatcher.Dispatch("VideoProxy.OnActivateReceived", d);
        }
        public static void SendOnActivateError(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("VideoProxy.OnActivateError");
        }
        public static void SendOnLoadVideo(this Dispatcher dispatcher)
        {
            dispatcher.Dispatch("VideoProxy.OnLoadVideo");
        }
    }
}