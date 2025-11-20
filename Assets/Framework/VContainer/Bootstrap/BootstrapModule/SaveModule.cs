using AES.Tools.Core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;


namespace AES.Tools.VContainer
{
    [CreateAssetMenu(menuName = "Game/Bootstrap Modules/Save Module", fileName = "SaveModule")]
    public sealed class SaveModule : BootstrapModule
    {
        [Header("기본 슬롯 ID")]
        [SerializeField] string defaultSlotId = "slot1";

        [Header("시작 시 자동 로드")]
        [SerializeField] bool autoLoadOnStart = true;

        public override  async UniTask Initialize(LifetimeScope rootScope)
        {
            if (rootScope == null)
            {
                Debug.LogWarning("[SaveModule] rootScope가 없습니다. DI 없이 동작하도록 구현해야 합니다.");
                return;
            }
            
            var container = rootScope.Container;
            var slotService = container.Resolve<ISlotService>();
            var saveCoordinator = container.Resolve<ISaveCoordinator>();

            Debug.Log($"[SaveModule] Initialize (defaultSlotId = {defaultSlotId})");

            if (autoLoadOnStart)
            {
                try
                {
                    slotService.SetSlot(defaultSlotId);
                    await saveCoordinator.LoadAllAsync();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}