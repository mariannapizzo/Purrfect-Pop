using System;
using UnityEngine;

public static class Actions
{
    public static Action<string, Vector3> OnObjectTouched;
    public static Action<string> OnEvent;
    public static Action OnQuit;
    public static Action OnDemoEnd;
    public static Action GoOn;
}