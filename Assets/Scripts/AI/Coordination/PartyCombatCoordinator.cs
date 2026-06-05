using System.Collections.Generic;
using UnityEngine;

public static class PartyCombatCoordinator
{
    private static readonly Dictionary<int, int> FocusCounts = new Dictionary<int, int>();

    public static void RegisterFocus(Transform target)
    {
        if (target == null) return;
        int id = target.GetInstanceID();
        FocusCounts.TryGetValue(id, out int count);
        FocusCounts[id] = count + 1;
    }

    public static void UnregisterFocus(Transform target)
    {
        if (target == null) return;
        int id = target.GetInstanceID();
        if (!FocusCounts.TryGetValue(id, out int count)) return;

        count--;
        if (count <= 0)
            FocusCounts.Remove(id);
        else
            FocusCounts[id] = count;
    }

    public static int GetFocusCount(Transform target)
    {
        if (target == null) return 0;
        return FocusCounts.TryGetValue(target.GetInstanceID(), out int count) ? count : 0;
    }

    public static void ClearAll() => FocusCounts.Clear();
}
