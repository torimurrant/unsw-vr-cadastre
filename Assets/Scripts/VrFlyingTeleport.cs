using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;

public class VrFlyingTeleport : MonoBehaviour
{
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private GameObject teleportPreviewPrefab = null;
    [SerializeField] private MeshRenderer teleportFader = null;
    [SerializeField][Range(10.0f, 50.0f)] private float teleportDistance = 20.0f;
    [SerializeField] private GameObject leftController = null;
    [SerializeField] private GameObject rightController = null;
    private Vector3 teleportVector = Vector3.zero;
    private Vector3 teleportPosition = Vector3.zero;
    private GameObject teleportPreview = null;
    private Coroutine teleportCoroutine = null;
    private bool previewing = false;
    private bool teleporting = false;
    private VrControls vrControls = null;
    private Color teleportFaderColour = new Color(0.0f, 0.0f, 0.0f, 0.0f);
    private Transform activeController = null;
    private const float TELEPORT_FADE_DURATION = 0.25f;
    
    private void Awake()
    {
        vrControls = new VrControls();
        teleportFader.material.SetColor("_BaseColor", teleportFaderColour);
    }

    private void OnEnable()
    {
        vrControls.Enable();
        vrControls.XRILeftLocomotion.TeleportMode.started += StartTeleport;
        vrControls.XRIRightLocomotion.TeleportMode.started += StartTeleport;
        vrControls.XRILeftLocomotion.TeleportMode.canceled += ConfirmTeleport;
        vrControls.XRIRightLocomotion.TeleportMode.canceled += ConfirmTeleport;
        vrControls.XRILeftLocomotion.TeleportModeCancel.performed += CancelTeleport;
        vrControls.XRIRightLocomotion.TeleportModeCancel.performed += CancelTeleport;
    }
    
    private void OnDisable()
    {
        vrControls.Disable();
        vrControls.XRILeftLocomotion.TeleportMode.started -= StartTeleport;
        vrControls.XRIRightLocomotion.TeleportMode.started -= StartTeleport;
        vrControls.XRILeftLocomotion.TeleportMode.canceled -= ConfirmTeleport;
        vrControls.XRIRightLocomotion.TeleportMode.canceled -= ConfirmTeleport;
        vrControls.XRILeftLocomotion.TeleportModeCancel.performed -= CancelTeleport;
        vrControls.XRIRightLocomotion.TeleportModeCancel.performed -= CancelTeleport;
    }

    private void StartTeleport(InputAction.CallbackContext ctx)
    {
        if (teleporting || previewing) return;
        previewing = true;
        activeController = ctx.action.actionMap == (InputActionMap)vrControls.XRILeftLocomotion ? leftController.transform : rightController.transform;
        if (teleportPreview) Destroy(teleportPreview);
        teleportPreview = Instantiate(teleportPreviewPrefab, teleportPosition, Quaternion.identity);
    }
    
    private void CancelTeleport(InputAction.CallbackContext ctx)
    {
        if (teleportCoroutine != null) StopCoroutine(teleportCoroutine);
        leftController.SetActive(true);
        rightController.SetActive(true);
        teleporting = false;
        Destroy(teleportPreview);
    }
    
    private void ConfirmTeleport(InputAction.CallbackContext ctx)
    {
        if (teleporting) return;
        if (teleportCoroutine != null) StopCoroutine(teleportCoroutine);
        teleportCoroutine = StartCoroutine(TeleportCoroutine());
    }
    
    private IEnumerator TeleportCoroutine()
    {
        teleporting = true;
        leftController.SetActive(false);
        rightController.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        teleportFader.material.DOFade(1.0f, TELEPORT_FADE_DURATION).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(TELEPORT_FADE_DURATION);
        this.transform.position = teleportPosition;
        Destroy(teleportPreview);
        yield return new WaitForSeconds(0.1f);
        teleportFader.material.DOFade(0.0f, TELEPORT_FADE_DURATION).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(TELEPORT_FADE_DURATION + 0.1f);
        leftController.SetActive(true);
        rightController.SetActive(true);
        teleporting = false;
        previewing = false;
    }

    private void Update()
    {
        if (!previewing) return;
        if (teleporting) return;
        if (!activeController) return;
        teleportVector = activeController.transform.forward;
        teleportPosition = activeController.position + teleportVector * teleportDistance;
        if (!teleportPreview) return;
        teleportPreview.transform.position = teleportPosition;
    }
}