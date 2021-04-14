using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using UnityEngine;
using Mirror;
using Steamworks;

[RequireComponent(typeof(AudioSource))]
public class VoiceChat : NetworkBehaviour
{
    public int sampleRate = 22050;
    public KeyCode voiceChatKey = KeyCode.Tilde;
    public uint chunkSize = 1024;
    public uint minBufferSize;
    public bool previewVoice = true;

    private AudioSource source;
    private List<float> audioBuffer = new List<float>();
    private int position;
    private float[] dataReadBuffer;

    private void Start()
    {
        source = GetComponent<AudioSource>();
        source.clip = AudioClip.Create("VoiceChat", (int)chunkSize, 1, sampleRate, true, OnAudioRead, OnPositionSet);
        source.loop = true;
        source.Play();
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            //start or stop recording
            if (isLocalPlayer && Input.GetKeyDown(voiceChatKey))
            {
                SteamUser.StartVoiceRecording();
            }
            if (isLocalPlayer && Input.GetKeyUp(voiceChatKey))
            {
                SteamUser.StopVoiceRecording();
            }


            //send voice data
            uint Compressed;

            EVoiceResult ret = SteamUser.GetAvailableVoice(out Compressed);
            if (ret == EVoiceResult.k_EVoiceResultOK && Compressed > chunkSize)
            {
                byte[] DestBuffer = new byte[chunkSize];
                uint BytesWritten;
                ret = SteamUser.GetVoice(true, DestBuffer, chunkSize, out BytesWritten);
                if (ret == EVoiceResult.k_EVoiceResultOK && BytesWritten > 0)
                {
                    CmdSendData(DestBuffer, BytesWritten);
                }

                if (previewVoice)
                {
                    AddDataToAudioBuffer(DestBuffer, BytesWritten);
                }
            }
        }
    }

    [Command(channel = 1)] //unreliable for speed
    private void CmdSendData(byte[] data, uint size)
    {
        //get all players
        GameObject[] players = CustomNetworkManager.GetAllPlayers().Where(p => p != gameObject).ToArray();

        //only send to players within the audiosource's range.
        for (int p = 0; p < players.Length; p++)
        {
            if (Vector3.Distance(transform.position, players[p].transform.position) < source.maxDistance)
            {
                CmdReceiveData(players[p].GetComponent<NetworkIdentity>().connectionToClient, data, size);
            }
        }
    }

    [TargetRpc(channel = 1)] //unreliable for speed
    private void CmdReceiveData(NetworkConnection connection, byte[] data, uint size)
    {
        AddDataToAudioBuffer(data, size);
    }

    private void AddDataToAudioBuffer(byte[] data, uint size)
    {
        byte[] decompressedData = new byte[sampleRate * 2];
        uint decompressedSize;
        EVoiceResult ret = SteamUser.DecompressVoice(data, size, decompressedData, (uint)decompressedData.Length, out decompressedSize, (uint)sampleRate);
        if (ret == EVoiceResult.k_EVoiceResultOK && decompressedSize > 0)
        {
            //convert data to mono float[]
            float[] singleData = new float[sampleRate];
            for (int i = 0; i < singleData.Length; ++i)
            {
                singleData[i] = (short)(decompressedData[i * 2] | decompressedData[i * 2 + 1] << 8) / 32768.0f;
            }

            //add floating point audio to buffer
            audioBuffer.AddRange(singleData);
        }
    }

    private void OnAudioRead(float[] data)
    {
        for (int d = 0; d < data.Length; d++)
        {
            data[d] = dataReadBuffer[d + position];
        }

        this.position = data.Length;
    }

    private void OnPositionSet(int position)
    {
        float[] data = new float[chunkSize];

        if (audioBuffer.Count > minBufferSize)
        {
            //get audio chunk from buffer
            float[] audioChunk = audioBuffer.GetRange(0, (int)chunkSize).ToArray();
            audioBuffer.RemoveRange(0, (int)chunkSize);

            for (int d = 0; d < audioChunk.Length; d++)
            {
                data[d] = audioChunk[d];
            }
        }

        this.dataReadBuffer = data;

        this.position = position;
    }
}