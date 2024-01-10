
using UdonSharp;
using UnityEngine;

namespace JP.Notek.Rocky
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class PlayerObject : UdonSharpBehaviour
    {
        [SerializeField] UIProvider _UIProvider;
        [SerializeField] Canvas _Canvas;
        [SerializeField] VideoScreenProvider _VideoScreenProvider;
        [SerializeField] GameObject _Screen;
        [SerializeField] SpeakerProvider _SpeakerProvider;
        [SerializeField] bool _UseLocalScale = false;
        public bool Active = false;
        public void Start()
        {
            SetActive(Active);
        }

        public void SetActive(bool active)
        {
            Active = active;
            if (active)
            {
                _UIProvider.transform.position = _Screen.transform.position;
                _UIProvider.transform.rotation = _Screen.transform.rotation;
                _UIProvider.transform.localScale = _UseLocalScale ? _Screen.transform.localScale : _Screen.transform.lossyScale;
                _SpeakerProvider.gameObject.SetActive(true);
                _Canvas.gameObject.SetActive(true);
                _VideoScreenProvider.SetScreenActive(true);
            }
            else
            {
                _SpeakerProvider.gameObject.SetActive(false);
                _Canvas.gameObject.SetActive(false);
                _VideoScreenProvider.SetScreenActive(false);
                _UIProvider.transform.position = Vector3.zero;
                _UIProvider.transform.rotation = Quaternion.identity;
                _UIProvider.transform.localScale = Vector3.one;
            }
        }
    }
}
