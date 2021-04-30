using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutOfBounds : MonoBehaviour
{
    public Vector3 respawnHeight;
    public LayerMask objectMask;

    private void OnTriggerEnter(Collider other)
    {
        if(((1 << other.gameObject.layer) & objectMask) != 0 && other.GetComponent<Rigidbody>() != null)
        {
            other.transform.position = new Vector3(other.transform.position.x, respawnHeight.y, other.transform.position.z);
        }
    }
}
