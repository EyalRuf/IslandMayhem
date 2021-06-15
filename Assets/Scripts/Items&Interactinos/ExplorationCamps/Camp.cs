using UnityEngine;
using System.Collections;
using Mirror;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(AudioSource))]
public class Camp : NetworkBehaviour
{
    [Header("Camp")]
    public List<CampStep> campSteps;
    [SyncVar] public int currStepsCompleted;
    [SyncVar] public bool isCoolingDown;
    [SyncVar] private float cooldownTimer;
    public float cooldownDuration;
    [SyncVar] public int spawnsLeft;
    public Vector2Int spawnLimitRange;

    [Header("Rewards")]
    [SyncVar] public bool isTotemDispenser;
    public GameObject totemDispenserVisual;
    public GameObject totemPrefab;
    public GameObject alternativePrefab;

    [Header("UI")]
    public Text campOverheadText;
    public Text campSignText;
    public Text timerAndExesText;
    public Text totemDispenserText;

    [Header("Misc")]
    public Transform pedestalTransform;
    public AudioClip campDoneSound;
    public Transform[] spawnPositions;

    private AudioSource source;
    [HideInInspector] public CampManager campManager;

    // Use this for initialization
    void Start()
    {
        source = GetComponent<AudioSource>();

        campSteps = new List<CampStep>(GetComponentsInChildren<CampStep>()).FindAll(step => step.transform.parent == transform);
        
        if (!isServer)
            return;

        // Getting first level hierarchy children who are camp steps
        spawnsLeft = UnityEngine.Random.Range(spawnLimitRange.x, spawnLimitRange.y);
    }

    // Update is called once per frame
    void Update()
    {
        if(campManager != null)
        {
            totemDispenserText.text = isTotemDispenser ? "" : Mathf.CeilToInt(campManager.swapDelay - campManager.swapTimer).ToString();
        }
        else
        {
            campManager = FindObjectOfType<CampManager>();
        }

        campOverheadText.text = spawnsLeft <= 0 ? "Totem pieces out of stock" : isCoolingDown ? (cooldownDuration - cooldownTimer < 3f ? "Take the piece to your totem!" : "Restocking totem piece") : "Solve to get a totem piece";
        campSignText.text = "Totem Pieces\nleft: " + spawnsLeft;
        timerAndExesText.text = spawnsLeft <= 0 ? "Come back another day" : isCoolingDown ? TimeFormatter.Format(cooldownTimer) : GetStepsText();
        
        if (!isServer || spawnsLeft <= 0)
            return;

        currStepsCompleted = campSteps.FindAll(step => step.isCompleted).Count;

        if (isCoolingDown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isCoolingDown = false;
                ResetCamp();
            }
        } else
        {
            if (!totemDispenserVisual.activeInHierarchy || isTotemDispenser)
            {
                if (currStepsCompleted == campSteps.Count)
                {
                    CampDone();

                    if (isServer)
                    {
                        //for clients
                        RpcCampDone();
                    }
                }
            }
        }
    }

    private string GetStepsText()
    {
        string stepsText = "";

        campSteps.ForEach(step => stepsText += step.isCompleted ? " ✓ " : " X ");

        return stepsText;
    }

    private void CampDone()
    {
        GameObject spawned = Instantiate(isTotemDispenser ? totemPrefab : alternativePrefab, pedestalTransform.position + (Vector3.up * 3), Quaternion.identity);
        NetworkServer.Spawn(spawned);

        campSteps.ForEach(step => step.isEnabled = false);

        isCoolingDown = true;
        cooldownTimer = cooldownDuration;
        spawnsLeft--;
    }

    [ClientRpc]
    private void RpcCampDone()
    {
        source.PlayOneShot(campDoneSound);
    }

    void ResetCamp ()
    {
        isCoolingDown = false;
        campSteps.ForEach(step => step.ResetStep());
    }
}
