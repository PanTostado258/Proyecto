using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

/// <summary>
/// Expone opciones de confort para locomoción VR (teletransporte o movimiento continuo).
/// Diseñado para invocarse desde UI (botones o dropdowns) y recordar la preferencia del jugador.
/// </summary>
public class ComfortOptionsManager : MonoBehaviour
{
    private const string MovePrefKey = "locomotionMode";

    public enum LocomotionMode
    {
        Teleport = 0,
        Smooth = 1
    }

    [Header("Referencias de locomoción")]
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private List<BaseTeleportationInteractable> teleportInteractors = new();
    [SerializeField] private ActionBasedContinuousMoveProvider continuousMoveProvider;

    [Header("Eventos")]
    public UnityEvent onTeleportMode;
    public UnityEvent onSmoothMode;

    [Header("Configuración")]
    [SerializeField] private LocomotionMode defaultMode = LocomotionMode.Teleport;

    private LocomotionMode activeMode;

    public LocomotionMode DefaultLocomotionMode => defaultMode;

    public void SetMode(int modeValue)
    {
        var mode = (LocomotionMode)Mathf.Clamp(modeValue, 0, 1);
        SetMode(mode);
    }

    public void SetMode(LocomotionMode mode)
    {
        if (activeMode == mode)
        {
            return;
        }

        activeMode = mode;
        PlayerPrefs.SetInt(MovePrefKey, (int)mode);

        bool teleportEnabled = mode == LocomotionMode.Teleport;

        if (teleportationProvider != null)
        {
            teleportationProvider.enabled = teleportEnabled;
        }

        foreach (var interactor in teleportInteractors)
        {
            if (interactor != null)
            {
                interactor.gameObject.SetActive(teleportEnabled);
            }
        }

        if (continuousMoveProvider != null)
        {
            continuousMoveProvider.enabled = !teleportEnabled;
            ToggleInput(continuousMoveProvider.leftHandMoveAction, !teleportEnabled);
            ToggleInput(continuousMoveProvider.rightHandMoveAction, !teleportEnabled);
        }

        if (teleportEnabled)
        {
            onTeleportMode?.Invoke();
        }
        else
        {
            onSmoothMode?.Invoke();
        }
    }

    public void Initialize(TeleportationProvider provider, ActionBasedContinuousMoveProvider moveProvider, IEnumerable<BaseTeleportationInteractable> surfaces)
    {
        if (provider != null)
        {
            teleportationProvider = provider;
        }

        if (moveProvider != null)
        {
            continuousMoveProvider = moveProvider;
        }

        if (surfaces != null)
        {
            teleportInteractors = surfaces.Where(s => s != null).Distinct().ToList();
        }

        ApplySavedPreference();
    }

    public void ApplySavedPreference()
    {
        var saved = (LocomotionMode)PlayerPrefs.GetInt(MovePrefKey, (int)defaultMode);
        activeMode = saved == LocomotionMode.Teleport ? LocomotionMode.Smooth : LocomotionMode.Teleport; // fuerza Apply
        SetMode(saved);
    }

    private void CacheReferences()
    {
        if (teleportationProvider == null)
        {
            teleportationProvider = FindObjectOfType<TeleportationProvider>();
        }

        if (continuousMoveProvider == null)
        {
            continuousMoveProvider = FindObjectOfType<ActionBasedContinuousMoveProvider>();
        }

        if (teleportInteractors.Count == 0)
        {
            teleportInteractors = FindObjectsOfType<BaseTeleportationInteractable>(true).ToList();
        }
    }

    private void ToggleInput(UnityEngine.InputSystem.InputActionProperty property, bool enabled)
    {
        var action = property.action;
        if (action == null)
        {
            return;
        }

        if (enabled)
        {
            action.Enable();
        }
        else
        {
            action.Disable();
        }
    }
}

