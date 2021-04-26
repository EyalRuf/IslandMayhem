using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrbitAround : MonoBehaviour
{
    public float speed;
    public Vector3 axis;
    public Transform target;

    private Vector3 offset;

    private void Start()
    {
        float idOffset = gameObject.GetInstanceID();

        offset = Random.insideUnitSphere * idOffset;
    }

    private void Update()
    {
        transform.RotateAround(
            target.position,
            axis, 
            speed * Time.deltaTime);
    }
}
