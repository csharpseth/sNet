using UnityEngine;

public static class Log
{
    public static void Err(object e)
    {
        //Replace With however you want to do logging
        Debug.LogError(e);
    }

    public static void Err(object e, params object[] p)
    {
        //Replace With however you want to do logging
        Debug.LogErrorFormat(e.ToString(), p);
    }

    public static void Msg(object e)
    {
        //Replace With however you want to do logging
        Debug.Log(e);
    }

    public static void Msg(object e, params object[] p)
    {
        //Replace With however you want to do logging
        Debug.LogFormat(e.ToString(), p);
    }

}
