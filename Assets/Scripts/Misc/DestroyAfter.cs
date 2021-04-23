using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAfter : MonoBehaviour
{
    public float seconds = 10f;

    private void Start()
    {
        Destroy(gameObject, seconds);
        Destroy(this);
    }
}
