using UnityEngine;
using System.Collections;
using Mirror;
using UnityEngine.UI;

public class IA_PlayerAmount : InteractionArea
{
    [Header("Requirements")]
    public int amountOfPlayersToSpawnObject;
    public bool isCoolingDown;
    [SyncVar]
    public bool isActive;
    [SyncVar]
    public int playersInside;

    [Header("Spawning")]
    public GameObject toSpawn;
    [SyncVar]
    public int currSpawnLimit;
    [Tooltip("Inclusive")]
    public int minSpawnLimit;
    [Tooltip("Exclusive")]
    public int maxSpawnLimit;
    public float spawnCD;
    [SyncVar]
    private float spawnTimer;
        
    [Header("Raycast")]
    public float sphereCastRadius;
    public LayerMask playerLM;

    [Header("Appearance")]
    public MeshRenderer mr;
    public Material activeMat;
    public Material inactiveMat;
    public Material noMoreSpawnsMat;
    public Text playerAmountIndicator;

    // Use this for initialization
    void Start()
    {
        currSpawnLimit = Random.Range(minSpawnLimit, maxSpawnLimit);
    }

    // Update is called once per frame
    void Update()
    {
        mr.material = isActive ? activeMat : currSpawnLimit <= 0 ? noMoreSpawnsMat : inactiveMat;
        
        playerAmountIndicator.text = isActive ? (playersInside + "/" + amountOfPlayersToSpawnObject)
            : currSpawnLimit > 0 ? TimeFormatting(spawnTimer) : "";

        if (isCoolingDown)
        {
            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0)
            {
                isCoolingDown = false;
            }
        }

        if (!isServer || isCoolingDown || currSpawnLimit <= 0)
            return;

        isActive = !isCoolingDown || currSpawnLimit > 0;

        Collider[] cols = Physics.OverlapSphere(transform.position, sphereCastRadius, playerLM);
        playersInside = cols.Length;
        if (cols.Length >= amountOfPlayersToSpawnObject)
        {
            SpawnObject();
        }
    }

    void SpawnObject()
    {
        GameObject spawned = Instantiate(toSpawn, transform.position + (Vector3.up * 3), Quaternion.identity);
        NetworkServer.Spawn(spawned);
        isCoolingDown = true;
        currSpawnLimit--;
        isActive = false;
        RpcStartTimer();
    }

    string TimeFormatting (float timer)
    {
        return string.Format("{0:#00}:{1:00}", 
            Mathf.Floor(timer / 60), //minutes
            Mathf.Floor(timer) % 60);//seconds
    }

    [ClientRpc]
    void RpcStartTimer()
    {
        isCoolingDown = true;
        spawnTimer = spawnCD;
    }
}
