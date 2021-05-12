using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Compressor : MonoBehaviour
{
    [Range(0, 1)]
    public float treshold;
    [Range(1, 8)]
    public float ratio;
    public float attack;
    public float release;
    [Range(0, 2)]
    public float makeup;

    private float gain = 1;

    private int sampleRate = 0;

    private void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float sampleTime = SampleTimeInSeconds(data.Length, channels, sampleRate);

        for (int d = 0; d < data.Length; d++)
        {
            float value = data[d];

            if (Mathf.Abs(value) > treshold)
            {
                gain = Mathf.Lerp(gain, 1 / ratio, sampleTime / data.Length * channels / (attack / 1000)); //divide by 1000 for miliseconds
            }
            else
            {
                gain = Mathf.Lerp(gain, 1, sampleTime / data.Length * channels / (release / 1000));
            }

            value *= gain;

            data[d] = Mathf.Clamp(value * (makeup + 1), -1, 1);
        }
    }

    private float SampleTimeInSeconds(int sampleLength, int channels, int sampleRate)
    {
        return ((float)sampleLength / (float)channels) / (float)sampleRate;
    }

    public void SetTreshold(float treshold)
    {
        this.treshold = treshold;
    }

    public void SetRatio(float ratio)
    {
        this.ratio = ratio;
    }

    public void SetAttack(float attack)
    {
        this.attack = attack;
    }

    public void SetRelease(float release)
    {
        this.release = release;
    }

    public void SetMakeup(float makeup)
    {
        this.makeup = makeup;
    }
}
