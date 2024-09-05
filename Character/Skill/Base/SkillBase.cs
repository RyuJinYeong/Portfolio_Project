using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public abstract class SkillBase // 스킬베이스 추상 클래스 구현부
{
    public string SkillName { get; protected set; } // 스킬 이름    
    public double ActivationSpeed { get; protected set; } // 스킬 발동 속도
    public float DamageMultiplier { get; protected set; } // 스킬의 공격력 배수

    public bool IsCounterSkill { get; protected set; } // 대응 스킬 여부
    public bool IsEvolvableSkill { get; protected set; } // 진화 가능한 스킬인지 여부
    public bool IsConditionalSkill { get; protected set; } // 습득 조건이 있는 스킬인지 여부

    public bool IsRangedSkill { get; protected set; } // 원거리 스킬 여부
    public bool IsBowSkill { get; protected set; } // 활 스킬 여부

    public bool CanUse { get; set; } // 사용 가능 여부
    public bool IsOffHand { get; set; } = false; // 보조무기 사용 여부

    public SkillType Type { get; protected set; } // 스킬 타입 ( 물리/마법 )
    public SkillAttribute Attribute { get; protected set; } // 스킬 세부 속성

    public Sprite skillIcon { get; protected set; }
    public string iconAddress { get; protected set; } // Addressables에서 아이콘을 찾을 주소

    // 스킬 아이콘을 비동기적으로 로드하는 메서드
    public async void LoadIcon()
    {
        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(iconAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            skillIcon = handle.Result;
            Debug.Log($"{SkillName} 아이콘 로드 완료: {skillIcon.name}");
        }
        else
        {
            Debug.LogError($"{SkillName} 아이콘 로드 실패: {iconAddress}");
        }
    }
}