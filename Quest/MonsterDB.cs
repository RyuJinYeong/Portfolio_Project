using System;
using System.Collections.Generic;

public static class MonsterDB
{
    private class Entry
    {
        public int uid;
        public string name;
        public string tag; // "Beast","Undead","Bandit"...
    }

    static readonly Dictionary<int, Entry> UID = new();
    static readonly Dictionary<string, List<int>> BY_TAG = new();

    static void Add(int uid, string name, string tag)
    {
        UID[uid] = new Entry { uid = uid, name = name, tag = tag };
        if (!BY_TAG.TryGetValue(tag, out var list))
            BY_TAG[tag] = list = new List<int>();
        list.Add(uid);
    }

    static MonsterDB()
    {
        // ---- Beast ----
        Add(14001, "½£´Á´ë", "Beast");
        Add(14002, "¸äµÅÁö", "Beast");
        Add(19101, "°Å´ë ´Á´ë(¿¤¸®Æ®)", "Beast");
        Add(19001, "¿ìµÎ¸Ó¸® ´Á´ë(º¸½º)", "Beast");

        // ---- Bandit ----
        Add(16001, "µµÀû´Ü¿ø", "Bandit");

        // ---- Undead ----
        Add(15001, "ÇØ°ñº´»ç", "Undead");
        Add(19002, "ÁöÇÏÀÇ ¸Á·É¿Õ(º¸½º)", "Undead");

        // ---- Ruins boss example ----
        Add(19003, "À¯ÀûÀÇ ¼öÈ£ÀÚ(º¸½º)", "Undead");
        Add(19301, "°í´ëÀÇ ÆÄ¼ö²Û(¿¤¸®Æ®)", "Undead");
        Add(19201, "µ¿±¼ Æ÷½ÄÀÚ(¿¤¸®Æ®)", "Beast");
    }

    public static string GetName(int uid) =>
        UID.TryGetValue(uid, out var e) ? e.name : $"#{uid}";

    public static int[] GetUidsByTag(string tag) =>
        BY_TAG.TryGetValue(tag, out var list) ? list.ToArray() : Array.Empty<int>();
}
