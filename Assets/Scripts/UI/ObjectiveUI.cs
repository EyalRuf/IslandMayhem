using UnityEngine;
using System.Collections;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform rectTransform;
    public TextMeshProUGUI objNameText;
    public TextMeshProUGUI objFinishProgressText;

    [Header("Objective")]
    public MatchObjective objective;

    [Header("Positioning")]
    public float objectiveItemHeight;
    public int objectiveIndex;

    // Use this for initialization
    void Start()
    {
        rectTransform.rect.Set(rectTransform.rect.x, -(objectiveItemHeight / 2 * objectiveIndex),
            rectTransform.rect.width, objectiveItemHeight * objectiveIndex);

        objNameText.text = objective.ObjectiveName;
    }

    void Update()
    {
        if (objective.ObjectiveType == ObjectiveType.multiple)
        {
            objFinishProgressText.text = objective.MultiObjCurrIndex + "/" + objective.MultiObjRequirement;
        }
        else
        {
            objFinishProgressText.text = objective.IsCompleted ? "[x]" : "[]";
        }

        objNameText.fontStyle = objective.IsCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
        objNameText.fontStyle = objective.IsCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
    }
}
