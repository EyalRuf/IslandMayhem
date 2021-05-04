using UnityEngine;
using System.Collections;

public class EnabledForPlayersOfTeam : MonoBehaviour
{
    NetworkPlayer localplayer;

    [Tooltip("-1 for both teams, 0 for Red, 1 for Blue")]
    public int team;

    // Update is called once per frame
    void Update()
    {
        if (team > -1) 
        {
            if (localplayer == null && CustomNetworkManager.localPlayerInitialized)
            {
                localplayer = CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkPlayer>();
            }

            if (localplayer != null && localplayer.playerTeam != -1)
            {
                gameObject.SetActive(team == localplayer.playerTeam);
            }
        }
    }
}
