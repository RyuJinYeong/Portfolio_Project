using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

public class JsonHelper
{
    public static string SerializeCharacterData(CharacterData characterData)
    {
        return JsonConvert.SerializeObject(characterData, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
    }

    public static CharacterData DeserializeCharacterData(string json)
    {
        return JsonConvert.DeserializeObject<CharacterData>(json, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Auto
        });
    }
}