using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshotter : MonoBehaviour
{

    public string filePath = "C:/Users/eharpste/Desktop/Screenshots";
    public int scale = 2;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.S)) {
            string fileName = string.Format("{0}/screenshot_{1}.png", filePath, System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
            ScreenCapture.CaptureScreenshot(fileName, scale);
            Debug.LogFormat("Screenshot saved to {0}", fileName);
        }
        
    }
}
