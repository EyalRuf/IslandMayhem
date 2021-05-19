using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerCampStep : CampStep
{
    [Header("Timer")]
    public float duration;
    [SyncVar]
    private float timer;
    public Text timerText;

    void Update()
    {
        timerText.text = isCompleted && isEnabled ? TimeFormatter.Format(timer) : "";
    }

    public virtual void FixedUpdate()
    {
        if (!isServer)
            return;

        if (isCompleted && timer > 0)
        {
            timer -= Time.fixedDeltaTime;

            if (timer <= 0)
            {
                ResetStep();
            }
        }
    }

    public override void CompleteStep()
    {
        base.CompleteStep();
        timer = duration;
    }
}
