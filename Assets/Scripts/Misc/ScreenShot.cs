using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShot : MonoBehaviour
{
    public KeyCode srcKey = KeyCode.P;
    [Range(1, 16)]
    public int superSize = 1;

    private void Update()
    {
        //only in editor, to assets/screenshots
        if (Application.isEditor && Input.GetKeyDown(srcKey))
        {
            ScreenCapture.CaptureScreenshot(Application.dataPath + "/Screenshots/" + $@"{System.Guid.NewGuid()}.png", superSize);
        }
    }
}
