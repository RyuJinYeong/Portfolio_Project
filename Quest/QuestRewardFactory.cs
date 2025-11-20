using System;

public static class QuestRewardFactory
{
    // 의뢰 주체별 보상 풀 (예시)
    static readonly int[] MAGE_TOWER_SKILLS = { 3010, 3011 };
    static readonly int[] SWORD_DOJO_SKILLS = { 3006, 3007 };
    static readonly int[] HUNTER_GUILD_SKILLS = { 3004, 3005 };

    static readonly int[] MAGE_TOWER_EQUIPS = { 1007 };   // 지팡이
    static readonly int[] SWORD_DOJO_EQUIPS = { 1004 };   // 장검
    static readonly int[] HUNTER_GUILD_EQUIPS = { 1008 }; // 활

    static readonly int[] TRAIT_SCROLLS_COMMON = { 18001, 18002, 18003 };
    static readonly int[] TRAIT_SCROLLS_RARE = { 18010 };

    static int PickOne(int[] pool, Random rand)
        => (pool == null || pool.Length == 0) ? 0 : pool[rand.Next(0, pool.Length)];

    public static QuestReward Build(int recLv, QuestIssuer issuer, Random rand)
    {
        var r = new QuestReward();
        r.gold = 60 + recLv * 40;

        int kind = rand.Next(0, 3); // 0=스킬 1=특성서 2=장비
        int pick = 0;

        switch (issuer)
        {
            case QuestIssuer.MageTower:
                pick = kind switch
                {
                    0 => PickOne(MAGE_TOWER_SKILLS, rand),
                    1 => PickOne(rand.Next(0, 100) < 80 ? TRAIT_SCROLLS_COMMON : TRAIT_SCROLLS_RARE, rand),
                    _ => PickOne(MAGE_TOWER_EQUIPS, rand),
                };
                break;

            case QuestIssuer.SwordDojo:
                pick = kind switch
                {
                    0 => PickOne(SWORD_DOJO_SKILLS, rand),
                    1 => PickOne(TRAIT_SCROLLS_COMMON, rand),
                    _ => PickOne(SWORD_DOJO_EQUIPS, rand),
                };
                break;

            case QuestIssuer.HunterGuild:
                pick = kind switch
                {
                    0 => PickOne(HUNTER_GUILD_SKILLS, rand),
                    1 => PickOne(TRAIT_SCROLLS_COMMON, rand),
                    _ => PickOne(HUNTER_GUILD_EQUIPS, rand),
                };
                break;

            default:
                r.itemUids = Array.Empty<int>();
                return r;
        }

        r.itemUids = pick == 0 ? Array.Empty<int>() : new[] { pick };
        return r;
    }
}
