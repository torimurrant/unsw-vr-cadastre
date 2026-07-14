using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using System;
using System.Text;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MetadataPicker : MonoBehaviour
{
    [SerializeField] private Camera cam = null;
    
    [SerializeField] private CanvasGroup metadataCanvas = null;

    [SerializeField] private TextMeshProUGUI metadataText = null;
    
    private Dictionary<String, CesiumMetadataValue> metadataValues = null;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (metadataCanvas)
        {
            metadataCanvas.alpha = 0.0f;
        }

        metadataValues = new Dictionary<String, CesiumMetadataValue>();

        if (!cam) cam = Camera.main;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool receivedInput = false;

        if (Mouse.current != null)
        {
            receivedInput = Mouse.current.leftButton.isPressed;
        }
        else if (Gamepad.current != null)
        {
            receivedInput = Gamepad.current.rightShoulder.isPressed;
        }
#else
        bool receivedInput = Input.GetMouseButtonDown(0);
#endif

        if (!receivedInput || !metadataText) return;

        RaycastHit hit;
        
        if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity))
        {
            CesiumPrimitiveFeatures features = hit.transform.GetComponent<CesiumPrimitiveFeatures>();
            CesiumModelMetadata metadata = hit.transform.GetComponentInParent<CesiumModelMetadata>();
            
            if (features && features.featureIdSets.Length > 0)
            {
                CesiumFeatureIdSet featureIdSet = features.featureIdSets[0];
                Int64 propertyTableIndex = featureIdSet.propertyTableIndex;
                if (metadata && propertyTableIndex >= 0 && propertyTableIndex < metadata.propertyTables.Length)
                {
                    CesiumPropertyTable propertyTable = metadata.propertyTables[propertyTableIndex];
                    Int64 featureID = featureIdSet.GetFeatureIdFromRaycastHit(hit);
                    propertyTable.GetMetadataValuesForFeature(metadataValues, featureID);
                }

                StringBuilder sb = new();
                
                foreach (var valuePair in metadataValues)
                {
                    string valueAsString = valuePair.Value.GetString();
                    if (string.IsNullOrEmpty(valueAsString) || valueAsString == "null") continue;
                    sb.Append($"<b>{valuePair.Key}</b>: {valueAsString}");
                    sb.AppendLine();
                }
                
                metadataText.text = sb.ToString();
                Debug.Log(sb.ToString());
            }
        }

        if (metadataCanvas)
        {
            metadataCanvas.alpha = 1.0f;
        }
    }
}
