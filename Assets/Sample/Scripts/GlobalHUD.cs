using Core.Systems.UI.Core.UIRoot;
using Core.Systems.UI.Core.UIManager;
using Core.Systems.UI.Core.UIView;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sample.Scripts
{
    public class GlobalHUD : UIView
    {
        // OnEnable / OnDisable 에서 UI.HideAsync 호출은 하지 않는 걸 권장
        // 필요하면 단순 이벤트 등록/해제 정도만 사용

        public void GotoPrevScene()
        {
            GotoPrevSceneAsync().Forget();
        }

        public void GotoNextScene()
        {
            GotoNextSceneAsync().Forget();
        }

        private async UniTask GotoPrevSceneAsync()
        {
            var index = SceneManager.GetActiveScene().buildIndex - 1;
            if (index <= 0) return;

            // 1) 현재 로컬 UI 정리
            await UI.CloseAllAsync(UIRootRole.Local);

            // 2) 씬 전환
            await SceneManager.LoadSceneAsync(index);
        }

        private async UniTask GotoNextSceneAsync()
        {
            var index = SceneManager.GetActiveScene().buildIndex + 1;
            var max = SceneManager.sceneCountInBuildSettings - 1;
            if (index > max) return;

            await UI.CloseAllAsync(UIRootRole.Local);
            await SceneManager.LoadSceneAsync(index);
        }
    }
}