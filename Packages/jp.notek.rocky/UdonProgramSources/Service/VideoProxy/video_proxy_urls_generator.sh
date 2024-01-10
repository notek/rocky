#!/bin/bash

for i in `seq 0 999`
do
    video_urls+="new VRCUrl(\"https://api.rocky.nohto.net/playlist/$i/video-proxy\"),
"
done

for i in `seq 0 999`
do
    title_urls+="new VRCUrl(\"https://api.rocky.nohto.net/playlist/$i/title\"),
"
done

for i in `seq 0 999`
do
    thumb_urls+="new VRCUrl(\"https://api.rocky.nohto.net/playlist/$i/thumbnail-proxy\"),
"
done

cat << EOF > VideoProxyURLs.cs
using UdonSharp;
using VRC.SDKBase;
namespace JP.Notek.Rocky
{
    public class VideoProxyURLs : UdonSharpBehaviour
    {
        public string ActivateUrlBase = "https://api.rocky.nohto.net/activate/;";
        public VRCUrl ActivateUrl = new VRCUrl("https://api.rocky.nohto.net/activate/;\n　▽▽▽▽消さずに下に貼り付けてね！▽▽▽▽\r\n");
        public VRCUrl[] VideoUrls = new VRCUrl[] {
$video_urls
            };
        public VRCUrl[] TitleUrls = new VRCUrl[] {
$title_urls
            };
        public VRCUrl[] ThumbnailUrls = new VRCUrl[] {
$thumb_urls
            };
    }
}
EOF