using System;
using System.Threading.Tasks;

public class ThreadingX
{
#pragma warning disable 1998
    public static async Task RunOnUnityThreadAsync(Action action)
#pragma warning restore 1998
    {
#if UNITY_WEBGL
        // There is only one thread on WebGL
        action();
#else
        await Task.Run(async () =>
        {
            await Awaiters.NextFrame;
            action();
        });
#endif
    }
}
