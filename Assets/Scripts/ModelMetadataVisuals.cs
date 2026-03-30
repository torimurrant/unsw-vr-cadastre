using System;
using CesiumForUnity;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ModelMetadataVisuals : MonoBehaviour
{
    [SerializeField] private bool focusOnHover = true;
    [SerializeField] private Material defaultMaterial = null;
    [SerializeField] private Material transparentMaterial = null;
    [SerializeField] private Cesium3DTileset cesiumTileset = null;
    private List<CesiumModelMetadata> metadataList = new List<CesiumModelMetadata>();
    private bool metadataFound = false;
    
    private void OnEnable()
    {
        GameEvents.onModelMetadataHoverOn += MetadataHoverOn;
        GameEvents.onModelMetadataHoverOffAll += MetadataHoverOffAll;
    }
    
    private void OnDisable()
    {
        GameEvents.onModelMetadataHoverOn -= MetadataHoverOn;
        GameEvents.onModelMetadataHoverOffAll -= MetadataHoverOffAll;
    }

    // Runtime CesiumModelMetadata doesn't seem to exist at the point.
    // private void Start()
    // {
    //     metadataList = GetComponentsInChildren<CesiumModelMetadata>().ToList();
    // }
    
    private void MetadataHoverOn(CesiumModelMetadata metadata)
    {
        if (!focusOnHover) return;

        // Always refresh (Cesium streams objects)
        metadataList = GetComponentsInChildren<CesiumModelMetadata>(true).ToList();

        if (metadataList.Count == 0) return;

        foreach (var metadataItem in metadataList)
        {
            var renderers = metadataItem.GetComponentsInChildren<MeshRenderer>(true);

            foreach (var renderer in renderers)
            {
                if (metadataItem != metadata)
                {
                    renderer.sharedMaterial = transparentMaterial;
                }
                else
                {
                    renderer.sharedMaterial = defaultMaterial;
                }
            }
        }
    }
    private void MetadataHoverOffAll()
    {
        cesiumTileset.opaqueMaterial = defaultMaterial;
        
        MeshRenderer renderer = null;
        
        foreach (var metadataItem in metadataList)
        {
            renderer = metadataItem.GetComponentInChildren<MeshRenderer>();
            renderer.material = defaultMaterial;
        }
    }
}
