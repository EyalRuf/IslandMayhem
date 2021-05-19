using UnityEngine;
using System.Collections;
using Mirror;

public class CampStep : NetworkBehaviour
{
    [SyncVar]
    public bool isCompleted;
    [SyncVar]
    public bool isEnabled;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public virtual void CompleteStep ()
    {
        isCompleted = true;
    }

    public virtual void ResetStep ()
    {
        isCompleted = false;
        isEnabled = true;
    }
}
