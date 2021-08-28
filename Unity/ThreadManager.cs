using System;
using System.Collections.Generic;
using UnityEngine;

public class ThreadManager : MonoBehaviour
{
    private static readonly List<Action> m_executeOnMainThread = new List<Action>();
    private static readonly List<Action> m_executeCopiedOnMainThread = new List<Action>();
    private static bool m_actionToExecuteOnMainThread = false;

    private void Update()
    {
        if (m_actionToExecuteOnMainThread)
        {
            m_executeCopiedOnMainThread.Clear();
            lock (m_executeOnMainThread)
            {
                m_executeCopiedOnMainThread.AddRange(m_executeOnMainThread);
                m_executeOnMainThread.Clear();
                m_actionToExecuteOnMainThread = false;
            }

            for (int i = 0; i < m_executeCopiedOnMainThread.Count; i++)
            {
                m_executeCopiedOnMainThread[i]();
            }
        }
    }

    /// <summary>Sets an action to be executed on the main thread.</summary>
    /// <param name="action">The action to be executed on the main thread.</param>
    public static void ExecOnMain(Action action)
    {
        if (action == null)
        {
            Log.Msg("ThreadManager", "No action to execute on main thread!");
            return;
        }

        lock (m_executeOnMainThread)
        {
            m_executeOnMainThread.Add(action);
            m_actionToExecuteOnMainThread = true;
        }
    }
}