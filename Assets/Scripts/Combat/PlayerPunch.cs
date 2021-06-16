using UnityEngine;
using System.Collections;
using Mirror;

public class PlayerPunch : HitInflictor
{
    [Header("PlayerPunch")]
    public float lifespan;
    private float lifespanTimer;
    public int punchBaseDmg;
    public float punchBaseKP;

    void Start()
    {
        punchBaseDmg = damage;
        punchBaseKP = knockbackPower;
    }

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
