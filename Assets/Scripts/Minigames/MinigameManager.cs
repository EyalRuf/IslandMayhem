using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    [Header("MinigameManager")]
    public bool gameStarted;
    private bool startingGame;
    public bool gameOver;
    public int numberOfPlayersNeededToStart;
    public int numberOfTeams;
    public List<Team> teams;
    public List<NetworkIdentity> players;

    [Header("Timer")]
    public float minigameTime = 60000; // 60 seconds
    private float timer;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {
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
            } else
            {
                // Checking for game end
                timer += Time.deltaTime;
                if (timer >= minigameTime)
                {
                    EndGame();
                }
            }
        }
    }

    protected virtual void CalculateAndAssignTeams()
    {
        teams = new List<Team>();
        List<NetworkIdentity> players = new List<NetworkIdentity>(CustomNetworkManager.GetAllPlayers());
        players.Sort(new RandomizeComparer());

        //for editor
        this.players = players;

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
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);

            teams[team].playersInTeam.Add(np);
            np.playerTeam = team;

            team = Mathf.RoundToInt(Mathf.Repeat(++team, teams.Count));
        }

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

    public int GetTeamIndexByPlayer(NetworkPlayer player)
    {
        for(int t = 0; t < teams.Count; t++)
        {
            if (teams[t].IsInTeam(player))
            {
                return t;
            }
        }

        return -1;
    }
}
