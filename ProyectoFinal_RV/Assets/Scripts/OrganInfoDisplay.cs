using System.Collections;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Controla la UI que muestra la información educativa de cada órgano.
/// </summary>
public class OrganInfoDisplay : MonoBehaviour
{
    private const string DefaultCloseHint = "Presiona el botón de información para cerrar";

    [Header("Referencias UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [SerializeField] private TextMeshProUGUI closeHintLabel;
    [SerializeField, Range(0.05f, 0.5f)] private float fadeDuration = 0.15f;
    [SerializeField, Tooltip("Escala del canvas world-space que se genera automáticamente.")]
    private float worldScale = 0.0023f;

    [Header("Feedback opcional")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip closeClip;

    [Header("Eventos")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    [Header("Alineación con la cámara")]
    [SerializeField, Tooltip("Mueve y orienta el panel frente a la cámara al mostrarse.")]
    private bool reanchorToCamera = true;
    [SerializeField, Tooltip("Distancia (m) frente a la cámara para colocar el panel.")]
    private float anchorDistance = 0.8f;
    [SerializeField, Tooltip("Altura adicional (m) sobre la cámara.")]
    private float anchorHeightOffset = 1.35f;
    [SerializeField, Tooltip("Mantiene el panel mirando hacia la cámara.")]
    private bool faceCamera = true;
    [SerializeField, Tooltip("Transform opcional que se usará como referencia para alinear el panel.")]
    private Transform anchorOverride;

    private Coroutine fadeRoutine;
    private OrganInfoInteractable currentOwner;
    private bool isVisible;
    private XROrigin xrOrigin;
    private Transform cachedCamera;

    public bool IsVisible => isVisible;
    public OrganInfoInteractable CurrentOwner => currentOwner;

    private void Awake()
    {
        xrOrigin = FindObjectOfType<XROrigin>();
        cachedCamera = ResolveCamera();

        if (canvasGroup == null)
        {
            BuildRuntimeCanvas();
        }

        SetInstantVisibility(0f);
    }

    public void Show(OrganInfoData data, OrganInfoInteractable owner)
    {
        if (titleLabel != null)
        {
            titleLabel.text = data.Title;
        }

        if (descriptionLabel != null)
        {
            descriptionLabel.text = data.Description;
        }

        currentOwner = owner;
        TryReanchorToCamera();
        ToggleVisibility(true);
        PlayClip(openClip);
        onOpened?.Invoke();
    }

    public void Hide()
    {
        if (!isVisible)
        {
            return;
        }

        ToggleVisibility(false);
        PlayClip(closeClip);

        var owner = currentOwner;
        currentOwner = null;
        owner?.OnDisplayClosed();

        onClosed?.Invoke();
    }

    private void ToggleVisibility(bool show)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeCanvas(show ? 1f : 0f));
    }

    private IEnumerator FadeCanvas(float targetAlpha)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        isVisible = targetAlpha > 0f;
        float startAlpha = canvasGroup.alpha;
        float time = 0f;

        canvasGroup.blocksRaycasts = isVisible;
        canvasGroup.interactable = isVisible;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        fadeRoutine = null;
    }

    private void SetInstantVisibility(float alpha)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = alpha;
        canvasGroup.blocksRaycasts = alpha > 0f;
        canvasGroup.interactable = alpha > 0f;
        isVisible = alpha > 0f;
    }

    private void LateUpdate()
    {
        if (!faceCamera || !isVisible)
        {
            return;
        }

        var reference = ResolveCamera();
        if (reference == null)
        {
            return;
        }

        var forward = reference.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = reference.forward;
        }

        transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    public void SetAnchorRoot(Transform customRoot)
    {
        anchorOverride = customRoot;
    }

    private void TryReanchorToCamera()
    {
        if (!reanchorToCamera)
        {
            return;
        }

        var reference = ResolveCamera();
        if (reference == null)
        {
            return;
        }

        var root = anchorOverride != null ? anchorOverride : reference;
        var originPosition = root.position;
        var forward = reference.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = reference.forward;
        }
        forward = forward.normalized;

        var targetPosition = originPosition + forward * anchorDistance;
        targetPosition.y = originPosition.y + anchorHeightOffset;

        transform.position = targetPosition;

        if (faceCamera)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    private Transform ResolveCamera()
    {
        if (cachedCamera != null)
        {
            return cachedCamera;
        }

        if (xrOrigin != null && xrOrigin.Camera != null)
        {
            cachedCamera = xrOrigin.Camera.transform;
        }
        else if (Camera.main != null)
        {
            cachedCamera = Camera.main.transform;
        }

        return cachedCamera;
    }

    private void PlayClip(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void BuildRuntimeCanvas()
    {
        var canvasGo = new GameObject("Organ Info Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGo.transform.SetParent(transform, false);
        var rectTransform = canvasGo.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(640f, 420f);
        rectTransform.localScale = Vector3.one * worldScale;

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var anchorCamera = ResolveCamera();
        var worldCamera = anchorCamera != null
            ? anchorCamera.GetComponent<Camera>() ?? anchorCamera.GetComponentInParent<Camera>()
            : Camera.main;
        canvas.worldCamera = worldCamera;
        canvas.sortingOrder = 40;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        scaler.referencePixelsPerUnit = 100f;

        var graphicRaycaster = canvasGo.GetComponent<GraphicRaycaster>();
        graphicRaycaster.enabled = false;

        canvasGroup = canvasGo.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(rectTransform, false);
        var backdropRect = (RectTransform)backdrop.transform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        var image = backdrop.GetComponent<Image>();
        image.color = new Color(0.05f, 0.07f, 0.1f, 0.92f);

        titleLabel = CreateLabel(rectTransform, "Título", new Vector2(0.5f, 0.82f), FontStyles.Bold, 48);
        descriptionLabel = CreateLabel(rectTransform, "Descripción", new Vector2(0.5f, 0.45f), FontStyles.Normal, 32);
        descriptionLabel.enableWordWrapping = true;
        descriptionLabel.margin = new Vector4(20, 20, 20, 20);
        descriptionLabel.text = string.Empty;

        closeHintLabel = CreateLabel(rectTransform, DefaultCloseHint, new Vector2(0.5f, 0.08f), FontStyles.Italic, 26);
        closeHintLabel.color = new Color(0.83f, 0.83f, 0.83f, 0.9f);
    }

    private TextMeshProUGUI CreateLabel(RectTransform parent, string defaultText, Vector2 anchor, FontStyles style, float fontSize)
    {
        var go = new GameObject(defaultText, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(600f, 120f);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }
}

public readonly struct OrganInfoData
{
    public string Title { get; }
    public string Description { get; }

    public OrganInfoData(string title, string description)
    {
        Title = title;
        Description = description;
    }
}

