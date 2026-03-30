using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using System;
using System.Text;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class VrMetadataPicker : MonoBehaviour
{
    [SerializeField] private Transform leftControllerTransform = null;
    [SerializeField] private Transform rightControllerTransform = null;
    [SerializeField] private TextMeshProUGUI metadataTextLeft = null;
    [SerializeField] private TextMeshProUGUI metadataTextRight = null;
    
    // Cached Dictionary of metadata values. This prevents reallocation every
    // time metadata is sampled from the tileset.
    private Dictionary<String, CesiumMetadataValue> metadataValues = null;
    private VrControls vrControls = null;
    private string hoveredMetadata = string.Empty;
    private string selectedMetadata = string.Empty;
    
    private const float RAYCAST_DISTANCE = 1000.0f;

    private void Awake()
    {
        vrControls = new VrControls();
    }
    
    private void OnEnable()
    {
        vrControls.XRILeftInteraction.Activate.performed += OnTriggerPress;
        vrControls.XRIRightInteraction.Activate.performed += OnTriggerPress;
        vrControls.Enable();
    }
    
    private void OnDisable()
    {
        vrControls.XRILeftInteraction.Activate.performed -= OnTriggerPress;
        vrControls.XRIRightInteraction.Activate.performed -= OnTriggerPress;
        vrControls.Disable();
    }
    
    private void Start()
    {
        metadataValues = new Dictionary<String, CesiumMetadataValue>();
        metadataTextLeft.text = "Use the trigger to select metadata!";
        metadataTextRight.text = "Use the trigger to select metadata!";
    }
    
    // We might need separate left/right functions if we need to distinguish between the 
    // two controller inputs, for some reason.
    private void OnTriggerPress(InputAction.CallbackContext obj)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;      // ignore input when hovering over UI object
        if (hoveredMetadata == string.Empty) return;
        selectedMetadata = hoveredMetadata;
        metadataTextLeft.text = hoveredMetadata;
        metadataTextRight.text = selectedMetadata;
        Debug.Log($"Selected:\n\n{selectedMetadata}!");
    }
    
    // Right now, both controllers are raycasting all the time, so if the user is waving
    // both around the metadata will change a lot. We could perhaps only use one controller
    // for metadata hovering/picking (and maybe other related stuff like building 'level selection')
    // and the other controller for things like player movement. Or perhaps searching for metadata
    // could require a button press-hold, and then some other input for selecting (release, or button press).
    private void Update()
    {
        if (leftControllerTransform) RayCastForMetadata(leftControllerTransform);
        if (rightControllerTransform) RayCastForMetadata(rightControllerTransform);
    }
    
    private void RayCastForMetadata(Transform controllerTransform)
    {
        RaycastHit hit;
        
        if (Physics.Raycast(controllerTransform.position, controllerTransform.TransformDirection(Vector3.forward), out hit, RAYCAST_DISTANCE))
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
                    string valueLLabelText = string.Empty;
                    string valueAsString = valuePair.Value.GetString();
                    if (string.IsNullOrEmpty(valueAsString) || valueAsString == "null") continue;
                    if (valuePair.Key != "lotNumber" && valuePair.Key != "lot" && valuePair.Key != "planNumber") continue;
                    if (valuePair.Key == "lotNumber" || valuePair.Key == "lot") valueLLabelText = "Lot";
                    if (valuePair.Key == "planNumber") valueLLabelText = "Plan";
                    sb.Append($"<b>{valueLLabelText}:</b> {valueAsString}");
                    sb.AppendLine();
                }
                
                hoveredMetadata = sb.ToString();
                Debug.Log($"Hovering on: {hoveredMetadata}!");
                return;
            }
        }
        
        hoveredMetadata = string.Empty;
    }
}
