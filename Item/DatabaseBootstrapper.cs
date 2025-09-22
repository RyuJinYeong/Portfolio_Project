// Assets/Scripts/Bootstrap/DatabaseBootstrapper.cs
using UnityEngine;

public static class DatabaseBootstrapper
{
    public static bool Initialized { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (Initialized) return;

        // 각 DB에선 중복 호출 무시
        EquipmentDatabase.InitializeDatabase();
        SkillDatabase.InitializeDatabase();

        Initialized = true;
        Debug.Log("[DB] Bootstrap initialized before scene load.");
    }

    // 필요하면 어디서든 강제 호출할 수 있게 공개 메서드 제공
    public static void EnsureInitialized()
    {
        if (!Initialized) Init();
    }
}
