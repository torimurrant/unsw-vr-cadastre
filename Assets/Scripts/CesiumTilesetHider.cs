using System;
using CesiumForUnity;
using UnityEngine;

public class CesiumTilesetHider : MonoBehaviour
{
    [SerializeField] private TileType tileType = TileType.All;
    private Cesium3DTileset tileset = null;

    private void Awake()
    {
        tileset = GetComponent<Cesium3DTileset>();
    }

    private void OnEnable()
    {
        GameEvents.onTileViewChanged += TileViewChanged;
    }
    
    private void OnDisable()
    {
        GameEvents.onTileViewChanged -= TileViewChanged;
    }

    private void TileViewChanged(TileType tileType)
    {
        if (!tileset) return;
        
        if (tileType == TileType.All || tileType == this.tileType)
        {
            tileset.enabled = true;
            return;
        }
        
        tileset.enabled = false;
    }
}

public enum TileType
{
    All,
    Lot,
    Common
};