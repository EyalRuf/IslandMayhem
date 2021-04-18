using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MatchManager : NetworkBehaviour
{
    [Header("MatchManager")]
    [SyncVar]
    public bool gameStarted;
    [HideInInspector]
    public bool startingGame = false;
    [SyncVar]
    public bool gameOver;
    [HideInInspector]
    public bool endingGame = false;
    public int numberOfPlayersNeededToStart;
    public int numberOfTeams;
    public List<Team> teams;
    public Color[] teamColors = { Color.red, Color.blue };

    protected virtual void Start()
    {

    }

    protected virtual void Update()
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
                    RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(teams)));
                    RpcStartGame(); //invoke sychronized start game sequence
                }
            }
        }
    }

    protected virtual void CalculateAndAssignTeams()
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

            //give team color
            player.GetComponent<NetworkPlayer>().teamColor = teamColors[team];

            //assign to team
            teams[team].playersInTeam.Add(player.name);
            team = Mathf.RoundToInt(Mathf.Repeat(++team, teams.Count));
        }
    }

    [ClientRpc]
    protected virtual void RpcStartGame()
    {
        StartCoroutine(StartGame());
    }

    protected virtual IEnumerator StartGame()
    {
        //at the end start game
        if (isServer)
        {
            gameStarted = true;
        }

        yield return new WaitUntil(() => true); //required because we may not want to end coroutine.
    }

    [ClientRpc]
    protected virtual void RpcEndGame()
    {
        StartCoroutine(EndGame());
    }

    protected virtual IEnumerator EndGame()
    {
        if (isServer)
        {
            gameOver = true;
        }

        yield return new WaitUntil(() => true); //required because we may not want to end coroutine.
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
        if(teams.Count <= 0)
        {
            teams = JsonUtility.FromJson<TeamInfo>(jsonTeamInfo).teams;
        }
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