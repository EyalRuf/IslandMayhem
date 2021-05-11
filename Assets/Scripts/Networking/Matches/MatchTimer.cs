using UnityEngine;
using Mirror;
using System;

public class MatchTimer : NetworkBehaviour
{
    [Header("General")]
    public float matchLength;
    [SyncVar]
    public float timeRemaining = 0;
    private float startTime = 0;

    [Header("States")]
    public bool didStart = false;

    public float GetMatchTime => timeRemaining;
    public string GetMatchTimeString => didStart ? TimeFormatter.Format(GetMatchTime) : TimeFormatter.Format(matchLength);
    public bool IsTimeOver => didStart && GetMatchTime <= 0;

    void FixedUpdate()
    {
        if (isServer && didStart)
        {
            timeRemaining -= Time.fixedDeltaTime;
        }
    }

    public void MatchStarted()
    {
        didStart = true;
        startTime = Time.time;
        timeRemaining = matchLength - (Time.time - startTime);
    }

    public void MatchEnd()
    {
        didStart = false;
        timeRemaining = 0;
    }
}
