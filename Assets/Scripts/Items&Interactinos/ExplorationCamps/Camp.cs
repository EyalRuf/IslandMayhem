using UnityEngine;
using System.Collections;
using Mirror;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class Camp : NetworkBehaviour
{
    [Header("Camp")]
    public List<CampStep> campSteps;
    public GameObject rewardPrefab;
    [SyncVar] public int currStepsCompleted;
    [SyncVar] public bool isCoolingDown;
    [SyncVar] private float cooldownTimer;
    public float cooldownDuration;
    [SyncVar] public int spawnsLeft;
    public Vector2Int spawnLimitRange;

    [Header("UI")]
    public Text text;

    [Header("MISC")]
    public Transform pedestalTransform;

    // Use this for initialization
    void Start()
    {
        if (!isServer)
            return;

        // Getting first level hierarchy children who are camp steps
        campSteps = new List<CampStep>(GetComponentsInChildren<CampStep>()).FindAll(step => step.transform.parent == transform);
        spawnsLeft = UnityEngine.Random.Range(spawnLimitRange.x, spawnLimitRange.y);
    }

    // Update is called once per frame
    void Update()
    {
        text.text = spawnsLeft <= 0 ? "Depleted" : isCoolingDown ? TimeFormatter.Format(cooldownTimer) : GetStepsText();
        
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
        GameObject spawned = Instantiate(rewardPrefab, pedestalTransform.position + (Vector3.up * 3), Quaternion.identity);
        NetworkServer.Spawn(spawned);

        campSteps.ForEach(step => step.isEnabled = false);

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
