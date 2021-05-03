using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    public Transform cam;

    private void FixedUpdate()
    {
        if (cam == null)
        {
            NetworkPlayer localPlayer = CustomNetworkManager.GetLocalPlayer()?.GetComponent<NetworkPlayer>();

            if(localPlayer != null)
            {
                cam = localPlayer.localPlayerCamera.transform;
            }
        }
        else
        {
            transform.LookAt(cam);
        }
    }
}
