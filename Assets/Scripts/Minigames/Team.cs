using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Team
{
    public int teamNumber;
    public List<GameObject> playersInTeam;

    public Team(int number)
    {
        teamNumber = number;
        playersInTeam = new List<GameObject>();
    }
}
