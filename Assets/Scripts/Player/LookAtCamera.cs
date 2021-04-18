using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Transform cam;

    private void Update()
    {
        if (cam == null)
        {
            GameObject localPlayer = CustomNetworkManager.GetLocalPlayer();

            if(localPlayer != null)
            {
                cam = localPlayer.transform.GetChild(2).GetChild(0);
            }
        }
        else
        {
            transform.LookAt(cam);
        }
    }
}
