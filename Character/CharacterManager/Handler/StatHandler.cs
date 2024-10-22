public class StatHandler
{
    public void InvestStatPoint(CharacterManager manager, string statName, int points) // 스탯 투자 메서드 ( 보너스 스탯을 받아서 투자하는 형식으로 수정 필요 )
    {
        manager.character.RemoveAllTraits(manager);

        switch (statName)
        {
            case "strength":
                manager.character.BaseStats.Strength += points;
                break;
            case "dexterity":
                manager.character.BaseStats.Dexterity += points;
                break;
            case "intelligence":
                manager.character.BaseStats.Intelligence += points;
                break;
                // 다른 스탯들도 추가
        }

        manager.character.ApplyAllTraits(manager);
        manager.character.UpdateFinalStats();
    }
}
