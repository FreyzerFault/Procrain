using System;
using UnityEngine;

namespace Procrain.Runtime.Utils
{
    public static class DebugTimer
    {
        public static void DebugTime(Action action, string message = "Time to run")
        {
            var iniTime = Time.realtimeSinceStartup;
            action();
            var endTime = Time.realtimeSinceStartup;
            var time = (endTime - iniTime) * 1000;
            var color = time < 10 ? "cyan" : time < 30 ? "green" : time < 60 ? "yellow" : "red";
            Debug.Log($"<b><color=white>{message}: <b><color={color}>{time:F0} ms</color></b></color></b>");
        }

        public static float RunTimerInMs(Action action)
        {
            var iniTime = Time.realtimeSinceStartup;
            action();
            var endTime = Time.realtimeSinceStartup;
            return (endTime - iniTime) * 1000;
        }
    }
}