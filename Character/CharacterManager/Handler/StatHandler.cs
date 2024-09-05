public class StatHandler
{
    public void InvestStatPoint(CharacterData character, string statName, int points) // 스탯 투자 메서드 ( 보너스 스탯을 받아서 투자하는 형식으로 수정 필요 )
    {
        character.RemoveAllTraits();

        switch (statName)
        {
            case "strength":
                character.BaseStats.Strength += points;
                break;
            case "dexterity":
                character.BaseStats.Dexterity += points;
                break;
            case "intelligence":
                character.BaseStats.Intelligence += points;
                break;
                // 다른 스탯들도 추가
        }

        character.ApplyAllTraits();
        character.UpdateFinalStats();
    }
}
