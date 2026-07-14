using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using System;
using System.Text;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class VrMetadataPicker : MonoBehaviour
{
    [SerializeField] private Transform leftControllerTransform = null;
    [SerializeField] private Transform rightControllerTransform = null;
    [SerializeField] private TextMeshProUGUI metadataTextLeft = null;
    [SerializeField] private TextMeshProUGUI metadataTextRight = null;
    [SerializeField] private TextMeshProUGUI tileViewTextLeft = null;
    [SerializeField] private TextMeshProUGUI tileViewTextRight = null;
    [SerializeField] private GameObject hitLocationVisualPrefab = null;
    [SerializeField] private CurveVisualController leftLineVisual = null;
    [SerializeField] private CurveVisualController rightLineVisual = null;
    [SerializeField] private Gradient hoverOnLineGradient = null;
    [SerializeField] private Gradient hoverOffLineGradient = null;
    [SerializeField] private float hoverOffDelay = 0.15f;
    [SerializeField] private HapticImpulsePlayer hapticsLeft = null;
    [SerializeField] private HapticImpulsePlayer hapticsRight = null;
    
    // Cached Dictionary of metadata values. This prevents reallocation every
    // time metadata is sampled from the tileset.
    private Dictionary<String, CesiumMetadataValue> metadataValues = null;
    private VrControls vrControls = null;
    private GameObject hitLocationLeftObject = null;
    private GameObject hitLocationRightObject = null;
    private string hoveredMetadataLeft = string.Empty;
    private string selectedMetadataLeft = string.Empty;
    private string hoveredMetadataRight = string.Empty;
    private string selectedMetadataRight = string.Empty;
    private bool wasHoveringAnyLastFrame = false;
    private bool hoverOnLeft = false;
    private bool hoverOnRight = false;
    private float hoverOffTimer = 0f;
    private uint currentTileViewIndex = 0;
    
    private const float SPHERECAST_RADIUS = 0.1f; // tweak 0.02–0.1
    private const float RAYCAST_DISTANCE = 1000.0f;
    private const float HIT_LOCATION_RESET = -3000.0f;
    private const float RAY_VISUAL_LINE_LENGTH = 40.0f;
    private const float SELECT_HAPTICS_AMP = 1.0f;
    private const float SELECT_HAPTICS_DUR = 0.1f;
    private const float HOVER_ON_HAPTICS_AMP = 0.2f;
    private const float HOVER_ON_HAPTICS_DUR = 0.02f;
    
    private void Awake()
    {
        vrControls = new VrControls();
    }
    
    private void OnEnable()
    {
        vrControls.XRILeftInteraction.Activate.performed += OnTriggerPressLeft;
        vrControls.XRIRightInteraction.Activate.performed += OnTriggerPressRight;
        vrControls.XRILeftInteraction.TileViewChange.performed += OnTileViewChange;
        vrControls.XRIRightInteraction.TileViewChange.performed += OnTileViewChange;
        vrControls.Enable();
    }
    
    private void OnDisable()
    {
        vrControls.XRILeftInteraction.Activate.performed -= OnTriggerPressLeft;
        vrControls.XRIRightInteraction.Activate.performed -= OnTriggerPressRight;
        vrControls.XRIRightInteraction.TileViewChange.performed -= OnTileViewChange;
        vrControls.Disable();
    }
    
    private void Start()
    {
        metadataValues = new Dictionary<String, CesiumMetadataValue>();
        metadataTextLeft.text = "Use the trigger to select metadata!";
        metadataTextRight.text = "Use the trigger to select metadata!";
        hitLocationLeftObject =  Instantiate(hitLocationVisualPrefab);
        hitLocationLeftObject.name = "hitLocationLeft";
        hitLocationLeftObject.transform.position = new Vector3(HIT_LOCATION_RESET, HIT_LOCATION_RESET, HIT_LOCATION_RESET);
        hitLocationRightObject =  Instantiate(hitLocationVisualPrefab);
        hitLocationRightObject.name = "hitLocationRight";
        hitLocationRightObject.transform.position = new Vector3(HIT_LOCATION_RESET, HIT_LOCATION_RESET, HIT_LOCATION_RESET);
        leftLineVisual.noValidHitProperties.gradient = hoverOffLineGradient;
        leftLineVisual.restingVisualLineLength = RAY_VISUAL_LINE_LENGTH;
        rightLineVisual.noValidHitProperties.gradient = hoverOffLineGradient;
        rightLineVisual.restingVisualLineLength = RAY_VISUAL_LINE_LENGTH;
        currentTileViewIndex = 0;
    }
    
    private void OnTriggerPressLeft(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;      // ignore input when hovering over UI object
        if (hoveredMetadataLeft == string.Empty) return;
        hapticsLeft.SendHapticImpulse(SELECT_HAPTICS_AMP, SELECT_HAPTICS_DUR);
        selectedMetadataLeft = hoveredMetadataLeft;
        metadataTextLeft.text = selectedMetadataLeft;
    }
    
    private void OnTriggerPressRight(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hoveredMetadataRight == string.Empty) return;
        hapticsRight.SendHapticImpulse(SELECT_HAPTICS_AMP, SELECT_HAPTICS_DUR);
        selectedMetadataRight = hoveredMetadataRight;
        metadataTextRight.text = selectedMetadataRight;
    }
    
    private void Update()
    {
        // // Reset per-frame state
        // hoverOnLeft = false;
        // hoverOnRight = false;
        if (leftControllerTransform) RayCastForMetadata(leftControllerTransform, true);
        if (rightControllerTransform) RayCastForMetadata(rightControllerTransform, false);
        //CheckForHoverOff();
    }
    
    private void RayCastForMetadata(Transform controllerTransform, bool isLeft)
    {
        RaycastHit hit;
        
        if (Physics.SphereCast(controllerTransform.position, SPHERECAST_RADIUS, controllerTransform.TransformDirection(Vector3.forward), out hit, RAYCAST_DISTANCE))
        {
            CesiumPrimitiveFeatures features = hit.transform.GetComponent<CesiumPrimitiveFeatures>();
            CesiumModelMetadata metadata = hit.transform.GetComponentInParent<CesiumModelMetadata>();
            GameEvents.OnModelMetadataHoverOn(metadata);
            if (!features || features.featureIdSets.Length <= 0) return;
            
            if (isLeft)
            {
                hoverOnLeft = true;
                hitLocationLeftObject.transform.position = hit.point;
                leftLineVisual.noValidHitProperties.gradient = hoverOnLineGradient;
            }
            else
            {
                hoverOnRight = true;
                hitLocationRightObject.transform.position = hit.point;
                rightLineVisual.noValidHitProperties.gradient = hoverOnLineGradient;
            }
                
            CesiumFeatureIdSet featureIdSet = features.featureIdSets[0];
            Int64 propertyTableIndex = featureIdSet.propertyTableIndex;
            
            if (metadata && propertyTableIndex >= 0 && propertyTableIndex < metadata.propertyTables.Length)
            {
                CesiumPropertyTable propertyTable = metadata.propertyTables[propertyTableIndex];
                Int64 featureID = featureIdSet.GetFeatureIdFromRaycastHit(hit);
                propertyTable.GetMetadataValuesForFeature(metadataValues, featureID);
            }
            
            StringBuilder sb = new();
            StringBuilder sbFull = new();
            
            foreach (var valuePair in metadataValues)
            {
                string valueLLabelText = string.Empty;
                string valueAsString = valuePair.Value.GetString();
                if (string.IsNullOrEmpty(valueAsString) || valueAsString == "null") continue;
                sbFull.Append($"<b>{valuePair.Key}:</b> {valueAsString}");
                sbFull.AppendLine();
                if (valuePair.Key != "lotNumber" && valuePair.Key != "lot" && valuePair.Key != "planNumber") continue;
                if (valuePair.Key == "lotNumber" || valuePair.Key == "lot") valueLLabelText = "Lot";
                if (valuePair.Key == "planNumber") valueLLabelText = "Plan";
                sb.Append($"<b>{valueLLabelText}:</b> {valueAsString}");
                sb.AppendLine();
            }
            
            Debug.Log(sbFull.ToString());
                
            if (isLeft)
            {
                hoveredMetadataLeft = sb.ToString();
                hapticsLeft.SendHapticImpulse(HOVER_ON_HAPTICS_AMP, HOVER_ON_HAPTICS_DUR);
            }
            else
            {
                hoveredMetadataRight = sb.ToString(); 
                hapticsRight.SendHapticImpulse(HOVER_ON_HAPTICS_AMP, HOVER_ON_HAPTICS_DUR);
            }
            
            return; 
        }
        
        if (isLeft)
        {
            if (!hoverOnLeft) return;
            hoverOnLeft = false;
            Debug.Log("Hover off left!");
            
            hoveredMetadataLeft = string.Empty;
            hitLocationLeftObject.transform.position = new Vector3(HIT_LOCATION_RESET, HIT_LOCATION_RESET, HIT_LOCATION_RESET);
            leftLineVisual.noValidHitProperties.gradient = hoverOffLineGradient;
        }
        else
        {
            if (!hoverOnRight) return;
            hoverOnRight = false;
            Debug.Log("Hover off right!");
            hoveredMetadataRight = string.Empty;
            hitLocationRightObject.transform.position = new Vector3(HIT_LOCATION_RESET, HIT_LOCATION_RESET, HIT_LOCATION_RESET);
            rightLineVisual.noValidHitProperties.gradient = hoverOffLineGradient;
        }
    }

    private void OnTileViewChange(InputAction.CallbackContext obj)
    {
        int maxValue = Enum.GetNames(typeof(TileType)).Length;
        currentTileViewIndex++;
        if (currentTileViewIndex == maxValue) currentTileViewIndex = 0;
        
        switch (currentTileViewIndex)
        {
            case 0:
                GameEvents.OnTileViewChanged(TileType.All);
                tileViewTextLeft.text = "View mode: ALL";
                tileViewTextRight.text = "View mode: ALL";
                break;
            case 1:
                GameEvents.OnTileViewChanged(TileType.Lot);
                tileViewTextLeft.text = "View mode: LOT";
                tileViewTextRight.text = "View mode: LOT";
                break;
            case 2:
                GameEvents.OnTileViewChanged(TileType.Common);
                tileViewTextLeft.text = "View mode: COMMON";
                tileViewTextRight.text = "View mode: COMMON";
                break;
        }
        
        Debug.Log($"Tile view switched to {(TileType)currentTileViewIndex}.");
    }
}
