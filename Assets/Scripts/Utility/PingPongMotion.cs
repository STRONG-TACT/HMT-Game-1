using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingPongMotion : MonoBehaviour
{
    [SerializeField]
    float maxHeight;

    [SerializeField]
    float motionSpeed;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + Mathf.PingPong(Time.time * motionSpeed, maxHeight), transform.localPosition.z);
    }
}
