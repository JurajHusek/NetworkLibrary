using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Implementation of multithreading
/// </summary>
public class ThreadsController : MonoBehaviour
{
    private static bool executeOnMain = false;
    private static readonly List<Action> mainThreadTasks = new List<Action>();
    private static readonly List<Action> copiedMainThreadTasks = new List<Action>();

    private void FixedUpdate()
    {
        RefreshMainThread();
    }

    public static void RefreshMainThread()
    {
        if(executeOnMain == true)
        {
            copiedMainThreadTasks.Clear();
            lock(mainThreadTasks)
            {
                copiedMainThreadTasks.AddRange(mainThreadTasks);
                mainThreadTasks.Clear();
                executeOnMain = false;
            }
            foreach (Action act in copiedMainThreadTasks)
            {
                act();
            }
        }
    }
    public static void StartOnMainThread(Action toBeExecuted)
    {
        if(toBeExecuted == null)
        {
            return;
        } else
        {
            lock(mainThreadTasks)
            {
                mainThreadTasks.Add(toBeExecuted);
                executeOnMain = true;
            }
        }
    }
}
