using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Team
{
    public int teamNumber;
    public List<string> playersInTeam;

    public Team()
    {
        teamNumber = 0;
        playersInTeam = new List<string>();
    }

    public Team(int number)
    {
        teamNumber = number;
        playersInTeam = new List<string>();
    }

    public bool IsInTeam(GameObject player)
    {
        return playersInTeam.Contains(player.name);
    }
}
