using UnityEngine;

public static class IconFixer
{
    // 파일명 규칙: 필요에 맞게 조정
    static string Sanitize(string name) => string.IsNullOrEmpty(name) ? "" : name.Replace(" ", "");

    public static void FixAllIcons(CharacterData c)
    {
        // 스킬 아이콘
        if (c.Skills != null)
        {
            foreach (var s in c.Skills)
            {
                if (s == null) continue;

                // 1) 캐시 시도 (IconStore 사용 중이면)
                s.EnsureIconFromCache();

                // 2) 폴백(Resources)
                if (s.icon == null)
                {
                    // IconAddress 우선 → 없으면 uid/이름을 규칙으로
                    var key = !string.IsNullOrEmpty(s.IconAddress) ? s.IconAddress : Sanitize(s.name);
                    s.icon = Resources.Load<Texture2D>($"Icons/Skills/{key}");
                }
            }
        }

        // 장비 아이콘
        var equips = c.GetEquipments() as System.Collections.Generic.List<Equipment>;
        if (equips != null)
        {
            foreach (var e in equips)
            {
                if (e == null || e.icon != null) continue;
                var key = Sanitize(e.name);
                e.icon = Resources.Load<Texture2D>($"Icons/{key}");
            }
        }
    }
}
