using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class SingelTotemMatchService : TTTMatchService
{
    private MatchTimer matchTimer;
    private CampManager cm;

    protected override void Update()
    {
        base.Update();

        if (!IsServer || !gameStarted || gameOver || endingGame) return;

        if (matchTimer != null && matchTimer.IsTimeOver)
        {
            endingGame = true;
            int team0Pieces = team0Objectives.Count > 0 ? team0Objectives[0].MultiObjCurrIndex : 0;
            int team1Pieces = team1Objectives.Count > 0 ? team1Objectives[0].MultiObjCurrIndex : 0;
            InvokeEndGameRequestedEvent(team0Pieces > team1Pieces ? 0 : team1Pieces > team0Pieces ? 1 : -1);
        }
    }

    protected override void PopulateTeamObjectives()
    {
        Totem redTotem = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Red).FirstOrDefault();
        SingleTotemObjective team0Objective = new SingleTotemObjective();
        team0Objective.totem = redTotem;
        team0Objectives = new List<MatchObjective> { team0Objective };

        Totem blueTotem = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Blue).FirstOrDefault();
        SingleTotemObjective team1Objective = new SingleTotemObjective();
        team1Objective.totem = blueTotem;
        team1Objectives = new List<MatchObjective> { team1Objective };

        UpdateTeamAndObjectiveUI();
    }

    public override void CalculateAndAssignTeams()
    {
        if (!IsServer) return;

        teams = new List<Team>();
        List<NetworkIdentity> players = new List<NetworkIdentity>(CustomNetworkManager.GetAllPlayers());
        players.Sort(new RandomizeComparer());

        Team natives = new Team(0);
        Team explorers = new Team(1);
        teams.Add(natives);
        teams.Add(explorers);
        int teamIndex = 0;

        while (players.Count > 0)
        {
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);
            np.playerTeam = teamIndex;
            np.teamColor = teamColors[teamIndex];
            teams[teamIndex].playersInTeam.Add(np);
            teamIndex = (teamIndex + 1) % numberOfTeams;
        }
    }

    public override string GetTimerString() =>
        matchTimer != null ? matchTimer.GetMatchTimeString : string.Empty;

    public override void OnRpcStartGame()
    {
        // Resolve scene-specific refs now that match scene is loaded
        matchTimer = FindObjectOfType<MatchTimer>();
        cm = FindObjectOfType<CampManager>();
        base.OnRpcStartGame();
    }

    protected override IEnumerator StartGameSequence()
    {
        yield return StartCoroutine(base.StartGameSequence()); // countdown + base start (fires GameStartedEvent)
        matchTimer?.MatchStarted();
        cm?.SwapMainCamp();
    }

    protected override IEnumerator EndGameSequence(int teamIndex)
    {
        matchTimer?.MatchEnd();
        yield return StartCoroutine(base.EndGameSequence(teamIndex));
    }

    public override void ResetMatch()
    {
        base.ResetMatch();
        matchTimer?.ResetMatch();
    }
}
