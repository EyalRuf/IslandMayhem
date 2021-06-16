using UnityEngine;
using System.Collections;
using Mirror;

public class Hittable : NetworkBehaviour
{
    Rigidbody rb;
    float hitCD;

    // Use this for initialization
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (hitCD > 0)
        {
            hitCD -= Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isServer && hitCD <= 0)
        {
            HitInflictor hit = other.GetComponent<HitInflictor>();

            if (hit != null && hit.isActive && hit.initiatorNetId != netId)
            {
                Vector3 knockbackDir = transform.position - hit.transform.position;
                rb.AddForce(knockbackDir.normalized * hit.knockbackPower / 15, ForceMode.Impulse);
                hit.HitInflicted();
                hitCD = 1f;
            }
        }
    }
}
