using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class MatchNetworkSync : NetworkBehaviour
{
    [Header("Scene Refs")]
    public GameObject startGameArea;
    public TextMeshProUGUI statusText;
    public Camera overviewCam;
    public TextMeshProUGUI overviewText;

    [SyncVar(hook = nameof(OnGameStartedSynced))]
    public bool gameStarted;

    [SyncVar(hook = nameof(OnGameOverSynced))]
    public bool gameOver;

    [SyncVar(hook = nameof(OnGameStatusSynced))]
    public string gameStatus;

    internal MatchService matchService;

    public void Initialize(MatchService service)
    {
        matchService = service;
        matchService.GameOverEvent += OnGameOver;
        matchService.EndGameRequestedEvent += OnEndGameRequested;

        CustomNetworkManager nm = FindObjectOfType<CustomNetworkManager>();
        if (nm != null) nm.matchNetworkSync = this;
    }

    private void OnDestroy()
    {
        if (matchService == null) return;
        matchService.GameOverEvent -= OnGameOver;
        matchService.EndGameRequestedEvent -= OnEndGameRequested;
    }

    private void Update()
    {
        if (matchService == null) return;

        startGameArea.SetActive(!gameStarted);
        statusText.text = gameStatus;

        if (!isServer) return;

        // Mirror SyncVars from service (server is source of truth)
        gameStarted = matchService.gameStarted;
        gameOver = matchService.gameOver;
        gameStatus = matchService.gameStatus;

        if (!matchService.gameOver && !matchService.gameStarted && !matchService.startingGame)
        {
            int playerCount = CustomNetworkManager.GetAllPlayers().Count;
            matchService.gameStatus = "Waiting for players... " + playerCount + "/" + matchService.numberOfPlayersNeededToStart;

            if ((playerCount >= matchService.numberOfPlayersNeededToStart && matchService.numberOfPlayersNeededToStart > 0)
                || Input.GetKeyDown(KeyCode.Return))
            {
                matchService.startingGame = true;
                matchService.gameStatus = "Starting game.";
                matchService.CalculateAndAssignTeams();
                RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(matchService.teams)));
                RpcStartGame();
            }
        }
    }

    private void OnEndGameRequested(int teamIndex) => RpcEndGame(teamIndex);

    private void OnGameOver(int teamIndex)
    {
        overviewCam.gameObject.SetActive(true);
        overviewText.color = teamIndex == -1 ? matchService.neutralTeamColor : matchService.teamColors[teamIndex];
        overviewText.text = teamIndex == -1 ? "Draw" : matchService.teamNames[teamIndex] + " Win!";
    }

    [ClientRpc]
    public void RpcStartGame()
    {
        OnMatchStartStop[] callbacks = FindObjectsOfType<OnMatchStartStop>();
        foreach (OnMatchStartStop cb in callbacks)
            cb.OnMatchStart();

        if (isServer)
        {
            ItemSpawner[] spawners = FindObjectsOfType<ItemSpawner>();
            foreach (ItemSpawner spawner in spawners)
                if (spawner.enabled && spawner.gameObject.activeInHierarchy)
                    spawner.Spawn();

            TagItemSpawner[] tagSpawners = FindObjectsOfType<TagItemSpawner>();
            foreach (TagItemSpawner spawner in tagSpawners)
                if (spawner.enabled && spawner.gameObject.activeInHierarchy)
                    spawner.Spawn();
        }

        matchService.OnRpcStartGame();
    }

    [ClientRpc]
    public void RpcEndGame(int teamIndex)
    {
        OnMatchStartStop[] callbacks = FindObjectsOfType<OnMatchStartStop>();
        foreach (OnMatchStartStop cb in callbacks)
            cb.OnMatchStop();

        matchService.OnRpcEndGame(teamIndex);
    }

    [ClientRpc]
    public void RpcSyncTeamInfo(string jsonTeamInfo)
    {
        matchService.HandleSyncTeamInfo(jsonTeamInfo);
    }

    public void ResetMatchSync()
    {
        if (overviewText != null) overviewText.text = string.Empty;
        if (overviewCam != null) overviewCam.gameObject.SetActive(true);
    }

    // SyncVar hooks — keep matchService fields in sync on clients
    private void OnGameStartedSynced(bool oldVal, bool newVal)
    {
        if (matchService != null && !isServer) matchService.gameStarted = newVal;
    }

    private void OnGameOverSynced(bool oldVal, bool newVal)
    {
        if (matchService != null && !isServer) matchService.gameOver = newVal;
    }

    private void OnGameStatusSynced(string oldVal, string newVal)
    {
        if (matchService != null && !isServer) matchService.gameStatus = newVal;
    }
}
