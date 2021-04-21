using UnityEngine;
using System.Collections;

public class Combatability : MonoBehaviour
{
    [Header("Combat")]
    public float knockbackMultiplyer;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            PlayerCombat pc = other.GetComponent<PlayerCombat>();
            Vector3 hitDir = transform.position - pc.transform.position;
            pc.TryToApplyHit(hitDir * knockbackMultiplyer);
        }
    }
}
