using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Gestiona la interacción diegética de cada órgano en VR.
/// Muestra un tooltip contextual y delega la visualización detallada en OrganInfoDisplay.
/// </summary>
public class OrganInfoInteractable : MonoBehaviour
{
    [Header("Contenido")]
    [SerializeField] private string organTitle = "Órgano";
    [TextArea(3, 8)]
    [SerializeField] private string organDescription;

    [Header("Detección de cercanía")]
    [SerializeField, Tooltip("Distancia (m) a la que aparece el prompt al jugador.")]
    private float promptDistance = 1.25f;
    [SerializeField, Tooltip("Distancia (m) máxima antes de cerrar la ficha automáticamente.")]
    private float autoHideDistance = 2.5f;
    [SerializeField] private Transform distanceReference;
    [SerializeField, Tooltip("Offset local del prompt respecto al órgano.")]
    private Vector3 promptOffset = new Vector3(0f, 0.18f, 0f);

    [Header("UI de prompt")]
    [SerializeField] private CanvasGroup promptCanvas;
    [SerializeField] private TextMeshProUGUI promptLabel;
    [SerializeField] private string promptText = "Presiona botón A para ver la información";
    [SerializeField, Range(0.0015f, 0.01f)] private float promptWorldScale = 0.0022f;

    [Header("Sistema de visualización")]
    [SerializeField] private OrganInfoDisplay infoDisplay;
    [SerializeField]
    private InputActionProperty infoButton = new InputActionProperty(
        new InputAction("Mostrar órgano", InputActionType.Button, "<XRController>{RightHand}/primaryButton"));
    [SerializeField] private XROrigin xrOrigin;

    [Header("Feedback opcional")]
    [SerializeField] private AudioSource spatialAudioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip openClip;

    private bool isPromptVisible;
    private bool hasInputSubscribed;
    private RectTransform promptRect;

    private Transform CameraTransform => xrOrigin != null ? xrOrigin.Camera.transform : Camera.main?.transform;
    private Transform DistanceOrigin => distanceReference != null ? distanceReference : transform;

    private void Reset()
    {
        promptLabel = GetComponentInChildren<TextMeshProUGUI>(true);
        promptCanvas = promptLabel != null ? promptLabel.GetComponentInParent<CanvasGroup>() : null;
        distanceReference = transform;
        promptRect = promptCanvas != null ? promptCanvas.GetComponent<RectTransform>() : null;
    }

    private void Awake()
    {
        TryCacheReferences();
        EnsurePromptUI();
        ConfigurePrompt(false);
        SubscribeInput();
    }

    private void OnEnable()
    {
        infoButton.action?.Enable();
    }

    private void OnDisable()
    {
        infoButton.action?.Disable();
        if (infoDisplay != null && infoDisplay.CurrentOwner == this)
        {
            infoDisplay.Hide();
        }

        ConfigurePrompt(false);
    }

    private void OnDestroy()
    {
        UnsubscribeInput();
    }

    private void Update()
    {
        if (!TryCacheReferences())
        {
            return;
        }

        UpdatePromptTransform();

        var cameraTransform = CameraTransform;
        if (cameraTransform == null)
        {
            return;
        }

        float distance = Vector3.Distance(cameraTransform.position, DistanceOrigin.position);
        bool shouldShowPrompt = distance <= promptDistance && (infoDisplay == null || !infoDisplay.IsVisible);

        if (shouldShowPrompt != isPromptVisible)
        {
            ConfigurePrompt(shouldShowPrompt);

            if (shouldShowPrompt)
            {
                PlayClip(hoverClip);
            }
        }

        if (infoDisplay != null && infoDisplay.IsVisible && infoDisplay.CurrentOwner == this && distance > autoHideDistance)
        {
            infoDisplay.Hide();
        }

        if (Keyboard.current != null && (Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame))
        {
            if (infoDisplay == null)
            {
                return;
            }

            if (infoDisplay.IsVisible && infoDisplay.CurrentOwner == this)
            {
                infoDisplay.Hide();
                return;
            }

            if (!isPromptVisible)
            {
                return;
            }

            infoDisplay.Show(new OrganInfoData(organTitle, organDescription), this);
            ConfigurePrompt(false);
            PlayClip(openClip);
        }
    }

    public void OnDisplayClosed()
    {
        if (IsWithinPromptDistance())
        {
            ConfigurePrompt(true);
        }
    }

    private void SubscribeInput()
    {
        if (hasInputSubscribed)
        {
            return;
        }

        var action = infoButton.action;
        if (action == null)
        {
            return;
        }

        action.performed += OnInfoPressed;
        hasInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!hasInputSubscribed)
        {
            return;
        }

        var action = infoButton.action;
        if (action == null)
        {
            hasInputSubscribed = false;
            return;
        }

        action.performed -= OnInfoPressed;
        hasInputSubscribed = false;
    }

    private void OnInfoPressed(InputAction.CallbackContext context)
    {
        if (!context.performed || infoDisplay == null)
        {
            return;
        }

        if (infoDisplay.IsVisible && infoDisplay.CurrentOwner == this)
        {
            infoDisplay.Hide();
            return;
        }

        if (!isPromptVisible)
        {
            return;
        }

        infoDisplay.Show(new OrganInfoData(organTitle, organDescription), this);
        ConfigurePrompt(false);
        PlayClip(openClip);
    }

    private bool TryCacheReferences()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        if (infoDisplay == null)
        {
            infoDisplay = FindObjectOfType<OrganInfoDisplay>(true);
        }

        if (distanceReference == null)
        {
            distanceReference = transform;
        }

        if (promptCanvas == null || promptLabel == null)
        {
            EnsurePromptUI();
        }

        return xrOrigin != null && infoDisplay != null && promptCanvas != null && promptLabel != null;
    }

    private void EnsurePromptUI()
    {
        if (promptCanvas != null && promptLabel != null)
        {
            promptRect = promptCanvas.GetComponent<RectTransform>();
            return;
        }

        var promptRoot = new GameObject($"{name}_Prompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        var rectTransform = promptRoot.GetComponent<RectTransform>();
        rectTransform.SetParent(transform, false);
        rectTransform.localPosition = promptOffset;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one * promptWorldScale;

        var canvas = promptRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 60;
        canvas.worldCamera = Camera.main;

        var scaler = promptRoot.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        promptCanvas = promptRoot.GetComponent<CanvasGroup>();
        promptCanvas.alpha = 0f;
        promptCanvas.blocksRaycasts = false;
        promptCanvas.interactable = false;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.SetParent(rectTransform, false);
        labelRect.sizeDelta = new Vector2(450f, 150f);

        promptLabel = labelGo.GetComponent<TextMeshProUGUI>();
        promptLabel.fontSize = 38f;
        promptLabel.alignment = TextAlignmentOptions.Center;
        promptLabel.text = promptText;

        promptRect = rectTransform;
    }

    private void ConfigurePrompt(bool show)
    {
        if (promptCanvas == null)
        {
            return;
        }

        isPromptVisible = show;
        promptCanvas.alpha = show ? 1f : 0f;
        promptCanvas.blocksRaycasts = show;
        promptCanvas.interactable = false;

        if (promptLabel != null)
        {
            promptLabel.text = promptText;
        }
    }

    private void UpdatePromptTransform()
    {
        if (promptRect == null)
        {
            return;
        }

        promptRect.position = DistanceOrigin.position + promptOffset;

        var camera = CameraTransform;
        if (camera == null)
        {
            return;
        }

        var lookDirection = promptRect.position - camera.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            promptRect.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private bool IsWithinPromptDistance()
    {
        var cam = CameraTransform;
        if (cam == null)
        {
            return false;
        }

        return Vector3.Distance(cam.position, DistanceOrigin.position) <= promptDistance;
    }

    public void SetOrganContent(string title, string description)
    {
        organTitle = title;
        organDescription = description;
    }

    public void SetPromptSettings(float showDistance, float hideDistance, Vector3 offset)
    {
        promptDistance = showDistance;
        autoHideDistance = hideDistance;
        promptOffset = offset;
        UpdatePromptTransform();
    }

    public void SetInfoDisplay(OrganInfoDisplay display)
    {
        infoDisplay = display;
    }

    private void PlayClip(AudioClip clip)
    {
        if (spatialAudioSource != null && clip != null)
        {
            spatialAudioSource.PlayOneShot(clip);
        }
    }
}

