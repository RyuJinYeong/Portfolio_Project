using System.Collections.Generic;
using UnityEngine;

public class CharacterManagementPanel : MonoBehaviour
{
    [Header("Layout")]
    public RectTransform content;       // 레이아웃 컨테이너
    public GameObject cardPrefab;     // CharacterInfoCard가 붙어있는 프리팹

    readonly List<CharacterInfoPanel> _infoPanel = new();

    void OnDisable()
    {
        Clear();
    }

    public void OpenAndBuild()
    {
        gameObject.SetActive(true);

        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        int need = pd?.characterIds?.Count ?? 0;
        int have = CharacterPoolManager.Instance.Pool.Count;

        if (have < need)
        {
            CharacterPoolManager.Instance.BuildPoolFromPlayerData(() =>
            {
                RebuildFromPool();
            });
        }
        else
        {
            RebuildFromPool();
        }
    }


    public void RebuildFromPool()
    {
        Clear();

        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        if (pd?.characterIds == null || pd.characterIds.Count == 0) return;

        foreach (var id in pd.characterIds)
        {
            var cm = CharacterPoolManager.Instance.Get(id);
            if (!cm || cm.character == null || !cm.character.IsAlive) continue;

            var go = Instantiate(cardPrefab, content);
            var panel = go.GetComponent<CharacterInfoPanel>();
            panel.Bind(cm);

            _infoPanel.Add(panel);
        }
    }

    public void RefreshAll()
    {
        foreach (var c in _infoPanel) c.Refresh();
    }

    void Clear()
    {
        foreach (Transform t in content) Destroy(t.gameObject);
        _infoPanel.Clear();
    }
}
