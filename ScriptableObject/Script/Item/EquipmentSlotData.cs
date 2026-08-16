using System;

[Serializable]
public class EquipmentSlotData
{
    public int helmetUid;
    public string helmetInstanceId;

    public int armorUid;
    public string armorInstanceId;

    public int glovesUid;
    public string glovesInstanceId;

    public int shoesUid;
    public string shoesInstanceId;

    public int ring1Uid;
    public string ring1InstanceId;

    public int ring2Uid;
    public string ring2InstanceId;

    public int necklaceUid;
    public string necklaceInstanceId;

    public int weaponUid;
    public string weaponInstanceId;

    public int subWeaponUid;
    public string subWeaponInstanceId;

    public void Clear()
    {
        helmetUid = 0;
        helmetInstanceId = null;

        armorUid = 0;
        armorInstanceId = null;

        glovesUid = 0;
        glovesInstanceId = null;

        shoesUid = 0;
        shoesInstanceId = null;

        ring1Uid = 0;
        ring1InstanceId = null;

        ring2Uid = 0;
        ring2InstanceId = null;

        necklaceUid = 0;
        necklaceInstanceId = null;

        weaponUid = 0;
        weaponInstanceId = null;

        subWeaponUid = 0;
        subWeaponInstanceId = null;
    }
}