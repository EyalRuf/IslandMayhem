using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class RandomEventSystem : NetworkBehaviour
{
    public Vector2 eventDelayRange = new Vector2(30f, 120f);

    private bool started = false;
    private MatchManager matchManager;
    private RandomEvent[] events;

    private void Start()
    {
        if (!isServer) //server only behaviour
            return;

        matchManager = FindObjectOfType<MatchManager>();
    }

    private void Update()
    {
        if (!isServer) //server only behaviour
            return;

        if (matchManager.gameStarted && !started)
        {
            //get all events
            events = FindObjectsOfType<RandomEvent>();

            //start coroutine
            StartCoroutine(EventLoop());

            started = true;
        }
    }

    private IEnumerator EventLoop()
    {
        while (true)
        {
            //first wait
            yield return new WaitForSeconds(Random.Range(eventDelayRange.x, eventDelayRange.y));

            //start event
            StartEvent(Random.Range(0, events.Length));
        }
    }

    private void StartEvent(int eventIndex)
    {
        //invoke on server
        events[eventIndex].ServerEvent();

        //invoke on client
        RpcStartClientEvent(eventIndex);
    }

    [ClientRpc]
    private void RpcStartClientEvent(int eventIndex)
    {
        events[eventIndex].ClientEvent();
    }
}
