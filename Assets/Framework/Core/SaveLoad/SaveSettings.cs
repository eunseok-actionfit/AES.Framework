// using AES.Tools.Core;
// using AES.Tools.Impl;
// using UnityEngine;
//
// namespace Infrastructure.Storage
// {
//     [CreateAssetMenu(menuName = "Game/Save Settings", fileName = "SaveSettings")]
//     public sealed class SaveSettings : ScriptableObject
//     {
//         public enum SerializerType
//         {
//             UnityJson,
//             NewtonsoftJson
//         }
//
//         [Header("Serializer")]
//         public SerializerType serializerType = SerializerType.NewtonsoftJson;
//
//         [Header("Cloud")]
//         public bool useCloud = false;
//
//         [Header("Slot")]
//         public string defaultSlotId = "default";
//     }
// }