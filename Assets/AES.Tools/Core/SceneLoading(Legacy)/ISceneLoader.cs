using AES.Tools.Models;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;


namespace AES.Tools
{
    public interface ISceneLoader
    {
        UniTask<Scene> Load(SceneRef sceneRef, SceneLoadOptions options);
        UniTask Unload(Scene scene);
    }
}