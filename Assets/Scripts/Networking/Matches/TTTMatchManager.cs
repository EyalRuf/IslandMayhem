using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class TTTMatchManager : MatchManager
{
    [Header("Two Team Totems")]
    public Totem[] team0Totems;
    public bool team0Won;
    public Totem[] team1Totems;
    public bool team1Won;

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
                //check if team 0 won
                team0Won = team0Totems.Length > 0;

                foreach (Totem team0Totem in team0Totems)
                {
                    if (!team0Totem.maxVisualStageReached)
                    {
                        team0Won = false;
                        break;
                    }
                }

                //check if team 1 won
                team1Won = team1Totems.Length > 0;

                foreach (Totem team1Totem in team1Totems)
                {
                    if (!team1Totem.maxVisualStageReached)
                    {
                        team1Won = false;
                        break;
                    }
                }

                //if either team won, start end sequence
                if (team0Won || team1Won)
                {
                    endingGame = true;
                    RpcEndGame();
                }
            }
        }
    }

    [ClientRpc]
    protected override void RpcStartGame()
    {
        //base.RpcStartGame(); DON'T CALL BASE. CUSTOM IMPlEMENTATION OF STARTGAME WON'T BE CALLED.

        //if server, find totems
        if (isServer)
        {
            //find totems
            team0Totems = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Red).ToArray();
            team1Totems = FindObjectsOfType<Totem>().Where(t => t.group == TotemPieceGroup.Blue).ToArray();
        }

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
    protected override void RpcEndGame()
    {
        //base.RpcEndGame(); DON'T CALL BASE. CUSTOM IMPlEMENTATION OF STARTGAME WON'T BE CALLED.

        StartCoroutine(EndGame());
    }

    protected override IEnumerator EndGame()
    {
        yield return StartCoroutine(base.EndGame()); //first call base. Game should end immediately.

        yield return null;
    }
}
