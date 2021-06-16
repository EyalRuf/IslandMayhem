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
    public MatchManager mm;

    [Header("Buffs")]
    public float overlapRadius;
    public LayerMask overlapMask;
    public GameObject buffParticles;

    private AudioSource source;
    [HideInInspector] public CampManager campManager;

    // Use this for initialization
    void Start()
    {
        source = GetComponent<AudioSource>();

        campSteps = new List<CampStep>(GetComponentsInChildren<CampStep>()).FindAll(step => step.transform.parent == transform);
        mm = FindObjectOfType<MatchManager>();

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

        campOverheadText.text = spawnsLeft <= 0 ? "Out of stock" : isCoolingDown ? (cooldownDuration - cooldownTimer < 3f ? "Camp complete!" : "Cooling down") : "Solve to get a buff/totem piece";
        campSignText.text = "Charges\nleft: " + spawnsLeft;
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
                if (mm.gameStarted && currStepsCompleted == campSteps.Count)
                {
                    CampDone();
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
        if (isTotemDispenser)
        {
            GameObject spawned = Instantiate(totemPrefab, pedestalTransform.position + (Vector3.up * 3), Quaternion.identity);
            NetworkServer.Spawn(spawned);
        } else
        {
            // Get players in area
            Collider[] colliders = Physics.OverlapSphere(pedestalTransform.position, overlapRadius, overlapMask);
            
            if (colliders.Length > 0)
            {
                List<NetworkPlayer> netplayers = new List<Collider>(colliders).ConvertAll(c => c.GetComponent<NetworkPlayer>());

                int team0 = netplayers.FindAll(p => p.playerTeam == 0).Count;
                int team1 = netplayers.FindAll(p => p.playerTeam == 1).Count;

                List<PlayerBuffs> buffedPlayers = new List<PlayerBuffs>();

                if (team0 == team1)
                {
                    buffedPlayers.AddRange(mm.teams[0].playersInTeam.ConvertAll(p => p.GetComponent<PlayerBuffs>()));
                    buffedPlayers.AddRange(mm.teams[1].playersInTeam.ConvertAll(p => p.GetComponent<PlayerBuffs>()));
                } else if (team0 > team1)
                {
                    buffedPlayers.AddRange(mm.teams[0].playersInTeam.ConvertAll(p => p.GetComponent<PlayerBuffs>()));
                } else
                {
                    buffedPlayers.AddRange(mm.teams[1].playersInTeam.ConvertAll(p => p.GetComponent<PlayerBuffs>()));
                }
            
                // Randomize buff
                Buff randomBuff = (Buff)UnityEngine.Random.Range(0, (int)Buff.PunchPower + 1);
                buffedPlayers.ForEach(p => p.RpcGiveBuff(randomBuff));
            }
        }

        campSteps.ForEach(step => step.isEnabled = false);

        isCoolingDown = true;
        cooldownTimer = cooldownDuration;
        spawnsLeft--;

        RpcCampDone();
    }

    [ClientRpc]
    private void RpcCampDone()
    {
        source.PlayOneShot(campDoneSound);
        Instantiate(buffParticles, transform);
    }

    void ResetCamp ()
    {
        isCoolingDown = false;
        campSteps.ForEach(step => step.ResetStep());
    }
}

public enum Buff
{
    MoveSpeed = 0,
    SizeUp = 1,
    JumpForce = 2,
    Health = 3,
    PunchPower = 4,
}