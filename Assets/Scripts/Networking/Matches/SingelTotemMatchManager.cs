using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;

public class SingelTotemMatchManager : TTTMatchManager
{
    [Header("SingelTotemMatchManager")]
    public MatchTimer matchTimer;

    protected override void Update()
    {
        base.Update();

        if (!isServer)
            return;

        if (matchTimer.IsTimeOver)
        {
            RpcEndGame(-1);
        }
    }

    protected override void PopulateTeamObjectives()
    {
        Totem redTotem = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Red).FirstOrDefault();
        SingleTotemObjective team0Objective = new SingleTotemObjective();
        team0Objective.totem = redTotem;
        team0Objectives = new List<MatchObjective>();
        team0Objectives.Add(team0Objective);

        Totem blueTotem = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Blue).FirstOrDefault();
        SingleTotemObjective team1Objective = new SingleTotemObjective();
        team1Objective.totem = blueTotem;
        team1Objectives = new List<MatchObjective>();
        team1Objectives.Add(team1Objective);

        UpdateTeamAndObjectiveUI();
    }

    public override void CalculateAndAssignTeams()
    {
        if (!isServer) //server only to be sure
            return;

        teams = new List<Team>();
        List<NetworkIdentity> players = new List<NetworkIdentity>(CustomNetworkManager.GetAllPlayers());
        players.Sort(new RandomizeComparer());

        Team natives = new Team(0);
        Team explorers = new Team(1);
        teams.Add(natives);
        teams.Add(explorers);
        int teamIndex = 0;

        // Natives
        while (players.Count > 0)
        {
            //dequeue
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);

            //give team color
            np.playerTeam = teamIndex;
            np.teamColor = teamColors[teamIndex];

            //assign to team
            teams[teamIndex].playersInTeam.Add(np);
            teamIndex = (teamIndex + 1) % numberOfTeams;
        }
    }

    protected override IEnumerator StartGame()
    {
        yield return base.StartGame();

        matchTimer.MatchStarted();

        yield return null;
    }

    protected override IEnumerator EndGame(int teamIndex)
    {
        matchTimer.MatchEnd();
        yield return base.EndGame(teamIndex);
    }

    public override void ResetMatch()
    {
        base.ResetMatch();
        matchTimer.ResetMatch();
    }
}
