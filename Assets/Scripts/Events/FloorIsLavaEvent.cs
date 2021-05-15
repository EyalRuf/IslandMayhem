using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorIsLavaEvent : RandomEvent
{
    public float duration;
    public float swapSpeed;
    public GameObject floorIsLavaMessagePrefab;
    public Vector3 messageSpawnOffset;
    public Transform water;
    public Transform lava;

    public override void ServerEvent()
    {
        
    }

    public override void ClientEvent()
    {
        //send message
        Instantiate(floorIsLavaMessagePrefab, CustomNetworkManager.GetLocalPlayer().transform.position + messageSpawnOffset, Quaternion.identity);

        StartCoroutine(MakeTheFloorLava());
    }

    private IEnumerator MakeTheFloorLava()
    {
        float waterVelocity = 0;
        float lavaVelocity = 0;

        Vector3 waterPos = water.transform.position;
        Vector3 lavaPos = lava.transform.position;

        float durationTimer = 0;
        while (durationTimer < duration)
        {
            durationTimer += Time.deltaTime;

            water.transform.position = new Vector3(
                water.transform.position.x,
                Mathf.SmoothDamp(water.transform.position.y, lavaPos.y, ref waterVelocity, swapSpeed * Time.deltaTime),
                water.transform.position.z);

            lava.transform.position = new Vector3(
                lava.transform.position.x,
                Mathf.SmoothDamp(lava.transform.position.y, waterPos.y, ref lavaVelocity, swapSpeed * Time.deltaTime),
                lava.transform.position.z);

            yield return new WaitForEndOfFrame();
        }

        while (Vector3.Distance(water.transform.position, waterPos) > 0.01f || Vector3.Distance(lava.transform.position, lavaPos) > 0.01f)
        {
            water.transform.position = new Vector3(
                water.transform.position.x,
                Mathf.SmoothDamp(water.transform.position.y, waterPos.y, ref waterVelocity, swapSpeed * Time.deltaTime),
                water.transform.position.z);

            lava.transform.position = new Vector3(
                lava.transform.position.x,
                Mathf.SmoothDamp(lava.transform.position.y, lavaPos.y, ref lavaVelocity, swapSpeed * Time.deltaTime),
                lava.transform.position.z);

            yield return new WaitForEndOfFrame();
        }

        yield return null;
    }
}
