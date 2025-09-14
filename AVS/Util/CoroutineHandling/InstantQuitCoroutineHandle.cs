namespace AVS.Util.CoroutineHandling
{
    internal class InstantQuitCoroutineHandle : ICoroutineHandle
    {
        public static InstantQuitCoroutineHandle Instance { get; } = new();
        public bool IsRunning => false;

        public void Stop()
        { }

        public object? WaitUntilDone() => null;
    }
}
