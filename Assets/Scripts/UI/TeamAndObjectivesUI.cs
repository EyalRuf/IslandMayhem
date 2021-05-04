using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;

public class TeamAndObjectivesUI : MonoBehaviour
{
    [Header("References")]
    public TTTMatchManager matchManager;
    public NetworkPlayer localPlayer;

    [Header("TeamUI")]
    public Image background;
    public TextMeshProUGUI teamNameText;

    [Header("ObjectivesUI")]
    public RectTransform container;
    public Transform objectiveList;
    public GameObject objectivePrefab;
    public float objListContainerBaseHeight = 180;
    public float objListContainerBasePosY = -110;
    public float objectiveListItemHeight = 60;
    public List<GameObject> currObjectivesListed = new List<GameObject>();

    private bool initialRefresh;

    // Use this for initialization
    void Start()
    {
        if (matchManager == null)
        {
            matchManager = FindObjectOfType<TTTMatchManager>();
        }

        currObjectivesListed = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (localPlayer == null && CustomNetworkManager.localPlayerInitialized)
        {
            localPlayer = CustomNetworkManager.GetLocalPlayer().GetComponent<NetworkPlayer>();
        }

        if (localPlayer != null )
        {
            if (!localPlayer.isLocalPlayer)
                return;

            if (!initialRefresh && matchManager.gameStarted && localPlayer.playerTeam != -1)
            {
                RefreshUI();
                initialRefresh = true;
            }
        }
    }

    void RecreateObjectiveList (List<MatchObjective> playerObjectives)
    {
        currObjectivesListed.ForEach(objGameObject => Destroy(objGameObject));
        currObjectivesListed = new List<GameObject>();

        // Setting container height and pos
        container.rect.Set(container.rect.x, objListContainerBasePosY - (objectiveListItemHeight / 2 * playerObjectives.Count),
            container.rect.width, objListContainerBaseHeight + objectiveListItemHeight * playerObjectives.Count);

        for (var i = 0; i < playerObjectives.Count; i++)
        {
            MatchObjective currObjective = playerObjectives[i];
            GameObject objectiveGO = Instantiate(objectivePrefab, objectiveList);
            ObjectiveUI objectiveUI = objectiveGO.GetComponent<ObjectiveUI>();
            objectiveUI.objective = currObjective;
            objectiveUI.objectiveItemHeight = objectiveListItemHeight;
            objectiveUI.objectiveIndex = i + 1;

            currObjectivesListed.Add(objectiveGO);
        }
    }

    public void RefreshUI ()
    {
        if (localPlayer.playerTeam != -1)
        {
            RefreshTeamUI();
            RefreshObjectiveUI();
        }
    }

    public void RefreshTeamUI ()
    {
        Color teamColor = matchManager.teamColors[localPlayer.playerTeam];
        background.color = new Color(teamColor.r, teamColor.g, teamColor.b, 25f/255f);
        teamNameText.text = "Team " + matchManager.teamNames[localPlayer.playerTeam];
    }

    public void RefreshObjectiveUI ()
    {
        List<MatchObjective> newPlayerObjectives = localPlayer.playerTeam == 0 ? matchManager.team0Objectives : matchManager.team1Objectives;

        RecreateObjectiveList(newPlayerObjectives);
    }
}
