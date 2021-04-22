using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CoconutEvent : RandomEvent
{
    [Header("Real Coconuts")]
    public GameObject realCoconut;
    public Vector3 realSpawnRange;
    public Vector3 realSpawnOffset;
    public Vector2Int realMinMaxSpawns;

    [Header("Fake Coconuts")]
    public ParticleSystem fakeCoconuts;
    public Vector3 fakeSpawnRange;
    public Vector3 fakeSpawnOffset;
    public Vector2Int fakeMinMaxSpawns;

    public override void ServerEvent()
    {
        //spawn coconuts on each player
        GameObject[] players = CustomNetworkManager.GetAllPlayers().ToArray();

        foreach(GameObject player in players)
        {
            int spawns = Random.Range(realMinMaxSpawns.x, realMinMaxSpawns.y);
            for(int s = 0; s < spawns; s++)
            {
                Vector3 offset = Random.insideUnitSphere;

                NetworkServer.Spawn(Instantiate(realCoconut, player.transform.position + new Vector3(realSpawnRange.x * offset.x, realSpawnRange.y * offset.y, realSpawnRange.z * offset.z) + realSpawnOffset, Quaternion.identity));
            }
        }
    }

    public override void ClientEvent()
    {
        int spawns = Random.Range(fakeMinMaxSpawns.x, fakeMinMaxSpawns.y);
        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

        for (int s = 0; s < spawns; s++)
        {
            Vector3 offset = Random.insideUnitSphere;

            emitParams.position = CustomNetworkManager.GetLocalPlayer().transform.position + new Vector3(fakeSpawnRange.x * offset.x, fakeSpawnRange.y * offset.y, fakeSpawnRange.z * offset.z) + fakeSpawnOffset;
            fakeCoconuts.Emit(emitParams, 1);
        }

    }
}
