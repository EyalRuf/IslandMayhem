using UnityEngine;
using System.Collections;
using Mirror;

public class PlayerPunch : HitInflictor
{
    public float lifespan;
    private float lifespanTimer;

    void OnEnable()
    {
        lifespanTimer = lifespan;
        isActive = true;
    }

    // Update is called once per frame
    public override void Update()
    {
        base.Update();

        lifespanTimer -= Time.deltaTime;

        if (lifespanTimer < 0)
        {
            gameObject.SetActive(false);
        }
    }
}
