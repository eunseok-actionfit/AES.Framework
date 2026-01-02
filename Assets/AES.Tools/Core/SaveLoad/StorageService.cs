using System;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;


namespace AES.Tools
{
    public sealed class StorageService : IStorageService
    {
        private readonly ILocalBlobStore local;
        private readonly ICloudBlobStore cloud;
        private readonly IJsonSerializer json;
        private readonly StorageProfile profile;

        public StorageService(ILocalBlobStore local, ICloudBlobStore cloud, IJsonSerializer json)
        {
            this.local = local;
            this.cloud = cloud;
            this.json = json;
            profile = null;
        }

        public StorageService(
            ILocalBlobStore local,
            ICloudBlobStore cloud,
            IJsonSerializer json,
            StorageProfile profile)
            : this(local, cloud, json)
        {
            this.profile = profile;
        }

        SaveDataInfo GetInfo<T>()
        {
            var type = typeof(T);
            var info = SaveDataRegistry.All.FirstOrDefault(i => i.Type == type);
            if (info == null)
                throw new Exception($"[Storage] {type.Name}에 SaveDataAttribute 필요");
            return info;
        }

        bool EffectiveUseSlot(SaveDataInfo info)
        {
            var entry = profile?.Find(info.Id);
            return entry?.useSlotOverride ?? info.UseSlot;
        }

        SaveBackend EffectiveBackend(SaveDataInfo info)
        {
            var entry = profile?.Find(info.Id);
            return entry?.backendOverride ?? info.Backend;
        }

        string BuildKeyWithProfile(SaveDataInfo info, string slotId)
        {
            return EffectiveUseSlot(info) ? $"{info.Id}_{slotId}" : info.Id;
        }

        string BuildKey(SaveDataInfo info, string slotId)
            => BuildKeyWithProfile(info, slotId); // StorageProfile-aware key builder

        // Result 제거 버전
        public async UniTask<T> LoadAsync<T>(string slotId = null, CancellationToken ct = default)
        {
            var info = GetInfo<T>();
            var key = BuildKey(info, slotId);
            var backend = EffectiveBackend(info);

            byte[] bytes = null;

            // Cloud First 시도
            if (backend == SaveBackend.CloudFirst && cloud != null)
            {
                try
                {
                    bytes = await cloud.LoadOrNullAsync(key, ct);
                }
                catch (Exception ex)
                {
                    // 에러 로깅만 하고, 로컬로 폴백
                    UnityEngine.Debug.LogWarning(
                        $"[Storage] Cloud load fail: {info.Id}, {ex.Message}");
                }
            }

            // Local fallback 또는 LocalOnly
            if (bytes == null)
            {
                bytes = await local.LoadOrNullAsync(key, ct);
            }

            // 저장된 데이터가 아예 없으면 default(T) 반환
            if (bytes == null)
                return default;

            try
            {
                var jsonStr = Encoding.UTF8.GetString(bytes);
                var data = json.Deserialize<T>(jsonStr);
                return data;
            }
            catch (Exception ex)
            {
                // JSON 파싱 실패 시 예외를 그대로 던지거나, default 반환 중 선택
                UnityEngine.Debug.LogError(
                    $"[Storage] Json parse fail: {info.Id}, {ex.Message}");
                throw;
                // 혹은 필요하면: return default;
            }
        }

        // SaveAsync / DeleteAsync 도 Result 제거 버전 예시
        public UniTask SaveAsync<T>(T data, CancellationToken ct = default)
            => SaveAsync(null, data, ct);

        public async UniTask SaveAsync<T>(string slotId, T data, CancellationToken ct = default)
        {
            var info = GetInfo<T>();
            var key = BuildKey(info, slotId);
            var backend = EffectiveBackend(info);

            try
            {
                var jsonStr = json.Serialize(data);
                var bytes = Encoding.UTF8.GetBytes(jsonStr);

                await local.SaveAsync(key, bytes, ct);

                if (backend == SaveBackend.CloudFirst && cloud != null)
                {
                    await cloud.SaveAsync(key, bytes, ct);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    $"[Storage] Save fail: {info.Id}, {ex.Message}");
                throw;
            }
        }

        public async UniTask DeleteAsync<T>(string slotId = null, CancellationToken ct = default)
        {
            var info = GetInfo<T>();
            var key = BuildKey(info, slotId);
            var backend = EffectiveBackend(info);

            try
            {
                // Local 삭제
                await local.DeleteAsync(key, ct);

                // CloudFirst면 클라우드도 삭제
                if (backend == SaveBackend.CloudFirst && cloud != null)
                {
                    await cloud.DeleteAsync(key, ct);
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError(
                    $"[Storage] Delete fail: {info.Id}, {ex.Message}");
                throw;
            }
        }
    }
}