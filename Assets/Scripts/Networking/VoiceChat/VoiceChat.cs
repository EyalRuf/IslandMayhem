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
    public KeyCode voiceChatKey = KeyCode.V;
    public bool previewVoice = true;

    [Header("Voice settings")]
    public SortedList<ulong, VoiceChatPacket> PacketQueue = new SortedList<ulong, VoiceChatPacket>();
    private AudioSource m_audioSource;
    // how many packets we should collect before starting playback
    public int PacketBuffer = 1;
    // whether or not we're currently waiting for more packets to be collected.
    public bool Buffering = true;

    [Header("Misc settings")]
    [Range(11025, 48000)]
    public int sampleRate = 48000;
    [Range(512, 2048)]
    public uint chunkSize = 1024;

    // the current position of the playhead in the AudioClip
    private int m_streamPosition = 0;
    // the packet that is currently being played
    private VoiceChatPacket m_currentlyPlayingPacket;
    // our position in the packet that's being played.
    private int m_currentlyPlayingPacketSampleIndex = 0;
    // the last/current packet that was played.  if we get packets older than this, we can throw them out.
    private ulong m_lastPlayedPacketId = 0;

    private ulong m_lastSentPacketId = 0;

    void Start()
    {
        m_audioSource = GetComponent<AudioSource>();
        m_audioSource.loop = true;
        m_audioSource.clip = AudioClip.Create("VoiceChat", sampleRate, 1, sampleRate, true, OnAudioRead, OnAudioSetPosition);
        m_audioSource.Play();
    }

    void Update()
    {
        // if we're buffering, we're not anymore if we've gotten enough packets.
        if (Buffering)
        {
            Buffering = PacketQueue.Count < PacketBuffer;
        }

        if (isLocalPlayer)
        {
            //start or stop recording
            if (Input.GetKeyDown(voiceChatKey))
            {
                SteamUser.StartVoiceRecording();
            }
            if (Input.GetKeyUp(voiceChatKey))
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
                    VoiceChatPacket packet = new VoiceChatPacket();
                    m_lastSentPacketId++;
                    packet.PacketId = m_lastSentPacketId;
                    packet.Length = (int)BytesWritten;
                    packet.Data = DestBuffer;

                    CmdSendData(packet);

                    if (previewVoice)
                    {
                        OnNewSample(packet);
                    }
                }
            }
        }
    }

    void OnAudioRead(float[] data)
    {
        // wait til we have some packets saved up.
        if (Buffering)
        {
            // write out silence and gtfo
            int count = 0;
            while (count < data.Length)
            {
                data[count] = 0;
                m_streamPosition++;
                count++;
            }
        }
        // we've got enough packets, start writing them to the buffer
        else
        {

            // if we dont' have a packet to play, try grabbing the next one.
            if (m_currentlyPlayingPacket == null)
            {
                GrabNextPacket();
            }

            int count = 0;
            while (count < data.Length)
            {
                // start at silence, and fill it in with the correct value.
                float sample = 0;

                if (m_currentlyPlayingPacket != null)
                {
                    sample = m_currentlyPlayingPacket.DecodedData[m_currentlyPlayingPacketSampleIndex];

                    // increment our current packet's playhead now that we've just read a sample
                    m_currentlyPlayingPacketSampleIndex++;

                    // mark down the last packet that was played so that we have an idea of which incoming packets are obsolete.
                    m_lastPlayedPacketId = m_currentlyPlayingPacket.PacketId;

                    // if we've reached the end of this packet, grab the next one.
                    if (m_currentlyPlayingPacketSampleIndex >= m_currentlyPlayingPacket.DecodedData.Length)
                    {
                        GrabNextPacket();
                    }
                }

                // write the sample to the AudioClip & update it's position
                data[count] = sample;
                m_streamPosition++;
                count++;
            }
        }
    }

    void OnAudioSetPosition(int newPosition)
    {
        m_streamPosition = newPosition;
    }

    private void GrabNextPacket()
    {
        if (PacketQueue.Count > 0)
        {
            var pair = PacketQueue.First();
            VoiceChatPacket packet = pair.Value;
            if (packet != null)
            {
                m_currentlyPlayingPacket = packet;
                PacketQueue.Remove(m_currentlyPlayingPacket.PacketId);
            }
        }
        else
        {
            m_currentlyPlayingPacket = null;
            Buffering = true;
        }

        // reset the index.
        m_currentlyPlayingPacketSampleIndex = 0;
    }

    public void OnNewSample(VoiceChatPacket newPacket)
    {
        // throw out duplicates. this should never happen...
        if (PacketQueue.ContainsKey(newPacket.PacketId))
        {
            Debug.LogError("already have packet " + newPacket.PacketId + ". aborting");
            return;
        }

        // throw out old packets
        if (m_lastPlayedPacketId > newPacket.PacketId)
        {
            Debug.Log("throwing out old packet " + newPacket.PacketId);
            return;
        }

        // ignore silence
        if (newPacket.IsSilence)
        {
            return;
        }

        // convert immediately.
        if (newPacket.Decode(sampleRate))
        {
            // shove it into our queue.
            PacketQueue.Add(newPacket.PacketId, newPacket);
        }
        else
        {
            Debug.LogError("bad packet " + newPacket.PacketId + ". aborting");
        }
    }

    [Command(channel = 1)] //unreliable for speed
    private void CmdSendData(object message)
    {
        //get all players
        GameObject[] players = CustomNetworkManager.GetAllPlayers().Where(p => p != gameObject).ToArray();

        //only send to players within the audiosource's range.
        for (int p = 0; p < players.Length; p++)
        {
            if (Vector3.Distance(transform.position, players[p].transform.position) < m_audioSource.maxDistance)
            {
                TargetReceiveData(players[p].GetComponent<NetworkIdentity>().connectionToClient, message);
            }
        }
    }

    [TargetRpc(channel = 1)] //unreliable for speed
    private void TargetReceiveData(NetworkConnection connection, object message)
    {
        OnNewSample((VoiceChatPacket)message);
    }
}

[System.Serializable]
public class VoiceChatPacket
{
    public ulong PacketId;
    public int Length;
    public byte[] Data;
    public float[] DecodedData = null;
    public bool IsSilence = false;

    // decodes from steam's uncompressed format to the float format that unity likes
    // note that this might need to change somewhat if i mess around with the frequency.
    public bool Decode(int sampleRate)
    {
        byte[] decompressedData = new byte[sampleRate * 2];
        uint decompressedSize;
        EVoiceResult ret = SteamUser.DecompressVoice(Data, (uint)Length, decompressedData, (uint)decompressedData.Length, out decompressedSize, (uint)sampleRate);
        if (ret == EVoiceResult.k_EVoiceResultOK && decompressedSize > 0)
        {
            //convert data to mono float[]
            DecodedData = new float[decompressedSize / 2];
            for (int i = 0; i < DecodedData.Length; ++i)
            {
                DecodedData[i] = (short)(decompressedData[i * 2] | decompressedData[i * 2 + 1] << 8) / 32768.0f;
            }

            return true;
        }
        else
        {
            return false;
        }
    }
}