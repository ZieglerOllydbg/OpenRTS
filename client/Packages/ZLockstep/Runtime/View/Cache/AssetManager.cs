using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

/// <summary>
/// Addressables resource manager with explicit type-separated caches.
/// </summary>
public static class AssetManager
{
    private readonly struct ResourceKey : IEquatable<ResourceKey>
    {
        public ResourceKey(Type assetType, string address)
        {
            AssetType = assetType;
            Address = address;
        }

        public Type AssetType { get; }
        public string Address { get; }

        public bool Equals(ResourceKey other)
        {
            return AssetType == other.AssetType && string.Equals(Address, other.Address, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((AssetType != null ? AssetType.GetHashCode() : 0) * 397) ^
                       (Address != null ? StringComparer.Ordinal.GetHashCode(Address) : 0);
            }
        }
    }

    private sealed class AssetEntry<T> where T : UnityEngine.Object
    {
        public string Address;
        public string PrimaryLabel;
        public T Asset;
        public AsyncOperationHandle<T> Handle;
        public readonly HashSet<string> Labels = new HashSet<string>(StringComparer.Ordinal);
    }

    private sealed class AssetStore<T> where T : UnityEngine.Object
    {
        private readonly Dictionary<string, AssetEntry<T>> addressToEntry =
            new Dictionary<string, AssetEntry<T>>(StringComparer.Ordinal);

        private readonly Dictionary<string, UniTaskCompletionSource<T>> pendingLoads =
            new Dictionary<string, UniTaskCompletionSource<T>>(StringComparer.Ordinal);

        public bool IsLoaded(string address)
        {
            return addressToEntry.ContainsKey(address);
        }

        public bool TryGetAsset(string address, out T asset)
        {
            asset = null;

            if (!addressToEntry.TryGetValue(address, out var entry))
            {
                return false;
            }

            asset = entry.Asset;
            return asset != null;
        }

        public async UniTask<T> LoadAsync(string address, string label)
        {
            if (TryGetAsset(address, out var cachedAsset))
            {
                BindLabel(address, label);
                return cachedAsset;
            }

            if (pendingLoads.TryGetValue(address, out var pending))
            {
                var pendingAsset = await pending.Task;
                BindLabel(address, label);
                return pendingAsset;
            }

            var completionSource = new UniTaskCompletionSource<T>();
            pendingLoads[address] = completionSource;

            AsyncOperationHandle<T> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<T>(address);
                var asset = await handle.ToUniTask();

                if (asset == null)
                {
                    Debug.LogError($"[AssetManager] Loaded asset is null for address '{address}'.");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    completionSource.TrySetResult(null);
                    return null;
                }

                var entry = new AssetEntry<T>
                {
                    Address = address,
                    PrimaryLabel = label,
                    Asset = asset,
                    Handle = handle
                };

                addressToEntry[address] = entry;
                BindLabel(address, label);

                completionSource.TrySetResult(asset);
                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetManager] Failed to load asset '{address}': {ex.Message}");
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                completionSource.TrySetException(ex);
                return null;
            }
            finally
            {
                pendingLoads.Remove(address);
            }
        }

        /// <summary>
        /// 同步加载并缓存指定地址资源。
        /// </summary>
        /// <param name="address">资源地址。</param>
        /// <param name="label">可选标签，用于绑定资源与标签关系。</param>
        /// <returns>加载成功返回资源实例；失败返回 null。</returns>
        public T LoadSync(string address, string label)
        {
            if (TryGetAsset(address, out var cachedAsset))
            {
                BindLabel(address, label);
                return cachedAsset;
            }

            AsyncOperationHandle<T> handle = default;

            try
            {
                handle = Addressables.LoadAssetAsync<T>(address);
                var asset = handle.WaitForCompletion();

                if (handle.Status != AsyncOperationStatus.Succeeded || asset == null)
                {
                    Debug.LogError($"[AssetManager] Loaded asset is null for address '{address}'.");
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }

                    return null;
                }

                var entry = new AssetEntry<T>
                {
                    Address = address,
                    PrimaryLabel = label,
                    Asset = asset,
                    Handle = handle
                };

                addressToEntry[address] = entry;
                BindLabel(address, label);
                return asset;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetManager] Failed to load asset '{address}': {ex.Message}");
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                return null;
            }
        }

        public void BindLabel(string address, string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            if (!addressToEntry.TryGetValue(address, out var entry))
            {
                return;
            }

            entry.Labels.Add(label);
            if (string.IsNullOrWhiteSpace(entry.PrimaryLabel))
            {
                entry.PrimaryLabel = label;
            }

            AddLabelBinding(label, new ResourceKey(typeof(T), address));
        }

        public string GetEntryTypeName(string address)
        {
            if (!addressToEntry.TryGetValue(address, out var entry))
            {
                return "Unknown";
            }

            if (entry.Asset != null)
            {
                return entry.Asset.GetType().Name;
            }

            return typeof(T).Name;
        }

        public bool RemoveLabel(string address, string label)
        {
            if (!addressToEntry.TryGetValue(address, out var entry))
            {
                return false;
            }

            entry.Labels.Remove(label);
            if (string.Equals(entry.PrimaryLabel, label, StringComparison.Ordinal))
            {
                entry.PrimaryLabel = null;
            }

            return entry.Labels.Count == 0;
        }

        public void Release(string address)
        {
            if (!addressToEntry.TryGetValue(address, out var entry))
            {
                return;
            }

            foreach (var label in entry.Labels)
            {
                RemoveLabelBinding(label, new ResourceKey(typeof(T), address));
            }

            if (entry.Handle.IsValid())
            {
                Addressables.Release(entry.Handle);
            }

            addressToEntry.Remove(address);
        }

        public void ReleaseAll()
        {
            foreach (var entry in addressToEntry.Values)
            {
                if (entry.Handle.IsValid())
                {
                    Addressables.Release(entry.Handle);
                }
            }

            addressToEntry.Clear();
            pendingLoads.Clear();
        }
    }

    private static readonly AssetStore<GameObject> PrefabAssets =
        new AssetStore<GameObject>();

    private static readonly AssetStore<TextAsset> TextAssets =
        new AssetStore<TextAsset>();

    private static readonly AssetStore<AudioClip> AudioAssets =
        new AssetStore<AudioClip>();

    private static readonly AssetStore<Sprite> SpriteAssets =
        new AssetStore<Sprite>();

    private static readonly Dictionary<string, HashSet<ResourceKey>> LabelToResources =
        new Dictionary<string, HashSet<ResourceKey>>(StringComparer.Ordinal);

    private static readonly Dictionary<int, string> InstanceIdToAddress =
        new Dictionary<int, string>();

    public static async UniTask PreloadByLabelAsync<T>(string label) where T : UnityEngine.Object
    {
        await PreloadByLabelInternalAsync<T>(label, true);
    }

    public static async UniTask PreloadByLabelsAsync(params string[] labels)
    {
        if (labels == null || labels.Length == 0)
        {
            Debug.LogError("[AssetManager] Labels cannot be null or empty.");
            return;
        }

        foreach (var label in labels)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                Debug.LogError("[AssetManager] Label cannot be null or empty.");
                continue;
            }

            await PreloadByLabelInternalAsync<GameObject>(label, false);
            await PreloadByLabelInternalAsync<TextAsset>(label, false);
            await PreloadByLabelInternalAsync<AudioClip>(label, false);
            await PreloadByLabelInternalAsync<Sprite>(label, false);
        }
    }

    public static bool IsLoaded(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        return PrefabAssets.IsLoaded(address) ||
               TextAssets.IsLoaded(address) ||
               AudioAssets.IsLoaded(address) ||
               SpriteAssets.IsLoaded(address);
    }

    public static T GetAsset<T>(string address) where T : UnityEngine.Object
    {
        if (TryGetAsset(address, out T asset))
        {
            return asset;
        }

        return null;
    }

    public static bool TryGetAsset<T>(string address, out T asset) where T : UnityEngine.Object
    {
        asset = null;

        if (string.IsNullOrWhiteSpace(address))
        {
            Debug.LogError("[AssetManager] Address cannot be null or empty.");
            return false;
        }

        if (!TryGetStore<T>(out var store))
        {
            Debug.LogError($"[AssetManager] Unsupported asset type '{typeof(T).Name}'. Supported types are GameObject, TextAsset, AudioClip, and Sprite.");
            return false;
        }

        if (store.TryGetAsset(address, out asset))
        {
            return true;
        }

        if (TryGetLoadedEntryTypeName(address, out var actualTypeName))
        {
            Debug.LogError($"[AssetManager] Cached asset type mismatch for address '{address}'. Requested '{typeof(T).Name}', actual '{actualTypeName}'.");
        }

        return false;
    }

    public static async UniTask<T> LoadAssetAsync<T>(string address, string label = null) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            Debug.LogError("[AssetManager] Address cannot be null or empty.");
            return null;
        }

        if (!TryGetStore<T>(out var store))
        {
            Debug.LogError($"[AssetManager] Unsupported asset type '{typeof(T).Name}'. Supported types are GameObject, TextAsset, AudioClip, and Sprite.");
            return null;
        }

        if (store.TryGetAsset(address, out var cachedAsset))
        {
            store.BindLabel(address, label);
            return cachedAsset;
        }

        if (TryGetLoadedEntryTypeName(address, out var actualTypeName))
        {
            Debug.LogError($"[AssetManager] Cached asset type mismatch for address '{address}'. Requested '{typeof(T).Name}', actual '{actualTypeName}'.");
            return null;
        }

        return await store.LoadAsync(address, label);
    }

    /// <summary>
    /// 同步加载指定地址的资源。
    /// 优先返回已缓存资源；未命中缓存时使用 Addressables 同步等待并写入缓存。
    /// </summary>
    /// <typeparam name="T">资源类型，仅支持已注册的资源类型。</typeparam>
    /// <param name="address">资源地址。</param>
    /// <param name="label">可选标签，用于后续按标签卸载。</param>
    /// <returns>加载成功返回资源实例；失败返回 null。</returns>
    public static T LoadAsset<T>(string address, string label = null) where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            Debug.LogError("[AssetManager] Address cannot be null or empty.");
            return null;
        }

        if (!TryGetStore<T>(out var store))
        {
            Debug.LogError($"[AssetManager] Unsupported asset type '{typeof(T).Name}'. Supported types are GameObject, TextAsset, AudioClip, and Sprite.");
            return null;
        }

        if (store.TryGetAsset(address, out var cachedAsset))
        {
            store.BindLabel(address, label);
            return cachedAsset;
        }

        if (TryGetLoadedEntryTypeName(address, out var actualTypeName))
        {
            Debug.LogError($"[AssetManager] Cached asset type mismatch for address '{address}'. Requested '{typeof(T).Name}', actual '{actualTypeName}'.");
            return null;
        }

        return store.LoadSync(address, label);
    }

    public static async UniTask<GameObject> InstantiatePrefabAsync(
        string prefabPath,
        Transform parent = null,
        Vector3? position = null,
        Quaternion? rotation = null)
    {
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Prefab path cannot be null or empty");
            return null;
        }

        return await InstantiateAsync(prefabPath, parent, position, rotation);
    }

    public static async UniTask<Sprite> GetSpriteAsync(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath))
        {
            Debug.LogError("Sprite path cannot be null or empty");
            return null;
        }

        return await LoadAssetAsync<Sprite>(spritePath);
    }

    public static async UniTask<TextAsset> GetTextAssetAsync(string textPath)
    {
        if (string.IsNullOrEmpty(textPath))
        {
            Debug.LogError("TextAsset path cannot be null or empty");
            return null;
        }

        return await LoadAssetAsync<TextAsset>(textPath);
    }

    /// <summary>
    /// 同步获取文本资源。
    /// </summary>
    /// <param name="textPath">文本资源地址。</param>
    /// <returns>加载成功返回文本资源；失败返回 null。</returns>
    public static TextAsset GetTextAsset(string textPath)
    {
        if (string.IsNullOrEmpty(textPath))
        {
            Debug.LogError("TextAsset path cannot be null or empty");
            return null;
        }

        return LoadAsset<TextAsset>(textPath);
    }

    public static async UniTask<AudioClip> GetAudioClipAsync(string audioPath)
    {
        if (string.IsNullOrEmpty(audioPath))
        {
            Debug.LogError("AudioClip path cannot be null or empty");
            return null;
        }

        return await LoadAssetAsync<AudioClip>(audioPath);
    }

    public static async UniTask<GameObject> InstantiateAsync(
        string address,
        Transform parent = null,
        Vector3? position = null,
        Quaternion? rotation = null)
    {
        var prefab = await LoadAssetAsync<GameObject>(address);
        if (prefab == null)
        {
            Debug.LogError($"[AssetManager] Failed to instantiate prefab '{address}'.");
            return null;
        }

        var instance = UnityEngine.Object.Instantiate(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = position ?? Vector3.zero;
        instance.transform.rotation = rotation ?? Quaternion.identity;
        instance.SetActive(true);

        InstanceIdToAddress[instance.GetInstanceID()] = address;
        return instance;
    }

    public static void ReleaseInstance(GameObject instance, float delay = 0f)
    {
        if (instance == null)
        {
            Debug.LogError("[AssetManager] Cannot release null instance.");
            return;
        }

        InstanceIdToAddress.Remove(instance.GetInstanceID());

        if (delay > 0f)
        {
            UnityEngine.Object.Destroy(instance, delay);
            return;
        }

        UnityEngine.Object.Destroy(instance);
    }

    public static void UnloadByLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            Debug.LogError("[AssetManager] Label cannot be null or empty.");
            return;
        }

        if (!LabelToResources.TryGetValue(label, out var resources))
        {
            return;
        }

        var releaseList = new List<ResourceKey>(resources);
        LabelToResources.Remove(label);

        foreach (var key in releaseList)
        {
            var shouldRelease = RemoveLabel(key, label);
            if (!shouldRelease)
            {
                continue;
            }

            WarnIfAddressHasLiveInstances(key.Address);
            Release(key);
        }
    }

    public static void UnloadByLabels(params string[] labels)
    {
        if (labels == null || labels.Length == 0)
        {
            Debug.LogError("[AssetManager] Labels cannot be null or empty.");
            return;
        }

        foreach (var label in labels)
        {
            UnloadByLabel(label);
        }
    }

    public static void UnloadAll()
    {
        PrefabAssets.ReleaseAll();
        TextAssets.ReleaseAll();
        AudioAssets.ReleaseAll();
        SpriteAssets.ReleaseAll();

        LabelToResources.Clear();
        InstanceIdToAddress.Clear();
    }

    private static async UniTask PreloadByLabelInternalAsync<T>(string label, bool warnWhenEmpty)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            Debug.LogError("[AssetManager] Label cannot be null or empty.");
            return;
        }

        if (!TryGetStore<T>(out _))
        {
            Debug.LogError($"[AssetManager] Unsupported asset type '{typeof(T).Name}'. Supported types are GameObject, TextAsset, AudioClip, and Sprite.");
            return;
        }

        AsyncOperationHandle<IList<IResourceLocation>> locationsHandle = default;

        try
        {
            locationsHandle = Addressables.LoadResourceLocationsAsync(label, typeof(T));
            var locations = await locationsHandle.ToUniTask();

            if (locations == null || locations.Count == 0)
            {
                if (warnWhenEmpty)
                {
                    Debug.LogWarning($"[AssetManager] No resource locations found for label '{label}' and type '{typeof(T).Name}'.");
                }

                return;
            }

            foreach (var location in locations)
            {
                var address = location.PrimaryKey;
                if (string.IsNullOrWhiteSpace(address))
                {
                    continue;
                }

                await LoadAssetAsync<T>(address, label);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AssetManager] Failed to preload label '{label}' as '{typeof(T).Name}': {ex.Message}");
        }
        finally
        {
            if (locationsHandle.IsValid())
            {
                Addressables.Release(locationsHandle);
            }
        }
    }

    private static bool TryGetStore<T>(out AssetStore<T> store)
        where T : UnityEngine.Object
    {
        var assetType = typeof(T);

        if (assetType == typeof(GameObject))
        {
            store = (AssetStore<T>)(object)PrefabAssets;
            return true;
        }

        if (assetType == typeof(TextAsset))
        {
            store = (AssetStore<T>)(object)TextAssets;
            return true;
        }

        if (assetType == typeof(AudioClip))
        {
            store = (AssetStore<T>)(object)AudioAssets;
            return true;
        }

        if (assetType == typeof(Sprite))
        {
            store = (AssetStore<T>)(object)SpriteAssets;
            return true;
        }

        store = null;
        return false;
    }

    private static bool TryGetLoadedEntryTypeName(string address, out string typeName)
    {
        if (PrefabAssets.IsLoaded(address))
        {
            typeName = PrefabAssets.GetEntryTypeName(address);
            return true;
        }

        if (TextAssets.IsLoaded(address))
        {
            typeName = TextAssets.GetEntryTypeName(address);
            return true;
        }

        if (AudioAssets.IsLoaded(address))
        {
            typeName = AudioAssets.GetEntryTypeName(address);
            return true;
        }

        if (SpriteAssets.IsLoaded(address))
        {
            typeName = SpriteAssets.GetEntryTypeName(address);
            return true;
        }

        typeName = null;
        return false;
    }

    private static bool RemoveLabel(ResourceKey key, string label)
    {
        if (key.AssetType == typeof(GameObject))
        {
            return PrefabAssets.RemoveLabel(key.Address, label);
        }

        if (key.AssetType == typeof(TextAsset))
        {
            return TextAssets.RemoveLabel(key.Address, label);
        }

        if (key.AssetType == typeof(AudioClip))
        {
            return AudioAssets.RemoveLabel(key.Address, label);
        }

        if (key.AssetType == typeof(Sprite))
        {
            return SpriteAssets.RemoveLabel(key.Address, label);
        }

        return false;
    }

    private static void Release(ResourceKey key)
    {
        if (key.AssetType == typeof(GameObject))
        {
            PrefabAssets.Release(key.Address);
            return;
        }

        if (key.AssetType == typeof(TextAsset))
        {
            TextAssets.Release(key.Address);
            return;
        }

        if (key.AssetType == typeof(AudioClip))
        {
            AudioAssets.Release(key.Address);
            return;
        }

        if (key.AssetType == typeof(Sprite))
        {
            SpriteAssets.Release(key.Address);
        }
    }

    private static void AddLabelBinding(string label, ResourceKey key)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        if (!LabelToResources.TryGetValue(label, out var resources))
        {
            resources = new HashSet<ResourceKey>();
            LabelToResources[label] = resources;
        }

        resources.Add(key);
    }

    private static void RemoveLabelBinding(string label, ResourceKey key)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return;
        }

        if (!LabelToResources.TryGetValue(label, out var resources))
        {
            return;
        }

        resources.Remove(key);
        if (resources.Count == 0)
        {
            LabelToResources.Remove(label);
        }
    }

    private static void WarnIfAddressHasLiveInstances(string address)
    {
        foreach (var pair in InstanceIdToAddress)
        {
            if (string.Equals(pair.Value, address, StringComparison.Ordinal))
            {
                Debug.LogWarning($"[AssetManager] Unloading address '{address}' while instantiated objects are still alive. Clear live consumers before unloading their label.");
                return;
            }
        }
    }
}
