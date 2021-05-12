using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Limiter : MonoBehaviour
{
    public float limit;

    private void OnAudioFilterRead(float[] data, int channels)
    {
        for (int d = 0; d < data.Length; d++)
        {
            data[d] = Mathf.Clamp(data[d], -limit, limit);
        }
    }
}
