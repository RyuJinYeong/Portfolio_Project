public enum FormationRow
{
    Front,
    Back
}

public static class FormationUtility
{
    public static int GetProtectionFormationBonus(FormationRow protectorRow, FormationRow targetRow)
    {
        if (protectorRow == FormationRow.Front && targetRow == FormationRow.Back)
            return 15;

        if (protectorRow == FormationRow.Front && targetRow == FormationRow.Front)
            return 5;

        if (protectorRow == FormationRow.Back && targetRow == FormationRow.Front)
            return -15;

        return 0;
    }
}