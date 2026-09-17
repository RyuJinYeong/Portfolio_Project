public static class SkillStyleUtility
{
    public const int MatchupBonus = 15;

    public static int GetMatchupBonus(SkillStyle attackStyle, SkillStyle counterStyle)
    {
        if (attackStyle == counterStyle)
            return 0;

        if (Beats(counterStyle, attackStyle))
            return MatchupBonus;

        if (Beats(attackStyle, counterStyle))
            return -MatchupBonus;

        return 0;
    }

    public static bool Beats(SkillStyle a, SkillStyle b)
    {
        return
            (a == SkillStyle.Strength && b == SkillStyle.Speed) ||
            (a == SkillStyle.Speed && b == SkillStyle.Dexterity) ||
            (a == SkillStyle.Dexterity && b == SkillStyle.Strength);
    }

    public static int GetStyleStat(CharacterData character, SkillStyle style)
    {
        if (character == null || character.FinalStats == null)
            return 0;

        return style switch
        {
            SkillStyle.Strength => character.FinalStats.Strength,
            SkillStyle.Dexterity => character.FinalStats.Dexterity,
            SkillStyle.Speed => character.FinalStats.Speed,
            _ => 0
        };
    }

    public static int GetStyleAdvantage(CharacterData character, SkillStyle style)
    {
        return GetStyleStat(character, style) / 10;
    }
}