using System;
using System.Collections.Generic;

[Serializable]
public class MercenaryIdentityPoolData
{
    public int maleChance = 50;

    public List<string> maleNames = new();
    public List<string> femaleNames = new();

    public List<int> maleFaceTypeIds = new();
    public List<int> femaleFaceTypeIds = new();
    public List<int> maleHairStyleIds = new();
    public List<int> femaleHairStyleIds = new();

    public List<int> hairColorIds = new();
    public List<int> skinColorIds = new();
    public List<int> eyeColorIds = new();
    public List<int> facialHairIds = new();
    public List<int> bustSizeIds = new();

    public string PickName(bool isMale, string fallback, System.Random random)
    {
        List<string> pool = isMale ? maleNames : femaleNames;

        if (pool == null || pool.Count == 0)
            return fallback;

        return pool[random.Next(0, pool.Count)];
    }

    public CustomizationData CreateCustomization(System.Random random)
    {
        bool isMale = random.Next(0, 100) < Math.Max(0, Math.Min(100, maleChance));

        return new CustomizationData
        {
            IsMale = isMale,
            GenderId = isMale ? 1 : 2,
            FaceTypeId = Pick(isMale ? maleFaceTypeIds : femaleFaceTypeIds, 1, random),
            HairStyleId = Pick(isMale ? maleHairStyleIds : femaleHairStyleIds, 1, random),
            HairColorId = Pick(hairColorIds, 1, random),
            SkinColorId = Pick(skinColorIds, 1, random),
            EyeColorId = Pick(eyeColorIds, 1, random),
            FacialHairId = isMale ? Pick(facialHairIds, 0, random) : 0,
            BustSizeId = isMale ? 2 : Pick(bustSizeIds, 2, random)
        };
    }

    private int Pick(List<int> pool, int fallback, System.Random random)
    {
        if (pool == null || pool.Count == 0)
            return fallback;

        return pool[random.Next(0, pool.Count)];
    }
}
