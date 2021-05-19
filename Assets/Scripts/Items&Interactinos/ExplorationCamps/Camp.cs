using UnityEngine;
using System.Collections;
using Mirror;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class Camp : NetworkBehaviour
{
    public List<CampStep> campSteps;
    public GameObject rewardPrefab;

    [SyncVar]
    public int currStepsCompleted;

    [SyncVar]
    public bool isCoolingDown;
    [SyncVar]
    public float cooldownTimer;
    public float cooldownDuration;

    [SyncVar]
    public int spawnsLeft;
    public Vector2Int spawnLimitRange;

    [Header("UI")]
    public Text text;

    // Use this for initialization
    void Start()
    {
        if (!isServer)
            return;

        campSteps = new List<CampStep>(GetComponentsInChildren<CampStep>());
        spawnsLeft = UnityEngine.Random.Range(spawnLimitRange.x, spawnLimitRange.y);
    }

    // Update is called once per frame
    void Update()
    {
        text.text = spawnsLeft <= 0 ? "Depleted" :isCoolingDown ? TimeFormatter.Format(cooldownTimer) : GetStepsText();
        
        if (!isServer && spawnsLeft <= 0)
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
            if (currStepsCompleted == campSteps.Count)
            {
                CampDone();
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
        GameObject spawned = Instantiate(rewardPrefab, transform.position + (Vector3.up * 3), Quaternion.identity);
        NetworkServer.Spawn(spawned);

        isCoolingDown = true;
        cooldownTimer = cooldownDuration;
        spawnsLeft--;
    }

    void ResetCamp ()
    {
        isCoolingDown = false;
        campSteps.ForEach(step => step.ResetStep());
    }
}
