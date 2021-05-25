using System.Collections.Generic;

public class TotemsObjective : MatchObjective
{
    public List<Totem> totems = new List<Totem>();

    public bool IsCompleted => totems.Count > 0 && totems.TrueForAll(totem => totem.maxVisualStageReached);
    public int MultiObjRequirement => totems.Count;
    public int MultiObjCurrIndex => totems.FindAll(totem => totem.maxVisualStageReached).Count;
    public string ObjectiveName => "Build Totem/s";

    public string ProgressIndication => MultiObjCurrIndex + "/" + MultiObjRequirement;
}
