using System;
using CesiumForUnity;
using UnityEngine;

public static class GameEvents
{
    /// <summary>
    /// Caller: VrMetadataPicker
    /// Listeners: ModelMetadataVisuals
    /// </summary>
    public static event Action<CesiumModelMetadata> onModelMetadataHoverOn;
    public static void OnModelMetadataHoverOn(CesiumModelMetadata metadata) { onModelMetadataHoverOn?.Invoke(metadata); }
    
    /// <summary>
    /// Caller: VrMetadataPicker
    /// Listeners: ModelMetadataVisuals
    /// </summary>
    public static event Action onModelMetadataHoverOffAll;
    public static void OnModelMetadataHoverOffAll() { onModelMetadataHoverOffAll?.Invoke(); }

    /// <summary>
    /// Caller: VrMetadataPicker
    /// Listeners: CesiumTilesetHider
    /// </summary>
    public static event Action<TileType> onTileViewChanged;
    public static void OnTileViewChanged(TileType tileType) { onTileViewChanged?.Invoke(tileType); }
}
