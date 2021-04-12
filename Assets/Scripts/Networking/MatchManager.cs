using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;

public class MatchManager : NetworkBehaviour
{
    [Header("General")]
    [SyncVar]
    public bool gameStarted;
    private bool startingGame;
    [SyncVar]
    public bool gameOver;
    public int numberOfPlayersNeededToStart;
    public int numberOfTeams;
    public List<Team> teams;

    private void Update()
    {
        if (!isServer) //server only
            return;

        if (!gameOver)
        {
            if (!gameStarted)
            {
                // Starting the game
                // Has to come from customnetworkmanager
                if (!startingGame && CustomNetworkManager.GetAllPlayers().Count >= numberOfPlayersNeededToStart && numberOfPlayersNeededToStart > 0)
                {
                    startingGame = true;
                    CalculateAndAssignTeams();
                }
            }
        }
    }

    private void CalculateAndAssignTeams()
    {
        if (!isServer) //server only to be sure
            return;

        teams = new List<Team>();
        List<GameObject> players = new List<GameObject>(CustomNetworkManager.GetAllPlayers());
        players.Sort(new RandomizeComparer());

        //add teams
        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            Team t = new Team(teamIndex);
            teams.Add(t);
        }

        //spread players over teams
        int team = 0;
        while (players.Count > 0)
        {
            //dequeue
            GameObject player = players[0];
            players.RemoveAt(0);

            teams[team].playersInTeam.Add(player.name);
            team = Mathf.RoundToInt(Mathf.Repeat(++team, teams.Count));
        }

        RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(teams)));
        StartGame();
    }

    protected virtual void StartGame()
    {
        gameStarted = true;
    }

    protected virtual void EndGame()
    {
        gameOver = true;
    }

    public int GetTeamIndexByPlayer(GameObject player)
    {
        for (int t = 0; t < teams.Count; t++)
        {
            if (teams[t].IsInTeam(player))
            {
                return t;
            }
        }

        return -1;
    }
    
    [ClientRpc]
    public void RpcSyncTeamInfo(string jsonTeamInfo) //manual synchronisation through an Rpc is required because of syncvar limitations
    {
        teams = JsonUtility.FromJson<TeamInfo>(jsonTeamInfo).teams;
    }
}

[System.Serializable]
public class TeamInfo
{
    public List<Team> teams;

    public TeamInfo(List<Team> teams)
    {
        this.teams = teams;
    }
}

public class RandomizeComparer : IComparer<GameObject>
{
    private readonly System.Random _random = new System.Random();

    public int Compare(GameObject x, GameObject y) => _random.Next(-1, 2);
}