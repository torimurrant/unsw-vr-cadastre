using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class VrFlyingTeleport : MonoBehaviour
{
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private GameObject teleportPreviewPrefab = null;
    [SerializeField] private MeshRenderer teleportFader = null;
    [SerializeField][Range(10.0f, 50.0f)] private float teleportDistance = 20.0f;
    [SerializeField] private GameObject leftController = null;
    [SerializeField] private CurveVisualController leftControllerVisual = null;
    [SerializeField] private GameObject rightController = null;
    [SerializeField] private CurveVisualController rightControllerVisual = null;
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
        vrControls.XRILeftInteraction.Select.performed += TeleportLeft;
        vrControls.XRIRightInteraction.Select.performed += TeleportRight;
        // vrControls.XRILeftLocomotion.TeleportModeCancel.performed += CancelTeleport;
        // vrControls.XRIRightLocomotion.TeleportModeCancel.performed += CancelTeleport;
    }
    
    private void OnDisable()
    {
        vrControls.Disable();
        vrControls.XRILeftInteraction.Select.performed -= TeleportLeft;
        vrControls.XRIRightInteraction.Select.performed -= TeleportRight;
        // vrControls.XRILeftLocomotion.TeleportModeCancel.performed -= CancelTeleport;
        // vrControls.XRIRightLocomotion.TeleportModeCancel.performed -= CancelTeleport;
    }

    private void TeleportLeft(InputAction.CallbackContext ctx)
    {
        int num = (int)ctx.ReadValue<float>();
        activeController = leftController.transform;
        Teleport(num);
    }

    private void TeleportRight(InputAction.CallbackContext ctx)
    {
        int num = (int)ctx.ReadValue<float>();
        activeController = rightController.transform;
        Teleport(num);
    }

    private void Teleport(int value)
    {
        switch (value)
        {
            case 1:
                StartTeleport();
                break;
            case 0:
                ConfirmTeleport();
                break;
        }
    }
    
    private void StartTeleport()
    {
        if (teleporting || previewing) return;
        previewing = true;
        if (teleportPreview) Destroy(teleportPreview);
        teleportPreview = Instantiate(teleportPreviewPrefab, teleportPosition, Quaternion.identity);
    }
    
    // private void CancelTeleport(InputAction.CallbackContext ctx)
    // {
    //     if (teleportCoroutine != null) StopCoroutine(teleportCoroutine);
    //     // leftController.SetActive(true);
    //     // rightController.SetActive(true);
    //     teleporting = false;
    //     Destroy(teleportPreview);
    // }
    
    private void ConfirmTeleport()
    {
        if (teleporting) return;
        if (teleportCoroutine != null) StopCoroutine(teleportCoroutine);
        teleportCoroutine = StartCoroutine(TeleportCoroutine());
    }
    
    private IEnumerator TeleportCoroutine()
    {
        teleporting = true;
        leftControllerVisual.enabled = false;
        rightControllerVisual.enabled = false;
        yield return new WaitForSeconds(0.1f);
        teleportFader.material.DOFade(1.0f, TELEPORT_FADE_DURATION).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(TELEPORT_FADE_DURATION);
        this.transform.position = teleportPosition;
        Destroy(teleportPreview);
        yield return new WaitForSeconds(0.1f);
        teleportFader.material.DOFade(0.0f, TELEPORT_FADE_DURATION).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(TELEPORT_FADE_DURATION + 0.1f);
        leftControllerVisual.enabled = true;
        rightControllerVisual.enabled = true;
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