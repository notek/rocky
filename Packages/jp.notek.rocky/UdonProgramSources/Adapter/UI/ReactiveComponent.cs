using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.Udon;

namespace JP.Notek.Rocky
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class ReactiveComponent : UdonSharpBehaviour
    {
        [SerializeField] UdonBehaviour _EventReceiver;
        [SerializeField] string _OnClickCallbackFunctionName;

        [SerializeField] Image[] _TargetImages;
        [SerializeField] Sprite[] _DefaultSprites;
        [SerializeField] Sprite[] _PressedSprites;
        [SerializeField] AudioClip _ClickAudioClip;
        [SerializeField] AudioSource _UIAudioSource;

        int _SpriteLength = 0;
        bool _PointerPressed = false;
        void Start()
        {
            _SpriteLength = Math.Min(_TargetImages.Length, Math.Min(_DefaultSprites.Length, _PressedSprites.Length));
        }
        public void OnPointerDown()
        {
            _PointerPressed = true;
            for (int i = 0; i < _SpriteLength; i++)
            {
                _TargetImages[i].sprite = _PressedSprites[i];
            }
        }
        public void OnPointerUp()
        {
            if (_PointerPressed)
            {
                _PointerPressed = false;
                for (int i = 0; i < _SpriteLength; i++)
                {
                    _TargetImages[i].sprite = _DefaultSprites[i];
                }
                _UIAudioSource.PlayOneShot(_ClickAudioClip);
                if (_EventReceiver != null)
                    _EventReceiver.SendCustomEvent(_OnClickCallbackFunctionName);
            }
        }
    }
}
