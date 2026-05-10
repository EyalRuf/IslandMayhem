using CardboardCore.DI;
using Steamworks;
using UnityEngine;

[Injectable]
public class SessionData
{
    private const string KEY_LOBBY_ID = "SavedLobbyId";
    private const string KEY_HOST_STEAM_ID = "SavedHostSteamId";

    public CSteamID SavedLobbyId { get; set; }
    public string SavedHostSteamId { get; set; }

    public bool HasSavedSession => SavedLobbyId != CSteamID.Nil;

    public void Save()
    {
        PlayerPrefs.SetString(KEY_LOBBY_ID, SavedLobbyId.m_SteamID.ToString());
        PlayerPrefs.SetString(KEY_HOST_STEAM_ID, SavedHostSteamId);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        string lobbyId = PlayerPrefs.GetString(KEY_LOBBY_ID, "0");
        SavedLobbyId = new CSteamID(ulong.Parse(lobbyId));
        SavedHostSteamId = PlayerPrefs.GetString(KEY_HOST_STEAM_ID, string.Empty);
    }

    public void Clear()
    {
        SavedLobbyId = CSteamID.Nil;
        SavedHostSteamId = string.Empty;
        PlayerPrefs.DeleteKey(KEY_LOBBY_ID);
        PlayerPrefs.DeleteKey(KEY_HOST_STEAM_ID);
        PlayerPrefs.Save();
    }
}
