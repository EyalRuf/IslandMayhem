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
    public List<GameObject> players;

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
                if (!startingGame && CustomNetworkManager.singleton.numPlayers >= numberOfPlayersNeededToStart)
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
        players = new List<GameObject>(CustomNetworkManager.GetAllPlayers());
        players.Sort(new RandomizeComparer());

        int amountOfPlayersPerTeam = players.Count / numberOfTeams;

        for (var teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
        {
            Team t = new Team(teamIndex);
            for (var playerIndex = 0; playerIndex < amountOfPlayersPerTeam; playerIndex++)
            {
                GameObject currPlayer = players[(teamIndex * amountOfPlayersPerTeam) + playerIndex];
                t.playersInTeam.Add(currPlayer);
            }
            teams.Add(t);
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
}

public class RandomizeComparer : IComparer<GameObject>
{
    private readonly System.Random _random = new System.Random();

    public int Compare(GameObject x, GameObject y) => _random.Next(-1, 2);
}
