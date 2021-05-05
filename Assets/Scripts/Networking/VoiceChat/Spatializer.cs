using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Spatializer : MonoBehaviour
{
    public AnimationCurve rollOff;

    private AudioSource source;
    private Transform listener;
    private float volume = 0;

    private void Start()
    {
        source = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if(listener != null)
        {
            volume = rollOff.Evaluate(
                Mathf.Clamp01((Vector3.Distance(transform.position, listener.position) - source.minDistance) / (source.maxDistance - source.minDistance))
                );
        }
        else
        {
            listener = CustomNetworkManager.GetLocalPlayer().GetComponentInChildren<AudioListener>().transform;
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        for (int d = 0; d < data.Length; d++)
        {
            data[d] *= volume;
        }
    }
}
