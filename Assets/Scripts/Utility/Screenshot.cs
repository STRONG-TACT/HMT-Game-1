using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Backspace))
            ScreenCapture.CaptureScreenshot("Screenshots/MultiplayerMode/L3/TempShot" + Time.time + ".png", 2);
    }

    private void OnMouseDown()
    {
        
    }
}
