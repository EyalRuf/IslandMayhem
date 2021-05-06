using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CollisionSound : MonoBehaviour
{
    public Vector2 pitchRange = new Vector2(1, 1);
    public AudioClip[] sounds;
    public float collisionThreshold;

    private AudioSource source;

    private void Start()
    {
        source = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.impulse.magnitude > collisionThreshold && !source.isPlaying)
        {
            source.pitch = Random.Range(pitchRange.x, pitchRange.y);
            source.clip = sounds[Random.Range(0, sounds.Length)];
            source.Play();
        }
    }
}
