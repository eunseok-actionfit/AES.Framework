using System.Collections.Generic;
using UnityEngine;


namespace AES.Tools.Sample
{
   
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Databinding/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        public string defaultName = "Player";
        public int defaultLevel = 1;
        public float defaultHp = 100f;
        public List<string> defaultItems = new() { "Sword", "Shield" };
    }
}


