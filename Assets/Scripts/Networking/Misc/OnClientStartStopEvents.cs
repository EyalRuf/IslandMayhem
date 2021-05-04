using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OnClientStartStopEvents : OnClientStartStop
{
    public UnityEvent OnStart;
    public UnityEvent OnStop;

    public override void OnClientStart()
    {
        OnStart.Invoke();

        base.OnClientStart();
    }

    public override void OnClientStop()
    {
        OnStop.Invoke();

        base.OnClientStop();
    }
}
