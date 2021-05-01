using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(AudioSource))]
public class SteamlessVoiceChat : NetworkBehaviour
{
    public KeyCode voiceChatKey = KeyCode.V;
    public bool previewVoice = true;

    [Header("Misc settings")]
    [Range(11025, 48000)]
    public int sampleRate = 22050;
    public int chunkSize = 256;
    public int deviceIndex = 0;

    public List<VoicePacket> packetQueue = new List<VoicePacket>();
    private AudioClip microphoneClip;
    private int lastMicReadPos = 0;

    private int playbackSampleRate;
    private float playbackPos = 0;
    private AudioSource source;

    private void Start()
    {
        //get samplerate for playback
        playbackSampleRate = AudioSettings.outputSampleRate;

        //start mic
        microphoneClip = Microphone.Start(Microphone.devices[deviceIndex], true, 1, sampleRate);
        lastMicReadPos = Microphone.GetPosition(Microphone.devices[deviceIndex]);

        //start playback source
        source = GetComponent<AudioSource>();

        source.loop = true;
        source.clip = AudioClip.Create("VoiceChat", sampleRate, 1, sampleRate, true, OnAudioRead, OnAudioSetPosition);
        source.Play();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            int micReadPos = Microphone.GetPosition(Microphone.devices[deviceIndex]); //read pos
            int sampleDiff = (micReadPos < lastMicReadPos) ? //get the difference in audio samples
                microphoneClip.samples - lastMicReadPos + micReadPos
                :
                micReadPos - lastMicReadPos
                ;

            int packetDiff = Mathf.FloorToInt((float)sampleDiff / (float)chunkSize); //find number of packets in sample diff

            if (Input.GetKey(voiceChatKey))
            {
                for (int p = 0; p < packetDiff; p++) //send packets
                {
                    VoicePacket packet = new VoicePacket(GetDataFromMic(chunkSize, lastMicReadPos + (p * chunkSize)));
                    Debug.Log(packet.chunk.Length);

                    SendPacket(packet);
                }
            }

            lastMicReadPos += packetDiff * chunkSize; //advance lastReadPos by how many packets we made
        }
    }

    private float[] GetDataFromMic(int length, int offset)
    {
        float[] micData = new float[microphoneClip.samples];
        microphoneClip.GetData(micData, 0);

        float[] data = new float[length];

        for(int d = 0; d < data.Length; d++)
        {
            data[d] = micData[Mathf.FloorToInt(Mathf.Repeat(d + offset, micData.Length))];
        }

        return data;
    }

    #region Networking

    private void SendPacket(VoicePacket packet)
    {
        if (previewVoice)
        {
            packetQueue.Add(packet);
        }

        CmdSendData(NetworkTools.ObjectToData(packet));
    }

    [Command(channel = 1)] //unreliable for speed
    private void CmdSendData(byte[] message)
    {
        //get all players
        NetworkIdentity[] players = CustomNetworkManager.GetAllPlayers().Where(p => p != netIdentity).ToArray();

        //only send to players within the audiosource's range.
        for (int p = 0; p < players.Length; p++)
        {
            if (Vector3.Distance(transform.position, players[p].transform.position) < source.maxDistance)
            {
                TargetReceiveData(players[p].connectionToClient, message);
            }
        }
    }

    [TargetRpc(channel = 1)] //unreliable for speed
    private void TargetReceiveData(NetworkConnection connection, byte[] message)
    {
        VoicePacket serializedMessage = null;

        try
        {
            serializedMessage = (VoicePacket)NetworkTools.DataToObject(message);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex.ToString());
        }

        if (serializedMessage != null)
        {
            packetQueue.Add(serializedMessage);
        }
    }

    #endregion

    #region Playback

    private void OnAudioRead(float[] data)
    {
        /*
        for (int d = 0; d < data.Length; d++)
        {
            if (packetQueue.Count <= 0)
            {
                //sweet, sweet silence
                data[d] = 0;
            }
            else
            {
                data[d] = packetQueue[0].chunk[playbackPos];

                //advance playbackpos and switch packet when neccesary
                playbackPos++;
                if (playbackPos >= packetQueue[0].chunk.Length)
                {
                    playbackPos = 0;
                    packetQueue.RemoveAt(0);
                }
            }
        }
        */
    }

    void OnAudioSetPosition(int newPosition)
    {
        /*
        if(packetQueue.Count > 0)
        {
            source.clip.SetData(packetQueue[0].chunk, 0);
            packetQueue.RemoveAt(0);
        }
        */
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        float sampleRateRatio = (float)sampleRate / (float)playbackSampleRate;
        float sample = 0;

        for (int d = 0; d < data.Length; d++)
        {
            if(d % channels == 0)
            {
                if (packetQueue.Count <= 0)
                {
                    //sweet, sweet silence
                    sample = 0;
                }
                else
                {
                    sample = packetQueue[0].chunk[Mathf.FloorToInt(playbackPos)];

                    //advance playbackpos and switch packet when neccesary
                    playbackPos += sampleRateRatio;
                    if (playbackPos >= packetQueue[0].chunk.Length)
                    {
                        playbackPos = 0;
                        packetQueue.RemoveAt(0);
                    }
                }
            }

            data[d] = sample;
        }
    }

    #endregion 
}

[System.Serializable]
public class VoicePacket
{
    public float[] chunk;

    public VoicePacket(float[] chunk)
    {
        this.chunk = chunk;
    }
}

public static class NetworkTools
{
    public static byte[] ObjectToData(object msg)
    {
        System.Runtime.Serialization.Formatters.Binary.BinaryFormatter bf = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        using (var ms = new System.IO.MemoryStream())
        {
            bf.Serialize(ms, (object)msg);
            return ms.ToArray();
        }
    }

    public static object DataToObject(byte[] data)
    {
        using (var memStream = new System.IO.MemoryStream())
        {
            var binForm = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            memStream.Write(data, 0, data.Length);
            memStream.Seek(0, System.IO.SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);

            try
            {
                return obj;
            }
            catch
            {
                Debug.LogWarning("Could not convert data to message.");
                return null;
            }
        }
    }
}