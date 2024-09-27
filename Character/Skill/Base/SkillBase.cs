using SoftKitty.InventoryEngine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public abstract class SkillBase : Item  // 에셋의 Item 클래스를 상속받음
{
    public double ActivationSpeed { get; protected set; } // 스킬 발동 속도
    public float DamageMultiplier { get; protected set; } // 스킬의 공격력 배수
    public int StaminaCost { get; protected set; }  // 지구력 소모
    public int MentalCost { get; protected set; }  // 정신력 소모

    public bool IsCounterSkill { get; protected set; } // 대응 스킬 여부
    public bool IsEvolvableSkill { get; protected set; } // 진화 가능한 스킬 여부
    public bool IsConditionalSkill { get; protected set; } // 습득 조건이 있는 스킬 여부

    public bool IsRangedSkill { get; protected set; } // 원거리 스킬 여부
    public bool IsBowSkill { get; protected set; } // 활 스킬 여부

    public bool CanUse { get; set; } // 사용 가능 여부
    public bool IsOffHand { get; set; } = false; // 보조무기 사용 여부

    public SkillType Type { get; protected set; } // 스킬 타입 (물리/마법)
    public SkillAttribute Attribute { get; protected set; } // 스킬 세부 속성

    public string IconAddress { get; protected set; } // Addressables에서 아이콘을 찾을 주소
    
    // 스킬 아이콘을 비동기적으로 로드하는 메서드
    public async void LoadIcon()
    {
        AsyncOperationHandle<Texture2D> handle = Addressables.LoadAssetAsync<Texture2D>(IconAddress);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            icon = handle.Result;
            Debug.Log($"{name} 아이콘 로드 완료: {icon.name}");
        }
        else
        {
            Debug.LogError($"{name} 아이콘 로드 실패: {IconAddress}");
        }
    }
}
