using JP.Notek.Udux;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.SDK3.Video.Components.Base;
using VRC.SDK3.Image;
using VRC.Udon.Common.Interfaces;
using JP.Notek.Rocky.VideoProxyAction;

namespace JP.Notek.Rocky
{
    [RequireComponent(typeof(VideoProxyActivateReceiver))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VideoProxyService : VideoProxyURLs
    {
        [SerializeField] Dispatcher _Dispatcher;
        [SerializeField] VideoProxyActivateReceiver _ActivateReceiver;
        VRCImageDownloader _ImageDownloader;
        float _LastRequestedVideoTime = -1;
        int _LastRequestVideoIndex = -1;
        BaseVRCVideoPlayer _LastRequestVideoPlayer = null;

        void Reset()
        {
            _ActivateReceiver = GetComponent<VideoProxyActivateReceiver>();
        }

        public void Start()
        {
            _ActivateReceiver.Init(_Dispatcher, ActivateUrlBase);
        }
        public void Update()
        {
            if (
                _LastRequestVideoIndex != -1
                && _LastRequestVideoPlayer != null
                && (Time.frameCount & 0x3F) == 0
                && _LastRequestedVideoTime + 5f < Time.time
                )
            {
                _Dispatcher.SendOnLoadVideo();
                _LastRequestVideoPlayer.LoadURL(VideoUrls[_LastRequestVideoIndex]);
                _LastRequestedVideoTime = Time.time;
                _LastRequestVideoIndex = -1;
                _LastRequestVideoPlayer = null;
            }
        }

        public void RequestVideoProxyActivate(VRCUrl url)
        {
            _ActivateReceiver.Request(url);
        }

        public void RequestVideo(BaseVRCVideoPlayer player, int index)
        {
            _LastRequestVideoIndex = index;
            _LastRequestVideoPlayer = player;
        }
        public void RequestTitle(int index, IUdonEventReceiver receiver)
        {
            VRCStringDownloader.LoadUrl(TitleUrls[index], receiver);
        }
        public IVRCImageDownload RequestThumbnail(int index, Material material, IUdonEventReceiver receiver, TextureInfo textureInfo = null)
        {
            return _ImageDownloader.DownloadImage(ThumbnailUrls[index], material, receiver, textureInfo);
        }
    }
}