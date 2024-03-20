using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace Procrain.Runtime.Utils.Threading
{
    public class MainThreadDispatcher : SingletonPersistent<MainThreadDispatcher>
    {
        private static readonly ConcurrentQueue<Action> Actions = new();
        private static readonly ConcurrentQueue<Action> LowPriorityActions = new();
        [SerializeField] private int maxLowPriorityActionsPerFrame = 10;

        private void Update()
        {
            while (Actions.TryDequeue(out var action))
                action.Invoke();

            for (var i = 0; i < maxLowPriorityActionsPerFrame; i++)
                if (LowPriorityActions.TryDequeue(out var lowPriorityAction))
                    lowPriorityAction.Invoke();
                else
                    break;
        }

        public static void Dispatch(Action action)
        {
            Actions.Enqueue(action);
        }

        public static void DispatchLowPriority(Action action)
        {
            LowPriorityActions.Enqueue(action);
        }
    }
}