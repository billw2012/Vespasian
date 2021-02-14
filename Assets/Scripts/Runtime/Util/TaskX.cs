
using System;
using System.Threading.Tasks;

public static class TaskX
{
    /// <summary>
    /// Runs a function as a Task, if the platform supports it, otherwise it will run synchronously
    /// </summary>
    /// <param name="fn"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
#pragma warning disable 1998
    public static async Task<T> Run<T>(Func<T> fn)
#pragma warning restore 1998
    {
#if UNITY_WEBGL
        return fn();
#else
        return await Task.Run(fn);
#endif
    }
    
    /// <summary>
    /// Runs a function as a Task, if the platform supports it, otherwise it will run synchronously
    /// </summary>
    /// <param name="fn"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
#pragma warning disable 1998
    public static async Task RunAsync(Func<Task> fn)
#pragma warning restore 1998
    {
#if UNITY_WEBGL
        await fn();
#else
        await Task.Run(fn);
#endif
    }
    
    /// <summary>
    /// Runs a function as a Task, if the platform supports it, otherwise it will run synchronously
    /// </summary>
    /// <param name="fn"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
#pragma warning disable 1998
    public static async Task<T> RunAsync<T>(Func<Task<T>> fn)
#pragma warning restore 1998
    {
#if UNITY_WEBGL
        return await fn();
#else
        return await Task.Run(fn);
#endif
    }
    
    /// <summary>
    /// Runs an action as a Task, if the platform supports it, otherwise it will run synchronously
    /// </summary>
    /// <param name="fn"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
#pragma warning disable 1998
    public static async Task Run(Action fn)
#pragma warning restore 1998
    {
#if UNITY_WEBGL
        fn();
#else
        await Task.Run(fn);
#endif
    }
}
