using UnityEngine;
using System.Collections;

public class CursorEnabler : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown((Application.isEditor ? KeyCode.C : KeyCode.Escape)))
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
