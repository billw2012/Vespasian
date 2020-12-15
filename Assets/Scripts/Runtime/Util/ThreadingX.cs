using System;
using System.Threading.Tasks;

public class ThreadingX
{
    public static void RunOnUnityThread(Action action) =>
        Task.Run(async () =>
        {
            await new WaitForUpdate();
            action();
        });

    public static async Task RunOnUnityThreadAsync(Action action) =>
        await Task.Run(async () =>
        {
            await new WaitForUpdate();
            action();
        });
}
