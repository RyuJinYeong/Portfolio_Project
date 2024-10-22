using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public static class DeepCopy
{    
    public static CharacterData DeepCopyCharacter(CharacterData original)
    {
        string character;
        character = JsonHelper.SerializeCharacterData(original);

        return JsonHelper.DeserializeCharacterData(character);
    }
}
