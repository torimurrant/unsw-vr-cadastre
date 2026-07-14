using System;
using CesiumForUnity;
using UnityEngine;

public static class GameEvents
{
    public static event Action<CesiumModelMetadata> onModelMetadataHoverOn;
    public static void OnModelMetadataHoverOn(CesiumModelMetadata metadata) { onModelMetadataHoverOn?.Invoke(metadata); }
    
    public static event Action onModelMetadataHoverOffAll;
    public static void OnModelMetadataHoverOffAll() { onModelMetadataHoverOffAll?.Invoke(); }

    public static event Action<TileType> onTileViewChanged;
    public static void OnTileViewChanged(TileType tileType) { onTileViewChanged?.Invoke(tileType); }
}
