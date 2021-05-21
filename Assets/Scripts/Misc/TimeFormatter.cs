using UnityEngine;

public static class TimeFormatter
{
    public static string Format(float timer)
    {
        return string.Format("{0:#00}:{1:00}",
            Mathf.Floor(timer / 60), //minutes
            Mathf.Floor((timer) % 60) + 1f);//seconds
    }
}
