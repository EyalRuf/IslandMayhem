using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public ThirdPersonCharacterController cController;

    [Header("Combat")]
    public bool wasHit;
    public float untouchableDuration;
    public float untouchableTimer;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (wasHit)
        {
            untouchableTimer -= Time.deltaTime;
            wasHit = untouchableTimer > 0;
        }
    }

    public void TryToApplyHit(Vector3 dir)
    {
        if (!wasHit)
            ApplyHit(dir);
    }

    void ApplyHit(Vector3 dir)
    {
        untouchableTimer = untouchableDuration;
        cController.rb.AddForce(dir, ForceMode.Impulse);
    }
}
