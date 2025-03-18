using System.Linq;
using UnityEngine;

public class Screenshoter : MonoBehaviour {

    public string path = "Screenshots/MultiplayerMode/L3/";
    public int superSizeScale = 2;

    public KeyCode[] screenshotKeys = new KeyCode[] { KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.J };

    // Start is called before the first frame update
    void Start() {
#if UNITY_EDITOR
        DontDestroyOnLoad(this);
#else
        Destroy(this);
#endif
    }

    // Update is called once per frame
    void Update() {
        if (KeyCodeCheck()) {
            ScreenCapture.CaptureScreenshot(path + "screenshot-" + System.DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm-ss-ffff") + ".png", superSizeScale);
        }
    }

    private bool KeyCodeCheck() {
        int keysPressed = screenshotKeys.Where(key => Input.GetKey(key)).Count();
        int keysDown = screenshotKeys.Where(key => Input.GetKeyDown(key)).Count();
        return keysPressed == screenshotKeys.Length && keysDown > 0;
    }

}
