public interface MatchObjective
{
    bool IsCompleted { get; }
    int MultiObjRequirement { get; }
    int MultiObjCurrIndex { get; }
    string ObjectiveName { get; }
    string ProgressIndication { get; }
}