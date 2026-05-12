using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class RandomEventSystem : NetworkBehaviour
{
    public Vector2 eventDelayRange = new Vector2(30f, 120f);

    private bool started = false;
    public MatchService matchManager;
    public RandomEvent[] events;

    [Header("Misc")]
    public float eventParticleDuration;
    public ParticleSystem[] eventParticles;
    public AudioSource eventSound;

    private void Start()
    {
        if (!isServer) //server only behaviour
            return;

        started = false;
    }

    private void Update()
    {
        //if (matchManager == null)
        //{
        //    matchManager = FindObjectOfType<MatchManager>();
        //} else
        //{
        //    if (matchManager.gameStarted && !started)
        //    {
        //        //DO THIS IN EDITOR
        //        //get all events
        //        //events = FindObjectsOfType<RandomEvent>().OrderBy(e => e.name).ToArray();
        //    }

        //    if (!isServer) //server only behaviour
        //        return;

        //    if (matchManager.gameStarted && !started)
        //    {
        //        //get all events
        //        events = FindObjectsOfType<RandomEvent>();

        //        //start coroutine
        //        StartCoroutine(EventLoop());

        //        started = true;
        //    }
        //}
    }

    private IEnumerator EventLoop()
    {
        while (true)
        {
            //first wait
            yield return new WaitForSecondsRealtime(Random.Range(eventDelayRange.x, eventDelayRange.y));

            //start event
            StartEvent(Random.Range(0, events.Length));
        }
    }

    public void StartEvent(int eventIndex)
    {
        //invoke on server
        events[eventIndex].ServerEvent();

        //invoke on client
        RpcStartClientEvent(events[eventIndex].name);
    }

    [ClientRpc]
    private void RpcStartClientEvent(string eventName)
    {
        foreach (RandomEvent randomEvent in events)
        {
            if(randomEvent.name == eventName)
            {
                randomEvent.ClientEvent();
            }
        }

        StartCoroutine(EventRoutine());
    }

    private IEnumerator EventRoutine()
    {
        eventSound.Play();

        foreach(ParticleSystem particle in eventParticles)
        {
            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = true;
        }

        yield return new WaitForSeconds(eventParticleDuration);

        foreach (ParticleSystem particle in eventParticles)
        {
            ParticleSystem.EmissionModule emission = particle.emission;
            emission.enabled = false;
        }

        yield return null;
    }
}
