using UnityEngine;
using System.Collections;
using static UnityEngine.ParticleSystem;

public class PlayerParticles : MonoBehaviour
{
    public ThirdPersonCharacterController cc;
    public ParticleSystem sprintParticleSys;
    EmissionModule emission;
    public ParticleSystem sprintParticleSys2;
    EmissionModule emission2;
    public float sprintParticleMagnitude;

    // Use this for initialization
    void Start()
    {
        emission = sprintParticleSys.emission;
        emission2 = sprintParticleSys2.emission;
    }

    // Update is called once per frame
    void Update()
    {
        emission.enabled = Mathf.Abs(cc.playerVelocity.x) >= sprintParticleMagnitude || Mathf.Abs(cc.playerVelocity.z) >= sprintParticleMagnitude;
        emission2.enabled = Mathf.Abs(cc.playerVelocity.x) >= sprintParticleMagnitude || Mathf.Abs(cc.playerVelocity.z) >= sprintParticleMagnitude;
    }
}
