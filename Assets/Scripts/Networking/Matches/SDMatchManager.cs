using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;

public class SDMatchManager : TTTMatchManager
{
    [Header("SDMatchManager")]
    public MatchTimer matchTimer;

    protected override void PopulateTeamObjectives()
    {
        List<Totem> totems0 = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Blue).ToList();

        TotemsObjective team0Objective = new TotemsObjective();
        team0Objective.totems = totems0;
        team0Objectives = new List<MatchObjective>();
        team0Objectives.Add(team0Objective);

        TimerObjective team1Objective = new TimerObjective(matchTimer);
        team1Objectives = new List<MatchObjective>();
        team1Objectives.Add(team1Objective);

        UpdateTeamAndObjectiveUI();
    }

    protected override void CalculateAndAssignTeams()
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

        // Natives
        while (players.Count > players.Count - (players.Count * 0.25f))
        {
            //dequeue
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);

            //give team color
            np.teamColor = teamColors[0];
            np.playerTeam = 0;

            //assign to team
            teams[0].playersInTeam.Add(np);
        }

        // Explorers
        while (players.Count > 0)
        {
            //dequeue
            NetworkIdentity player = players[0];
            NetworkPlayer np = player.GetComponent<NetworkPlayer>();
            players.RemoveAt(0);

            //give team color
            np.teamColor = teamColors[1];
            np.playerTeam = 1;

            //assign to team
            teams[1].playersInTeam.Add(np);
        }
    }

    protected override IEnumerator StartGame()
    {
        yield return base.StartGame();

        matchTimer.MatchStarted();

        yield return null;
    }
}
