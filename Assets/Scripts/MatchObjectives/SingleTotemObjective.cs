public class SingleTotemObjective : MatchObjective
{
    public Totem totem;

    public bool IsCompleted => totem != null && totem.maxVisualStageReached;
    public int MultiObjRequirement => totem == null ? 1 : totem.visuals.Length;
    public int MultiObjCurrIndex => totem == null ? 0 : totem.currentVisualStage;
    public string ObjectiveName => "Totem pieces: ";

    public string ProgressIndication => MultiObjCurrIndex + "/" + MultiObjRequirement;
}
