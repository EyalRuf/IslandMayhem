using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TimerObjective : MatchObjective
{
    MatchTimer timer;

    public TimerObjective(MatchTimer timer)
    {
        this.timer = timer;
    }

    public bool IsCompleted => timer.IsTimeOver;

    public int MultiObjRequirement => 0;

    public int MultiObjCurrIndex => 0;

    public string ObjectiveName => "Prevent totem building";
    public string ProgressIndication => "";
}