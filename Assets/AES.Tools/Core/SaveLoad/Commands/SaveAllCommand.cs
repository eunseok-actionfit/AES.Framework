using System;
using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;
using UnityEngine;


namespace AES.Tools.Commands
{
    public sealed class SaveAllCommand : IAsyncCommand
    {

        private readonly ISaveCoordinator _coord; 

        public SaveAllCommand(ISaveCoordinator coord)
        {
            _coord = coord ?? throw new ArgumentNullException(nameof(coord));
        }

        public bool CanExecute(object parameter = null)
        {
            // 저장 가능 여부 체크가 필요하면 여기에 추가
            return true;
        }

        public void Execute(object parameter = null)
        {
            _ = ExecuteAsync(parameter);
        }

        public async UniTask ExecuteAsync(object parameter = null)
        {
            var ct = parameter is CancellationToken token ? token : CancellationToken.None;

            Debug.Log("[SaveAllCommand] Start SaveAll");

            Result result;
            try
            {
                result = await _coord.SaveAllAsync(ct);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveAllCommand] Exception while saving: {ex}");
                throw;
            }

            if (result.IsSuccess)
            {
                Debug.Log($"[SaveAllCommand] Success: {result}");
            }
            else
            {
                Debug.LogError($"[SaveAllCommand] Fail: {result}");
            }
        }
    }
}