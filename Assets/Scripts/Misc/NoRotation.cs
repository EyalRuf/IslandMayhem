using UnityEngine;
using System.Collections;

public class NoRotation : MonoBehaviour
{
    private Vector3 baseLocalPos;

    void Start()
    {
        baseLocalPos = transform.localPosition;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = transform.parent.position + baseLocalPos;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }
}
