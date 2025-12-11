using System;
using System.Linq;
using System.Text;
using System.Threading;
using AES.Tools;
using AES.Tools.Core;
using Cysharp.Threading.Tasks;

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

    public async UniTask<Result<T>> LoadAsync<T>(string slotId = null, CancellationToken ct = default)
    {
        var info = GetInfo<T>();
        var key = BuildKey(info, slotId);
        var backend = EffectiveBackend(info);

        byte[] bytes = null;
        Error lastErr;

        // Cloud First 시도
        if (backend == SaveBackend.CloudFirst && cloud != null)
        {
            try
            {
                bytes = await cloud.LoadOrNullAsync(key, ct);
            }
            catch (Exception ex)
            {
                lastErr = new Error("cloud-load-fail", ex.Message, info.Id, true, ex);
            }
        }

        // Local fallback 또는 LocalOnly
        if (bytes == null)
        {
            try
            {
                bytes = await local.LoadOrNullAsync(key, ct);
            }
            catch (Exception ex)
            {
                lastErr = new Error("local-load-fail", ex.Message, info.Id, false, ex);
                return Result<T>.Fail(lastErr);
            }
        }

        if (bytes == null)
            return Result<T>.Ok(default);

        try
        {
            var jsonStr = Encoding.UTF8.GetString(bytes);
            var data = json.Deserialize<T>(jsonStr);
            return Result<T>.Ok(data);
        }
        catch (Exception ex)
        {
            return Result<T>.Fail(new Error("json-parse", ex.Message, info.Id, false, ex));
        }
    }
    public UniTask<Result> SaveAsync<T>(T data, CancellationToken ct = default)
    => SaveAsync(null, data, ct);
    
    public async UniTask<Result> SaveAsync<T>(string slotId, T data, CancellationToken ct = default)
    {
        var info = GetInfo<T>();
        var key = BuildKey(info, slotId);
        var backend = EffectiveBackend(info);

        try
        {
            var jsonStr = json.Serialize(data);
            var bytes = Encoding.UTF8.GetBytes(jsonStr);

            var rLocal = await local.SaveAsync(key, bytes, ct);
            if (rLocal.IsFail) return rLocal;

            if (backend == SaveBackend.CloudFirst && cloud != null)
            {
                var rCloud = await cloud.SaveAsync(key, bytes, ct);
                if (rCloud.IsFail) return rCloud;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("save-fail", ex.Message, info.Id, false, ex));
        }
    }
    
    public async UniTask<Result> DeleteAsync<T>(string slotId = null, CancellationToken ct = default)
    {
        var info = GetInfo<T>();
        var key = BuildKey(info, slotId);
        var backend = EffectiveBackend(info);

        try
        {
            // Local 삭제
            var rLocal = await local.DeleteAsync(key, ct);
            if (rLocal.IsFail) return rLocal;

            // CloudFirst면 클라우드도 삭제
            if (backend == SaveBackend.CloudFirst && cloud != null)
            {
                var rCloud = await cloud.DeleteAsync(key, ct);
                if (rCloud.IsFail) return rCloud;
            }

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("delete-fail", ex.Message, info.Id, false, ex));
        }
    }
}
