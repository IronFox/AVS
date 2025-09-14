using AVS.Log;
using UnityEngine;

namespace AVS.Util
{
    internal record CoroutineHandle(SmartLog Log, Coroutine Coroutine, RootModController StartedOn) : ICoroutineHandle
    {
        public bool IsRunning { get; private set; } = true;

        public void SignalStop()
        {
            //Logger.Log($"[{Log.Id}] SignalStop");
            IsRunning = false;
            Log.Dispose();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;
            Log.Debug($"Coroutine manually stopped.");
            StartedOn.StopCoroutine(Coroutine);
            SignalStop();
        }

        public object? WaitUntilDone()
            => new WaitUntil(() => !IsRunning);
    }
}
