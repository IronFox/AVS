using AVS.Log;
using UnityEngine;

namespace AVS.Util.CoroutineHandling
{
    internal class CoroutineHandle : ICoroutineHandle
    {
        public CoroutineHandle(SmartLog log, Coroutine coroutine, RootModController startedOn)
        {
            if (coroutine.IsNull())
                throw new System.ArgumentNullException(nameof(coroutine));
            Log = log;
            Coroutine = coroutine;
            StartedOn = startedOn;
        }
        public bool IsRunning { get; private set; } = true;
        public SmartLog Log { get; }
        public Coroutine Coroutine { get; }
        public RootModController StartedOn { get; }

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
