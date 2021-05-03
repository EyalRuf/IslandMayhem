public interface MatchObjective
{
    bool IsCompleted { get; }
    ObjectiveType ObjectiveType { get; }
    int MultiObjRequirement { get; }
    int MultiObjCurrIndex { get; }
    string ObjectiveName { get; }
}

public enum ObjectiveType
{
    single,
    multiple
}