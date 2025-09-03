using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class QuestDef
{
    public string id;              // "Q_Forest_001"
    public string title;
    public string description;
    public string stageKey;        // "Forest" 등
    public int rewardGold;
    public int[] rewardItemUids;   // 선택
    public List<int> enemyPool;    // 스테이지에 배치될 몬스터 UID들
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { 
        get; 
        private set; 
    }

    void Awake() {
        if (Instance != null && Instance != this) { 
            Destroy(gameObject); return; 
        } 
        Instance = this; 
        DontDestroyOnLoad(gameObject);
    }

    public QuestDef current;               // 수락한 1개
    public List<QuestDef> board = new();   // 의뢰판에 노출될 후보

    public bool CanAccept() => current == null;

    public bool Accept(QuestDef q)
    {
        if (!CanAccept()) return false;
        current = q;
        // 필요하면 저장: PlayerManager.Instance.SavePlayerDataToPlayFab();
        return true;
    }

    public void Clear()
    {
        current = null;
    }
}
