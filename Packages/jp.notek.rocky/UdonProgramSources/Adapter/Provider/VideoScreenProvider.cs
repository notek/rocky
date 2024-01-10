using UnityEngine;
using JP.Notek.Udux;

namespace JP.Notek.Rocky
{
    public class VideoScreenProvider : IStoreObservable<VideoPlayer>
    {
        [SerializeField] VideoPlayerStore _Store;
        [SerializeField] Renderer _TargetRenderer;
        [SerializeField] int _MaterialIndex = 0;
        [SerializeField] Texture _StandbyTexture;
        [SerializeField] bool _UseSharedMaterial = true;
        [SerializeField] string _ShaderMainTextureName = "_EmissionMap";
        [SerializeField] Renderer _VideoScreenFetchRenderer;
        // [SerializeField] string _AvProToggleParam = "_IsAVProInput";
        bool _PlayerActive = false;
        bool _ScreenActive = false;
        Material _FetchMaterial;
        Texture _LastRenderTexture;

        public override void OnChange(VideoPlayer currentState, VideoPlayer newState)
        {
            _PlayerActive = newState.Active;
            SetVideoScreenActive();
        }

        public void SetScreenActive(bool active)
        {
            _ScreenActive = active;
            SetVideoScreenActive();
        }

        void SetVideoScreenActive()
        {
            var newActive = _PlayerActive && _ScreenActive;
            if (newActive)
            {
                SetTextureActive();
            }
            else
            {
                SetTextureStandby();
                RendererExtensions.UpdateGIMaterials(_TargetRenderer);
            }
        }

        void Reset()
        {
            _TargetRenderer = GetComponent<Renderer>();
        }

        void Start()
        {
            _FetchMaterial = _VideoScreenFetchRenderer.material;
            _Store.SubscribeOnChange(this);
            OnChange(null, _Store.NewState);
        }

        void Update()
        {
            if (_PlayerActive && _ScreenActive)
            {
                if((Time.frameCount & 0xF) == 0 && _Store.NewState.VideoPlayingStatus != VideoPlayingStatusType.Terminate && _LastRenderTexture == null)
                    SetTextureActive();
                RendererExtensions.UpdateGIMaterials(_TargetRenderer);
            }
        }


        void SetTextureActive()
        {
            var texture = GetTexture();
            if (texture == _LastRenderTexture)
                return;

            if (_TargetRenderer)
            {
                Material rendererMat = _UseSharedMaterial ? _TargetRenderer.sharedMaterials[_MaterialIndex] : _TargetRenderer.materials[_MaterialIndex];

                if (texture != null)
                {
                    rendererMat.SetTexture(_ShaderMainTextureName, texture);
                }
                else
                {
                    rendererMat.SetTexture(_ShaderMainTextureName, _StandbyTexture);
                }
            }

            _LastRenderTexture = texture;
        }

        void SetTextureStandby()
        {
            Material rendererMat = _UseSharedMaterial ? _TargetRenderer.sharedMaterials[_MaterialIndex] : _TargetRenderer.materials[_MaterialIndex];
            rendererMat.SetTexture(_ShaderMainTextureName, _StandbyTexture);
            _LastRenderTexture = null;
        }

        Texture GetTexture()
        {
            return _FetchMaterial.GetTexture("_MainTex");
        }
    }
}