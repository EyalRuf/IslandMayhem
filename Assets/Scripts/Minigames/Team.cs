using System.Collections.Generic;

[System.Serializable]
public class Team
{
    public int teamNumber;
    public List<NetworkPlayer> playersInTeam;

    public Team()
    {
        teamNumber = 0;
        playersInTeam = new List<NetworkPlayer>();
    }

    public Team(int number)
    {
        teamNumber = number;
        playersInTeam = new List<NetworkPlayer>();
    }

    public bool IsInTeam(NetworkPlayer player)
    {
        return playersInTeam.Find(p => p.netId == player.netId) != null;
    }
}
