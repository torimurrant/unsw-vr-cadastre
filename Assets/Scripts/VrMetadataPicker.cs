using UnityEngine;
using System.Collections.Generic;
using CesiumForUnity;
using System;
using System.Text;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
    
    // We might need separate left/right functions if we need to distinguish between the 
    // two controller inputs, for some reason.
    private void OnTriggerPressLeft(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;      // ignore input when hovering over UI object
        if (hoveredMetadataLeft == string.Empty) return;
        selectedMetadataLeft = hoveredMetadataLeft;
        metadataTextLeft.text = selectedMetadataLeft;
    }
    
    private void OnTriggerPressRight(InputAction.CallbackContext ctx)
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (hoveredMetadataRight == string.Empty) return;
        selectedMetadataRight = hoveredMetadataRight;
        metadataTextRight.text = selectedMetadataRight;
    }
    
    // Right now, both controllers are raycasting all the time, so if the user is waving
    // both around the metadata will change a lot. We could perhaps only use one controller
    // for metadata hovering/picking (and maybe other related stuff like building 'level selection')
    // and the other controller for things like player movement. Or perhaps searching for metadata
    // could require a button press-hold, and then some other input for selecting (release, or button press).
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
        
        // Raycast hit detected
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
            }
            else
            {
                hoveredMetadataRight = sb.ToString(); 
            }
            
            return; 
        }
        
        // No raycast hit detected
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

    // Simple implementation of tile view changes will just cycle through the tile types
    private void OnTileViewChange(InputAction.CallbackContext obj)
    {
        int maxValue = Enum.GetNames(typeof(TileType)).Length;
        currentTileViewIndex++;
        if (currentTileViewIndex == maxValue) currentTileViewIndex = 0;
        
        switch (currentTileViewIndex)
        {
            case 0:
                GameEvents.OnTileViewChanged(TileType.All);
                tileViewTextLeft.text = "ALL";
                tileViewTextRight.text = "ALL";
                break;
            case 1:
                GameEvents.OnTileViewChanged(TileType.Lot);
                tileViewTextLeft.text = "LOT";
                tileViewTextRight.text = "LOT";
                break;
            case 2:
                GameEvents.OnTileViewChanged(TileType.Common);
                tileViewTextLeft.text = "COMMON";
                tileViewTextRight.text = "COMMON";
                break;
        }
        
        Debug.Log($"Tile view switched to {(TileType)currentTileViewIndex}.");
    }
    
    // private void CheckForHoverOff()
    // {
    //     bool isHoveringAny = hoverOnLeft || hoverOnRight;
    //
    //     if (isHoveringAny)
    //     {
    //         // Reset timer immediately when we have a valid hit
    //         hoverOffTimer = 0f;
    //         wasHoveringAnyLastFrame = true;
    //         return;
    //     }
    //
    //     // No hover this frame → accumulate time
    //     hoverOffTimer += Time.deltaTime;
    //     
    //     // Only trigger if we've been off for long enough
    //     if (wasHoveringAnyLastFrame && hoverOffTimer >= hoverOffDelay)
    //     {
    //         GameEvents.OnModelMetadataHoverOffAll();
    //         //Debug.Log("Hover OFF (debounced)");
    //         wasHoveringAnyLastFrame = false;
    //         hitLocationLeftObject.transform.position = new Vector3(HIT_LOCATION_RESET, HIT_LOCATION_RESET, HIT_LOCATION_RESET);
    //         leftLineVisual.noValidHitProperties.gradient = hoverOffLineGradient;
    //         hitLocationRightObject.transform.position = new Vector3(HIT_LOCATION_RESET, HIT_LOCATION_RESET, HIT_LOCATION_RESET);
    //         rightLineVisual.noValidHitProperties.gradient = hoverOffLineGradient;
    //     }
    // }
}