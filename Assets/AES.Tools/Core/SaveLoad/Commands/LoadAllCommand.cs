using System;
using System.Threading;
using AES.Tools.TBC.Result;
using Cysharp.Threading.Tasks;
using UnityEngine;


// 로그 출력용

namespace AES.Tools.Commands
{
    public sealed class LoadAllCommand : IAsyncCommand
    {
        private readonly ISaveCoordinator _coord;
    

        public LoadAllCommand(ISaveCoordinator coord)
        {
            _coord = coord ?? throw new ArgumentNullException(nameof(coord));
        }

        public bool CanExecute(object parameter = null)
        {
            // 로드 가능 여부 체크가 필요하면 여기에 추가
            return true;
        }

        public void Execute(object parameter = null)
        {
            _ = ExecuteAsync(parameter);
        }

        public async UniTask ExecuteAsync(object parameter = null)
        {
            var ct = parameter is CancellationToken token ? token : CancellationToken.None;

            Debug.Log("[LoadAllCommand] Start LoadAll");

            Result result;
            try
            {
                result = await _coord.LoadAllAsync(ct);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LoadAllCommand] Exception while loading: {ex}");
                throw;
            }

            if (result.IsSuccess)
            {
                Debug.Log($"[LoadAllCommand] Success: {result}");
            }
            else
            {
                Debug.LogError($"[LoadAllCommand] Fail: {result}");
            }
        }
    
    }
}