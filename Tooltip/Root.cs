// Root 클래스 (모든 객체의 공통 부모 클래스)
using UnityEngine;

public abstract class Root
{
    public string Name { get; set; }
    public string Description { get; set; } 
    public Texture2D Icon { get; set; } // 아이콘
    public ObjectType ObjectType { get; set; } // 열거형 변수로 타입 구분
}

public enum ObjectType
{
    Equipment,      // 장비
    StatusEffect,   // 상태이상
    Skill,          // 스킬
    Consumable,     // 소모품
    Material,       // 재료
    QuestItem       // 퀘스트
}