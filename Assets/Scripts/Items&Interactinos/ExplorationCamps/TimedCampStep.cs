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

    public override void Update()
    {
        base.Update();
        timerText.text = isCompleted && isEnabled ? TimeFormatter.Format(timer) : "";
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!isServer || !isEnabled)
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
