using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class Cheats : NetworkBehaviour
{
    public bool showCheats;

    [Header("Coconut Machine Gun")]
    public float cmgCooldown;
    public GameObject coconut;
    private float cmgTimer;

    private Rect windowRect;
    private Vector2 itemScroll;
    private Texture2D pepega;
    private string username;

    private MatchService matchService;
    private MatchNetworkSync matchNetworkSync;
    private RandomEventSystem eventSystem;
    private CustomNetworkManager networkManager;

    private ThirdPersonCharacterController player;
    private ThirdPersonCameraController playerCamera;
    private PlayerItemInteractions playerItemInteractions;

    private bool coconutMachineGun;
    private string passwordEntered = "";

    private void Start()
    {
        matchService = FindObjectOfType<MatchService>();
        matchNetworkSync = FindObjectOfType<MatchNetworkSync>();
        eventSystem = FindObjectOfType<RandomEventSystem>();
        networkManager = FindObjectOfType<CustomNetworkManager>();
        windowRect = new Rect(Screen.width - 250, 0, 250, 250);
        pepega = (Texture2D)Resources.Load("pepega");
        username = System.Environment.UserName;
    }

    private void Update()
    {
        if ((Application.isEditor || Debug.isDebugBuild))
        {
            if (Input.GetKeyDown(KeyCode.F11))
            {
                showCheats = !showCheats;
            }

            if (showCheats)
            {
                if(player == null)
                {
                    NetworkIdentity playerId = CustomNetworkManager.GetLocalPlayer();
                    if (playerId != null)
                    {
                        player = playerId.GetComponent<ThirdPersonCharacterController>();
                        playerItemInteractions = playerId.GetComponent<PlayerItemInteractions>();
                    }
                }
            }

            if (coconutMachineGun && playerItemInteractions != null)
            {
                cmgTimer += Time.deltaTime;

                if(cmgTimer > cmgCooldown && Input.GetMouseButton(0))
                {
                    cmgTimer = 0;

                    ThrowableItem coconut = Instantiate(this.coconut, playerItemInteractions.itemHoldPos.position, Quaternion.identity).GetComponent<ThrowableItem>();
                    NetworkServer.Spawn(coconut.gameObject);

                    Vector3 throwTarget = player.transform.GetChild(1).forward;

                    Vector3 adjustedThrowVec = new Vector3(throwTarget.x * coconut.aimMultiplyerVec.x,
                        throwTarget.y * coconut.aimMultiplyerVec.y,
                        throwTarget.z * coconut.aimMultiplyerVec.z);

                    coconut.UseMain(coconut.transform.position, coconut.transform.rotation, coconut.rb.linearVelocity, (adjustedThrowVec * coconut.throwForce) + coconut.aimAdditionVec);
                }
            }
        }
    }

    private void OnGUI()
    {
        if ((Application.isEditor || Debug.isDebugBuild) && showCheats)
        {
            windowRect = GUILayout.Window(0, windowRect, DoCheatWindow, "Cheats");
        }
    }

    private void DoCheatWindow(int windowID)
    {
        if (passwordEntered == "Pepega" || Application.isEditor)
        {
            GUILayout.Label(pepega);
            GUILayout.Label("The pepega wishes you luck with development " + username + ". Pepega bless!");

            GUILayout.Label("Press F11 to toggle cheats.");

            if (isServer)
            {
                if (GUILayout.Button("Start Game") && matchService != null && matchNetworkSync != null)
                {
                    matchService.startingGame = true;
                    matchService.gameStatus = "Starting game.";
                    matchService.CalculateAndAssignTeams();
                    matchNetworkSync.RpcSyncTeamInfo(JsonUtility.ToJson(new TeamInfo(matchService.teams)));
                    matchNetworkSync.RpcStartGame();
                }

                if (networkManager != null && GUILayout.Button("Return to Menu"))
                    networkManager.StopHost();

                if (GUILayout.Button("Random event") && eventSystem != null)
                {
                    eventSystem.StartEvent(Random.Range(0, eventSystem.events.Length));
                }

                if (eventSystem != null)
                {
                    for (int e = 0; e < eventSystem.events.Length; e++)
                    {
                        if (GUILayout.Button("Start " + eventSystem.events[e].name))
                        {
                            eventSystem.StartEvent(e);
                        }
                    }
                }
            }

            if (player != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Grow"))
                {
                    player.transform.localScale *= 1.5f;
                    player.groundCheckDistance *= 1.5f;
                }
                if (GUILayout.Button("Shrink"))
                {
                    player.transform.localScale *= 0.75f;
                    player.groundCheckDistance *= 0.75f;
                }
                GUILayout.EndHorizontal();

                GUILayout.Label("Player movement speed:");
                player.baseMoveSpeed = GUILayout.HorizontalSlider(player.baseMoveSpeed, 0f, 100f);

                GUILayout.Label("Player jump force:");
                player.jumpForce = GUILayout.HorizontalSlider(player.jumpForce, 0f, 100f);
            }

            if (!isServer && NetworkClient.active && networkManager != null && GUILayout.Button("Return to Menu"))
                networkManager.StopClient();

            if (isServer)
            {
                coconutMachineGun = GUILayout.Toggle(coconutMachineGun, "Coconut machine gun:");
            }

            if (matchService?.networkManager != null && isServer)
            {
                GUILayout.Label("Spawn items:");

                itemScroll = GUILayout.BeginScrollView(itemScroll, GUILayout.Height(100));
                foreach (GameObject prefab in matchService.networkManager.spawnPrefabs)
                {
                    if (GUILayout.Button("Spawn " + prefab.name))
                    {
                        NetworkServer.Spawn(Instantiate(prefab, player.transform.position + Vector3.up * 25, Quaternion.identity));
                    }
                }
                GUILayout.EndScrollView();
            }

            GUI.DragWindow();
        }
        else
        {
            GUILayout.Label(pepega);
            GUILayout.Label("The pepega urges you to enter the password!");

            GUILayout.Label("Password:");
            passwordEntered = GUILayout.PasswordField(passwordEntered, '*');

            GUI.DragWindow();
        }
    }
}
