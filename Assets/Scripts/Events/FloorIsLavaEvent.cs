using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorIsLavaEvent : RandomEvent
{
    public float duration;
    public float initialSwapSpeed;
    public float swapSpeed;
    public GameObject floorIsLavaMessagePrefab;
    public Vector3 messageSpawnOffset;
    public float lavaOffset;
    public Transform water;
    public Transform lava;

    private bool isActive = false;

    public override void ServerEvent()
    {
        
    }

    public override void ClientEvent()
    {
        if (!isActive)
        {
            Instantiate(floorIsLavaMessagePrefab, CustomNetworkManager.GetLocalPlayer().transform.position + messageSpawnOffset, Quaternion.identity);

            StartCoroutine(MakeTheFloorLava());
        }
    }

    private IEnumerator MakeTheFloorLava()
    {
        isActive = true;

        //enable lava
        lava.gameObject.SetActive(true);

        float waterVelocity = 0;
        float lavaVelocity = 0;

        Vector3 waterPos = water.transform.position;
        Vector3 lavaPos = lava.transform.position;

        while (Vector3.Distance(water.position, lavaPos) > 0.01f || Vector3.Distance(lava.position, waterPos) > 0.01f)
        {
            water.position = new Vector3(
                water.position.x,
                Mathf.SmoothDamp(water.position.y, lavaPos.y, ref waterVelocity, initialSwapSpeed * Time.deltaTime),
                water.position.z);

            lava.position = new Vector3(
                lava.position.x,
                Mathf.SmoothDamp(lava.position.y, waterPos.y, ref lavaVelocity, initialSwapSpeed * Time.deltaTime),
                lava.position.z);

            yield return new WaitForEndOfFrame();
        }

        float durationTimer = 0;
        while (durationTimer < duration)
        {
            durationTimer += Time.deltaTime;

            water.position = new Vector3(
                water.position.x,
                Mathf.SmoothDamp(water.position.y, lavaPos.y, ref waterVelocity, swapSpeed * Time.deltaTime),
                water.position.z);

            lava.position = new Vector3(
                lava.position.x,
                Mathf.SmoothDamp(lava.position.y, waterPos.y + lavaOffset, ref lavaVelocity, swapSpeed * Time.deltaTime),
                lava.position.z);

            yield return new WaitForEndOfFrame();
        }

        while (Vector3.Distance(water.position, waterPos) > 0.01f || Vector3.Distance(lava.position, lavaPos) > 0.01f)
        {
            water.position = new Vector3(
                water.position.x,
                Mathf.SmoothDamp(water.position.y, waterPos.y, ref waterVelocity, initialSwapSpeed * Time.deltaTime),
                water.position.z);

            lava.position = new Vector3(
                lava.position.x,
                Mathf.SmoothDamp(lava.position.y, lavaPos.y, ref lavaVelocity, initialSwapSpeed * Time.deltaTime),
                lava.position.z);

            yield return new WaitForEndOfFrame();
        }

        //disable lava
        lava.gameObject.SetActive(false);

        isActive = false;

        yield return null;
    }
}
