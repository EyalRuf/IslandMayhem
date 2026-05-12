using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Networking;
using CardboardCore.DI;
using Mirror;
using UnityEngine;

[Injectable]
public class MatchService : MonoBehaviour
{
    [Header("Match Config")]
    public int numberOfPlayersNeededToStart;
    public int numberOfTeams;
    public Color[] teamColors = { Color.red, Color.blue };
    public Color neutralTeamColor = Color.white;
    public string[] teamNames = { "Natives", "Explorers" };
    public List<bool> hideColorsForTeam = new List<bool>() { false, false };

    [HideInInspector] public bool gameStarted;
    [HideInInspector] public bool startingGame;
    [HideInInspector] public bool gameOver;
    [HideInInspector] public bool endingGame;
    public string gameStatus;

    public List<Team> teams = new List<Team>();
    public List<MatchObjective> team0Objectives = new List<MatchObjective>();
    public List<MatchObjective> team1Objectives = new List<MatchObjective>();

    public bool didTeam0Win => team0Objectives?.Count > 0 && team0Objectives.TrueForAll(obj => obj.IsCompleted);
    public bool didTeam1Win => team1Objectives?.Count > 0 && team1Objectives.TrueForAll(obj => obj.IsCompleted);

    public event Action GameStartedEvent;
    public event Action<int> GameOverEvent;
    public event Action<string> CountdownTickEvent;
    public event Action<int> EndGameRequestedEvent;

    protected void InvokeGameStartedEvent() => GameStartedEvent?.Invoke();
    protected void InvokeGameOverEvent(int teamIndex) => GameOverEvent?.Invoke(teamIndex);
    protected void InvokeCountdownTickEvent(string tick) => CountdownTickEvent?.Invoke(tick);
    protected void InvokeEndGameRequestedEvent(int teamIndex) => EndGameRequestedEvent?.Invoke(teamIndex);

    [Inject] public CustomNetworkManager networkManager;
    [Inject] public SteamLobby steamLobby;

    protected bool IsServer => NetworkServer.active;
    protected bool IsClientOnly => NetworkClient.active && !NetworkServer.active;

    protected virtual void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void Update() { }

    public virtual void ResetMatch()
    {
        gameStarted = false;
        startingGame = false;
        gameOver = false;
        endingGame = false;
        gameStatus = "Waiting for players...";
        teams = new List<Team>();
        team0Objectives = new List<MatchObjective>();
        team1Objectives = new List<MatchObjective>();
    }

    public virtual void CalculateAndAssignTeams()
    {
        if (!IsServer) return;

        teams = new List<Team>();
        List<NetworkIdentity> players = new List<NetworkIdentity>(CustomNetworkManager.GetAllPlayers());
        players.Sort(new RandomizeComparer());

        for (int teamIndex = 0; teamIndex < numberOfTeams; teamIndex++)
            teams.Add(new Team(teamIndex));

        int team = 0;
        while (players.Count > 0)
        {
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);
            np.teamColor = teamColors[team];
            np.playerTeam = team;
            teams[team].playersInTeam.Add(np);
            team = Mathf.RoundToInt(Mathf.Repeat(++team, teams.Count));
        }
    }

    public virtual void UpdateTeamAndObjectiveUI()
    {
        CustomNetworkManager.GetLocalPlayer()?.GetComponent<NetworkPlayer>()?.localPlayer_TNO_UI?.RefreshUI();
    }

    public virtual string GetTimerString() => string.Empty;

    public virtual void OnRpcStartGame()
    {
        StartCoroutine(StartGameSequence());
    }

    protected virtual IEnumerator StartGameSequence()
    {
        if (IsServer)
        {
            gameStarted = true;
            gameStatus = string.Empty;
        }
        InvokeGameStartedEvent();
        yield return null;
    }

    public virtual void OnRpcEndGame(int teamIndex)
    {
        StartCoroutine(EndGameSequence(teamIndex));
    }

    protected virtual IEnumerator EndGameSequence(int teamIndex)
    {
        if (IsServer)
            gameOver = true;

        yield return null;
    }

    public void HandleSyncTeamInfo(string jsonTeamInfo)
    {
        if (teams == null || teams.Count <= 0)
            teams = JsonUtility.FromJson<TeamInfo>(jsonTeamInfo).teams;
    }
}

[System.Serializable]
public class TeamInfo
{
    public List<Team> teams;
    public TeamInfo(List<Team> teams) { this.teams = teams; }
}

public class RandomizeComparer : IComparer<object>
{
    public int Compare(object x, object y) => UnityEngine.Random.Range(-1, 2);
}
