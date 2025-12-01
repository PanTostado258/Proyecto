using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

[DefaultExecutionOrder(-450)]
public class VRSceneConfigurator : MonoBehaviour
{
    [System.Serializable]
    private class OrganEntry
    {
        public string objectName;
        public string title;
        [TextArea(4, 10)] public string description;
        public Vector3 promptLocalOffset = new Vector3(0f, 0.2f, 0f);
        public float promptDistance = 1.2f;
        public float autoHideDistance = 2.4f;
    }

    [Header("Referencias principales")]
    [SerializeField] private XROrigin xrOrigin;
    [SerializeField] private LocomotionSystem locomotionSystem;
    [SerializeField] private TeleportationProvider teleportationProvider;
    [SerializeField] private Transform uiParentOverride;
    [SerializeField] private OrganInfoDisplay sharedDisplay;

    [Header("Datos de órganos")]
    [SerializeField] private List<OrganEntry> organs = new List<OrganEntry>
    {
        new OrganEntry
        {
            objectName = "tripo_convert_860e35ee-1f00-418f-836c-780e181f16fd",
            title = "Corazón",
            description = "Bombea la sangre para distribuir oxígeno y nutrientes por todo el cuerpo. Late de 60‑100 veces por minuto en reposo.",
            promptLocalOffset = new Vector3(0f, 0.35f, 0f)
        },
        new OrganEntry
        {
            objectName = "tripo_convert_0d52ff7d-359f-469b-91ad-ca2cadf2be2b",
            title = "Pulmones",
            description = "Intercambian oxígeno y dióxido de carbono. El pulmón derecho tiene tres lóbulos y el izquierdo dos para dejar espacio al corazón.",
            promptLocalOffset = new Vector3(0f, 0.4f, 0f)
        },
        new OrganEntry
        {
            objectName = "tripo_convert_2953b72e-c99a-4d95-bff3-7d00e365e319",
            title = "Hígado",
            description = "Metaboliza nutrientes, depura toxinas y produce bilis para la digestión de grasas. Puede regenerar parte de su masa si se daña.",
            promptLocalOffset = new Vector3(0.05f, 0.25f, 0f)
        },
        new OrganEntry
        {
            objectName = "tripo_convert_14da3b8f-2296-4de7-8861-f99473f15439",
            title = "Riñones",
            description = "Filtran la sangre y producen orina para eliminar desechos. También regulan la presión arterial y el equilibrio de líquidos.",
            promptLocalOffset = new Vector3(-0.02f, 0.25f, 0f)
        }
    };

    private readonly List<BaseTeleportationInteractable> teleportSurfaces = new();

    private void Awake()
    {
        CacheReferences();
        SetupLocomotion();
        var display = EnsureDisplay();
        ConfigureOrgans(display);
    }

    private void CacheReferences()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindObjectOfType<XROrigin>();
        }

        if (locomotionSystem == null)
        {
            locomotionSystem = FindObjectOfType<LocomotionSystem>();
            if (locomotionSystem == null)
            {
                locomotionSystem = new GameObject("Locomotion System", typeof(LocomotionSystem)).GetComponent<LocomotionSystem>();
            }
        }

        if (teleportationProvider == null)
        {
            teleportationProvider = FindObjectOfType<TeleportationProvider>();
        }

        teleportSurfaces.Clear();
        teleportSurfaces.AddRange(FindObjectsOfType<BaseTeleportationInteractable>(true));
    }

    private void SetupLocomotion()
    {
        if (xrOrigin == null || locomotionSystem == null)
        {
            Debug.LogWarning("No se pudo configurar la locomoción: faltan referencias a XROrigin o LocomotionSystem.");
            return;
        }

        locomotionSystem.xrOrigin = xrOrigin;

        var snapTurn = locomotionSystem.GetComponent<ActionBasedSnapTurnProvider>() ??
                       locomotionSystem.gameObject.AddComponent<ActionBasedSnapTurnProvider>();
        snapTurn.system = locomotionSystem;

        var continuousTurn = locomotionSystem.GetComponent<ActionBasedContinuousTurnProvider>() ??
                             locomotionSystem.gameObject.AddComponent<ActionBasedContinuousTurnProvider>();
        continuousTurn.system = locomotionSystem;
        continuousTurn.turnSpeed = 60f;

        var moveProvider = xrOrigin.GetComponent<ActionBasedContinuousMoveProvider>() ??
                           xrOrigin.gameObject.AddComponent<ActionBasedContinuousMoveProvider>();
        moveProvider.system = locomotionSystem;
        moveProvider.forwardSource = xrOrigin.Camera != null ? xrOrigin.Camera.transform : moveProvider.forwardSource;
        moveProvider.enableStrafe = true;
        moveProvider.useGravity = false;
        moveProvider.gravityApplicationMode = ContinuousMoveProviderBase.GravityApplicationMode.Immediately;

        teleportationProvider ??= locomotionSystem.GetComponent<TeleportationProvider>() ??
                                   locomotionSystem.gameObject.AddComponent<TeleportationProvider>();
        teleportationProvider.system = locomotionSystem;

        ConfigureInputAction(snapTurn.leftHandSnapTurnAction, value => snapTurn.leftHandSnapTurnAction = value,
            "Left Hand Snap Turn", "<XRController>{LeftHand}/primary2DAxis");
        ConfigureInputAction(snapTurn.rightHandSnapTurnAction, value => snapTurn.rightHandSnapTurnAction = value,
            "Right Hand Snap Turn", "<XRController>{RightHand}/primary2DAxis");

        ConfigureInputAction(continuousTurn.leftHandTurnAction, value => continuousTurn.leftHandTurnAction = value,
            "Left Hand Turn", "<XRController>{LeftHand}/primary2DAxis");
        ConfigureInputAction(continuousTurn.rightHandTurnAction, value => continuousTurn.rightHandTurnAction = value,
            "Right Hand Turn", "<XRController>{RightHand}/primary2DAxis");

        ConfigureInputAction(moveProvider.leftHandMoveAction, value => moveProvider.leftHandMoveAction = value,
            "Left Hand Move", "<XRController>{LeftHand}/primary2DAxis");
        ConfigureInputAction(moveProvider.rightHandMoveAction, value => moveProvider.rightHandMoveAction = value,
            "Right Hand Move", "<XRController>{RightHand}/primary2DAxis");

        foreach (var surface in teleportSurfaces.Where(s => s != null))
        {
            surface.teleportationProvider = teleportationProvider;
        }

        var comfort = FindObjectOfType<ComfortOptionsManager>() ??
                      locomotionSystem.gameObject.AddComponent<ComfortOptionsManager>();
        comfort.Initialize(teleportationProvider, moveProvider, teleportSurfaces);

        var turnPrefSync = FindObjectOfType<SetTurnTypeFromPlayerPref>() ??
                           locomotionSystem.gameObject.AddComponent<SetTurnTypeFromPlayerPref>();
        turnPrefSync.snapTurn = snapTurn;
        turnPrefSync.continuousTurn = continuousTurn;
        turnPrefSync.ApplyPlayerPref();
    }

    private OrganInfoDisplay EnsureDisplay()
    {
        if (sharedDisplay == null)
        {
            sharedDisplay = FindObjectOfType<OrganInfoDisplay>(true);
        }

        if (sharedDisplay == null)
        {
            Transform parent = uiParentOverride;
            if (parent == null && xrOrigin != null)
            {
                parent = xrOrigin.CameraFloorOffsetObject != null
                    ? xrOrigin.CameraFloorOffsetObject.transform
                    : xrOrigin.transform;
            }

            var go = new GameObject("Organ Info Display");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 1.3f, 0.6f);
            sharedDisplay = go.AddComponent<OrganInfoDisplay>();
        }

        if (uiParentOverride == null && xrOrigin != null)
        {
            uiParentOverride = xrOrigin.CameraFloorOffsetObject != null
                ? xrOrigin.CameraFloorOffsetObject.transform
                : xrOrigin.transform;
        }

        sharedDisplay.SetAnchorRoot(uiParentOverride);
        return sharedDisplay;
    }

    private void ConfigureOrgans(OrganInfoDisplay display)
    {
        foreach (var entry in organs)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.objectName))
            {
                continue;
            }

            var target = FindOrgan(entry.objectName);
            if (target == null)
            {
                Debug.LogWarning($"No se encontró el objeto \"{entry.objectName}\" en la escena para configurar el órgano.");
                continue;
            }

            var targetGameObject = target.gameObject;
            var interactable = targetGameObject.GetComponent<OrganInfoInteractable>() ??
                               targetGameObject.AddComponent<OrganInfoInteractable>();

            interactable.SetOrganContent(entry.title, entry.description);
            interactable.SetPromptSettings(entry.promptDistance, entry.autoHideDistance, entry.promptLocalOffset);
            interactable.SetInfoDisplay(display);
        }
    }

    private static void ConfigureInputAction(InputActionProperty property, System.Action<InputActionProperty> setter, string name, string binding)
    {
        if (HasValidBinding(property))
        {
            return;
        }

        var action = new InputAction(name, InputActionType.Value, binding, null, null, "Vector2");
        setter(new InputActionProperty(action));
    }

    private static bool HasValidBinding(InputActionProperty property)
    {
        if (property.reference != null)
        {
            return true;
        }

        var action = property.action;
        return action != null && action.bindings.Count > 0;
    }

    private Transform FindOrgan(string objectName)
    {
        var allTransforms = FindObjectsOfType<Transform>(true);
        return allTransforms.FirstOrDefault(t => t.name == objectName);
    }
}

