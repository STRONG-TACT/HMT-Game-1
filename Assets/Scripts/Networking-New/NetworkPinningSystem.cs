using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkPinningSystem : MonoBehaviour
{
    public GameObject pinWheel;

    private void Start()
    {
        pinWheel.SetActive(false);
    }
}
