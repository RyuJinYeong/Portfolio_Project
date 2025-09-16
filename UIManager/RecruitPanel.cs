using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecruitPanel : MonoBehaviour
{
    [Serializable] public class RecruitOffer { public CharacterData template; public int cost; public int contractDays = 7; }

    [Header("Offers")]
    public List<RecruitOffer> offers = new();           // 인스펙터에 세팅
    public RectTransform listContainer;
    public GameObject listItemPrefab;
    public RectTransform detailContainer;
    public Button hireButton;
    public TMP_Text costText;
    public TMP_Text contractText;

    CharacterData _selected;

    void OnEnable()
    {
        BuildList();
        hireButton.onClick.AddListener(HireSelected);
    }
    void OnDisable()
    {
        hireButton.onClick.RemoveAllListeners();
    }

    void BuildList()
    {
        foreach (Transform c in listContainer) Destroy(c.gameObject);
        foreach (var off in offers)
        {
            var go = Instantiate(listItemPrefab, listContainer);
            var btn = go.GetComponent<Button>();
            var txt = go.GetComponentInChildren<TMP_Text>();
            var img = go.GetComponentInChildren<RawImage>();

            txt.text = off.template.Name;
            img.texture = off.template.Portrait;
            btn.onClick.AddListener(() => Select(off));
        }
        if (offers.Count > 0) Select(offers[0]);
    }

    void Select(RecruitOffer off)
    {
        _selected = off.template;
        // 우측 상세: 필요 정보(스탯/장비/스킬) 텍스트/아이콘 채우기 (간단 예시)
        // detailContainer 내에 바인딩한 텍스트/아이콘 업데이트 로직 작성

        costText.text = $"{off.cost:N0} G";
        contractText.text = $"{off.contractDays} days";
        hireButton.interactable = PlayerManager.Instance.GetCurrentPlayerData().gold >= off.cost;
    }

    void HireSelected()
    {
        if (_selected == null) return;

        // 비용 차감
        var pd = PlayerManager.Instance.GetCurrentPlayerData();
        var offer = offers.Find(o => o.template == _selected);
        if (offer == null) return;
        if (pd.gold < offer.cost) return;

        pd.gold -= offer.cost;

        // 캐릭터 생성(고유 ID 부여 + 저장)
        PlayerManager.Instance.CreateCharacter(_selected); // 내부에서 ID 부여 + SaveCharacter()
        PlayerManager.Instance.AddCharacterID(_selected.ID);

        // 풀에 즉시 편입
        CharacterPoolManager.Instance.BuildPoolFromPlayerData(() => {
            // 선택적으로: 리스트 갱신/토스트 표시
        });

        // 플레이어 저장
        PlayerManager.Instance.SavePlayerDataToPlayFab();

        // 채용된 오퍼 제거/리스트 갱신
        offers.Remove(offer);
        BuildList();
    }
}
