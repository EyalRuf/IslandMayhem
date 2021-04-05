using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class Team
{
    public int teamNumber;
    public List<GameObject> playersInTeam;

    public Team(int number)
    {
        teamNumber = number;
        playersInTeam = new List<GameObject>();
    }

    public bool IsInTeam(GameObject player)
    {
        return playersInTeam.Contains(player);
    }
}
