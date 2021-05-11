using UnityEngine;
using System.Collections;
using TMPro;

public class ObjectiveUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform rectTransform;
    public TextMeshProUGUI objNameText;
    public TextMeshProUGUI objProgressText;

    [Header("Objective")]
    public MatchObjective objective;

    [Header("Positioning")]
    public float objectiveItemHeight;
    public int objectiveIndex;

    void Start()
    {
        rectTransform.rect.Set(rectTransform.rect.x, -(objectiveItemHeight / 2 * objectiveIndex),
            rectTransform.rect.width, objectiveItemHeight * objectiveIndex);

        objNameText.text = objective.ObjectiveName;
    }

    void Update()
    {
        objProgressText.text = objective.ProgressIndication;

        objNameText.fontStyle = objective.IsCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
        objNameText.fontStyle = objective.IsCompleted ? FontStyles.Strikethrough : FontStyles.Normal;
    }
}
