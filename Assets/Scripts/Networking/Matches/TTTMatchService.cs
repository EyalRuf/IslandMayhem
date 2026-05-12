using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class TTTMatchService : MatchService
{
    protected virtual void PopulateTeamObjectives()
    {
        List<Totem> totems0 = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Red).ToList();
        List<Totem> totems1 = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Blue).ToList();

        TotemsObjective team0Objective = new TotemsObjective();
        team0Objective.totems = totems0;
        team0Objectives = new List<MatchObjective> { team0Objective };

        TotemsObjective team1Objective = new TotemsObjective();
        team1Objective.totems = totems1;
        team1Objectives = new List<MatchObjective> { team1Objective };

        UpdateTeamAndObjectiveUI();
    }

    protected override void Update()
    {
        base.Update();

        if (!IsServer || !gameStarted || gameOver || endingGame) return;

        if (didTeam0Win || didTeam1Win)
        {
            endingGame = true;
            InvokeEndGameRequestedEvent(didTeam0Win ? 0 : 1);
        }
    }

    public override void OnRpcStartGame()
    {
        PopulateTeamObjectives();
        StartCoroutine(StartGameSequence());
    }

    protected override IEnumerator StartGameSequence()
    {
        var wait = new WaitForSeconds(1f);

        yield return wait;
        gameStatus = "3";
        InvokeCountdownTickEvent("3");

        yield return wait;
        gameStatus = "2";
        InvokeCountdownTickEvent("2");

        yield return wait;
        gameStatus = "1";
        InvokeCountdownTickEvent("1");

        yield return wait;
        gameStatus = "GO!";
        InvokeCountdownTickEvent("GO!");

        yield return wait;

        yield return StartCoroutine(base.StartGameSequence());
    }

    public override void OnRpcEndGame(int teamIndex)
    {
        StartCoroutine(EndGameSequence(teamIndex));
    }

    protected override IEnumerator EndGameSequence(int teamIndex)
    {
        yield return StartCoroutine(base.EndGameSequence(teamIndex));

        yield return new WaitForSeconds(1f);

        // Fire event — MatchNetworkSync shows win text, SM transitions to GameOverState
        InvokeGameOverEvent(teamIndex);

        yield return new WaitForSeconds(5f);

        if (IsClientOnly)
            networkManager.StopClient();

        if (IsServer)
            networkManager.StopHost();

        if (NetworkServer.active && !NetworkClient.active)
            networkManager.StopServer();
    }
}
