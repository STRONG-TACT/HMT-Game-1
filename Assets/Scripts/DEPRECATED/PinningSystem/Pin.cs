using UnityEngine;


#if DEPRECATED
public class Pin : MonoBehaviour
{
    public int pinObjId; // 3D
    public int pinUIId;  //2D

    public void pinIdSetup( int _pinObjId, int _pinUIId)
    {
        pinObjId = _pinObjId;
        pinUIId = _pinUIId;
    }
}
#endif