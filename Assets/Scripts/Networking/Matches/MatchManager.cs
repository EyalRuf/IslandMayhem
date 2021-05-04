using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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
    public Color[] teamColors = { Color.red, Color.blue };
    public string[] teamNames = { "Red", "Blue" };
    public List<Team> teams;

    [HideInInspector]
    public List<MatchObjective> team0Objectives = new List<MatchObjective>();
    [HideInInspector]
    public List<MatchObjective> team1Objectives = new List<MatchObjective>();
    public bool didTeam0Win => team0Objectives?.Count > 0 && team0Objectives.TrueForAll(obj => obj.IsCompleted);
    public bool didTeam1Win => team1Objectives?.Count > 0 && team1Objectives.TrueForAll(obj => obj.IsCompleted);

    [Header("Start game")]
    public GameObject startGameArea;

    [Header("UI")]
    [SyncVar]
    public string gameStatus;
    public Text statusText;
    public Camera overviewCam;
    public Text overviewText;

    [HideInInspector]
    public CustomNetworkManager networkManager;

    protected virtual void Start()
    {
        //if (!isServer) // Server only
        //    return;

        // Set status
        gameStatus = "Waiting for players...";

        // Get network manager
        networkManager = FindObjectOfType<CustomNetworkManager>();
    }

    protected virtual void Update()
    {
        //dissapear if started
        startGameArea.SetActive(!gameStarted);
        statusText.text = gameStatus;

        if (!isServer) //server only
            return;

        if (!gameOver)
        {
            if (!gameStarted)
            {
                if (!startingGame)
                {
                    gameStatus = "Waiting for players... " + CustomNetworkManager.GetAllPlayers().Count + "/" + numberOfPlayersNeededToStart;

                    // Starting the game
                    // Has to come from customnetworkmanager
                    if (CustomNetworkManager.GetAllPlayers().Count >= numberOfPlayersNeededToStart && numberOfPlayersNeededToStart > 0)
                    {
                        startingGame = true;

                        //disallow joining
                        networkManager.AllowJoin(false);

                        gameStatus = "Starting game.";
                        CalculateAndAssignTeams();

                        //sync info
                        RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(teams)));
                        RpcStartGame(); //invoke sychronized start game sequence
                    }
                }
            }
        }
    }

    public virtual void UpdateTeamAndObjectiveUI()
    {
        CustomNetworkManager.GetLocalPlayer()?.GetComponent<NetworkPlayer>()?.localPlayer_TNO_UI?.RefreshUI();
    }

    public virtual void ResetMatch()
    {
        gameStarted = false;
        gameOver = false;
        gameStatus = "Waiting for players...";
        overviewCam.gameObject.SetActive(true);
        overviewText.text = "";

        teams = new List<Team>();
        team0Objectives = new List<MatchObjective>();
        team1Objectives = new List<MatchObjective>();
    }

    private void OnGUI()
    {
        //custom UI for casper
        if (Application.isEditor)
        {
            GUILayout.BeginArea(new Rect(Screen.width - 100, 0, 100, 25));

            if (GUILayout.Button("Start Game"))
            {
                startingGame = true;

                //disallow joining
                networkManager.AllowJoin(false);

                gameStatus = "Starting game.";
                CalculateAndAssignTeams();

                //sync info
                RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(teams)));
                RpcStartGame(); //invoke sychronized start game sequence
            }

            GUILayout.EndArea();
        }
    }

    protected virtual void CalculateAndAssignTeams()
    {
        if (!isServer) //server only to be sure
            return;

        teams = new List<Team>();
        List<NetworkIdentity> players = new List<NetworkIdentity>(CustomNetworkManager.GetAllPlayers());
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
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);

            //give team color
            np.teamColor = teamColors[team];
            np.playerTeam = team;

            //assign to team
            teams[team].playersInTeam.Add(np);
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
        //at start game
        if (isServer)
        {
            gameStarted = true;
            gameStatus = "";

            //spawn items
            ItemSpawner[] spawners = FindObjectsOfType<ItemSpawner>();

            foreach (ItemSpawner spawner in spawners)
            {
                if(spawner.enabled && spawner.gameObject.activeInHierarchy)
                {
                    spawner.Spawn();
                }
            }

            TagItemSpawner[] tagSpawners = FindObjectsOfType<TagItemSpawner>();

            foreach (TagItemSpawner spawner in tagSpawners)
            {
                if (spawner.enabled && spawner.gameObject.activeInHierarchy)
                {
                    spawner.Spawn();
                }
            }
        }

        yield return new WaitUntil(() => true); //required because we may not want to end coroutine.
    }

    [ClientRpc]
    protected virtual void RpcEndGame(int teamIndex)
    {
        StartCoroutine(EndGame(teamIndex));
    }

    protected virtual IEnumerator EndGame(int teamIndex)
    {
        if (isServer)
        {
            gameOver = true;
        }

        overviewCam.gameObject.SetActive(true);

        yield return new WaitUntil(() => true); //required because we may not want to end coroutine.
    }

    public int GetTeamIndexByPlayer(NetworkPlayer player)
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

public class RandomizeComparer : IComparer<object>
{
    private readonly System.Random _random = new System.Random();

    public int Compare(object x, object y) => _random.Next(-1, 2);
}