using System.Collections.Generic;

public static class GameGlobals
{
    // internal storage for boolean flags
    static readonly Dictionary<string, bool> boolFlags = new Dictionary<string, bool>();

    // set a named flag to true/false
    public static void SetFlag(string name, bool value)
    {
        if (string.IsNullOrEmpty(name)) return;
        boolFlags[name] = value;
    }

    // get value of named flag (returns false if missing)
    public static bool GetFlag(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        if (boolFlags.TryGetValue(name, out bool v)) return v;
        return false;
    }

    // toggle a flag (returns new value)
    public static bool ToggleFlag(string name)
    {
        bool newVal = !GetFlag(name);
        SetFlag(name, newVal);
        return newVal;
    }

    // clear all flags (useful for testing)
    public static void ClearAll()
    {
        boolFlags.Clear();
    }
}
