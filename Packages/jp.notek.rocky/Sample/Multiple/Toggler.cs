
using JP.Notek.Rocky;
using UdonSharp;
using UnityEngine;

public class Toggler : UdonSharpBehaviour
{
    public PlayerObject ObjectA;
    public PlayerObject ObjectB;
    bool _State = false;

    public void OnClick()
    {
        _State = !_State;
        if (_State)
        {
            ObjectA.SetActive(false);
            ObjectB.SetActive(true);
        }
        else
        {
            ObjectB.SetActive(false);
            ObjectA.SetActive(true);
        }
    }
}
