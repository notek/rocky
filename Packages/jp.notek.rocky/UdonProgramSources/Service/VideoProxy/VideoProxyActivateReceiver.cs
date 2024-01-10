using JP.Notek.Udux;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Data;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;
using JP.Notek.Rocky.VideoProxyAction;
using VRC.Udon.Common.Interfaces;

namespace JP.Notek.Rocky
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoProxyActivateReceiver : UdonSharpBehaviour
    {
        Dispatcher _Dispatcher;
        string _UrlBase;
        VRCUrl _LastRequestUrl = null;
        bool _ResponseWaiting = false;
        public void Init(Dispatcher dispatcher, string activateUrlBase)
        {
            _Dispatcher = dispatcher;
            _UrlBase = activateUrlBase;
        }

        public void Update()
        {
            if (!_ResponseWaiting && _LastRequestUrl != null)
            {
                _ResponseWaiting = true;
                VRCStringDownloader.LoadUrl(_LastRequestUrl, (IUdonEventReceiver)this);
            }
        }
        public void Request(VRCUrl url)
        {
            if (url.ToString().StartsWith(_UrlBase))
                _LastRequestUrl = url;
        }
        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            if(_LastRequestUrl == null || _LastRequestUrl.ToString() != result.Url.ToString())
              return;
            _LastRequestUrl = null;
            DataToken data;
            VRCJson.TryDeserializeFromJson(result.Result, out data);

            var startToken = ((DataDictionary)data)["start"];
            var endToken = ((DataDictionary)data)["end"];
            var statusToken = ((DataDictionary)data)["status"];
            if ((string)statusToken == "OK")
            {
                var start = (int)(double)startToken;
                var end = (int)(double)endToken;
                _Dispatcher.SendOnActivateReceived(start, end);
            }
            else
            {
                Debug.LogError("OpenUrl InternalServerError");
                _Dispatcher.SendOnActivateError();
            }
            _ResponseWaiting = false;
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            if(_LastRequestUrl == null || _LastRequestUrl.ToString() != result.Url.ToString())
              return;
            _LastRequestUrl = null;
            Debug.LogError(result.Error);
            _Dispatcher.SendOnActivateError();
            _ResponseWaiting = false;
        }


    }
}