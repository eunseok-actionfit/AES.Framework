using AES.Tools.SceneLoading.Models;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;


namespace AES.Tools.SceneLoading
{
    public interface ISceneLoader
    {
        UniTask<Scene> Load(SceneRef sceneRef, SceneLoadOptions options);
        UniTask Unload(Scene scene);
    }
}