using UnityEngine;
using System.Collections;
using Mirror;

public class IA_PlayerAmount : InteractionArea
{
    [Header("Requirements")]
    public int amountOfPlayersToSpawnObject;
    public bool isCoolingDown;
    [SyncVar]
    public bool isActive;

    [Header("Spawning")]
    public GameObject toSpawn;
    [SyncVar]
    public int currSpawnLimit;
    [Tooltip("Inclusive")]
    public int minSpawnLimit;
    [Tooltip("Exclusive")]
    public int maxSpawnLimit;
    public float spawnCD;
        
    [Header("Raycast")]
    public float sphereCastRadius;
    public LayerMask playerLM;

    [Header("Appearance")]
    public MeshRenderer mr;
    public Material activeMat;
    public Material inactiveMat;
    public Material noMoreSpawnsMat;

    // Use this for initialization
    void Start()
    {
        currSpawnLimit = Random.Range(minSpawnLimit, maxSpawnLimit);
    }

    // Update is called once per frame
    void Update()
    {
        mr.material = isActive ? activeMat : currSpawnLimit <= 0 ? noMoreSpawnsMat : inactiveMat;

        if (!isServer || isCoolingDown || currSpawnLimit <= 0)
            return;

        isActive = !isCoolingDown || currSpawnLimit > 0;

        Collider[] cols = Physics.OverlapSphere(transform.position, sphereCastRadius, playerLM);
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
        StartCoroutine(Cooldown());
    }

    IEnumerator Cooldown ()
    {
        yield return new WaitForSeconds(spawnCD);
        isCoolingDown = false;
    }
}
