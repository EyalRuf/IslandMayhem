using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using System.Linq;

public class TTTMatchManager : MatchManager
{
    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update(); //first do base

        if (!isServer)
            return;

        //check if we need to end the game
        if (!gameOver)
        {
            if (!endingGame)
            {
                //if either team won, start end sequence
                if (didTeam0Win || didTeam1Win)
                {
                    endingGame = true;
                    RpcEndGame(didTeam0Win ? 0 : 1);
                }
            }
        }
    }

    protected override void CalculateAndAssignTeams()
    {
        base.CalculateAndAssignTeams();
    }

    protected virtual void PopulateTeamObjectives()
    {
        List<Totem> totems0 = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Red).ToList();
        List<Totem> totems1 = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Blue).ToList();

        TotemsObjective team0Objective = new TotemsObjective();
        team0Objective.totems = totems0;
        team0Objectives = new List<MatchObjective>();
        team0Objectives.Add(team0Objective);

        TotemsObjective team1Objective = new TotemsObjective();
        team1Objective.totems = totems1;
        team1Objectives = new List<MatchObjective>();
        team1Objectives.Add(team1Objective);

        UpdateTeamAndObjectiveUI();
    }

    [ClientRpc]
    protected override void RpcStartGame()
    {
        //base.RpcStartGame(); DON'T CALL BASE. CUSTOM IMPlEMENTATION OF STARTGAME WON'T BE CALLED.

        PopulateTeamObjectives();
        StartCoroutine(StartGame());
    }

    protected override IEnumerator StartGame()
    {
        //count down
        yield return new WaitForSeconds(1f);
        gameStatus = "3";

        yield return new WaitForSeconds(1f);
        gameStatus = "2";

        yield return new WaitForSeconds(1f);
        gameStatus = "1";

        yield return new WaitForSeconds(1f);
        gameStatus = "GO!";

        yield return new WaitForSeconds(1f);

        //call base
        yield return StartCoroutine(base.StartGame());

        yield return null;
    }


    [ClientRpc]
    protected override void RpcEndGame(int teamIndex)
    {
        //base.RpcEndGame(); DON'T CALL BASE. CUSTOM IMPlEMENTATION OF STARTGAME WON'T BE CALLED.

        StartCoroutine(EndGame(teamIndex));
    }

    protected override IEnumerator EndGame(int teamIndex)
    {
        yield return StartCoroutine(base.EndGame(teamIndex)); //first call base. Game should end immediately.

        yield return new WaitForSeconds(1f);

        overviewText.color = teamColors[teamIndex];
        overviewText.text = teamNames[teamIndex] + " Win!";

        yield return new WaitForSeconds(5f);

        if (isClientOnly)
        {
            networkManager.StopClient();
        }

        if (isServer)
        {
            networkManager.StopHost();
        }

        if (isServerOnly)
        {
            networkManager.StopServer();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);

        yield return null;
    }

    void OnLevelWasLoaded(int level)
    {
        ResetMatch();
    }
}
