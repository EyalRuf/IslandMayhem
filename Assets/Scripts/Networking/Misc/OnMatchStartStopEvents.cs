using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnMatchStartStopEvents : OnMatchStartStop
{
    public UnityEvent OnStart;
    public UnityEvent OnStop;

    public override void OnMatchStart()
    {
        OnStart.Invoke();

        base.OnMatchStart();
    }

    public override void OnMatchStop()
    {
        OnStop.Invoke();

        base.OnMatchStop();
    }
}
