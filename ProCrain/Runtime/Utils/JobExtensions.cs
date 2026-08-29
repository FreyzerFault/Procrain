using System.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Procrain
{
    public static class JobExtensions
    {
        public static JobHandle ScheduleJob<T>(this JobHandle handle, T job)
            where T : struct, IJob
        {
            // If last Job didn't end, wait for it
            if (!handle.IsCompleted) handle.Complete();
            
            return job.Schedule();
        }
        
        
        public static IEnumerator WaitForJobToEnd(this JobHandle handle, bool debugTime, string debugInfo = "")
        {
            float time = Time.time;
            
            yield return new WaitWhile(() => !handle.IsCompleted);

            handle.Complete();

            if (debugTime)
                Debug.Log($"{(Time.time - time) * 1000:F1} ms\n{debugInfo}");
        }
    }
}
