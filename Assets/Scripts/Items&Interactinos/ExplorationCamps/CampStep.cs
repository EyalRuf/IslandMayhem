using UnityEngine;
using System.Collections;
using Mirror;

public class CampStep : NetworkBehaviour
{
    [SyncVar]
    public bool isCompleted;
    [SyncVar]
    public bool isEnabled;

    [Header("Particles")]
    public GameObject idleParticles;
    public GameObject finishStepParticles;
    public Transform finishParticlesTransform;

    // Use this for initialization
    public virtual void Start()
    {

    }

    // Update is called once per frame
    public virtual void Update()
    {
        idleParticles.SetActive(!isCompleted && isEnabled);
    }

    public virtual void FixedUpdate()
    {

    }

    public virtual void CompleteStep ()
    {
        isCompleted = true;
        RpcCompleteStep();
    }

    [ClientRpc]
    public void RpcCompleteStep()
    {
        Instantiate(finishStepParticles, finishParticlesTransform);
    }

    public virtual void ResetStep ()
    {
        isCompleted = false;
        isEnabled = true;
    }
}
