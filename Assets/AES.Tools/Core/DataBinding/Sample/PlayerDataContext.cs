using UnityEngine;


namespace AES.Tools.Sample
{
 
    public class PlayerDataContext: DataContextBase
    {
        [SerializeField] private PlayerConfig playerConfig;
        
        protected override object CreateViewModel() => new PlayerViewModel(playerConfig);
    }
}


