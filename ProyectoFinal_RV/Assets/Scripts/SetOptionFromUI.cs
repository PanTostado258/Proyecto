using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class SetOptionFromUI : MonoBehaviour
{
    public Scrollbar volumeSlider;
    public TMPro.TMP_Dropdown turnDropdown;
    public SetTurnTypeFromPlayerPref turnTypeFromPlayerPref;

    private void Start()
    {
        volumeSlider.onValueChanged.AddListener(SetGlobalVolume);
        turnDropdown.onValueChanged.AddListener(SetTurnPlayerPref);

        if (PlayerPrefs.HasKey("turn"))
            turnDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("turn"));
        
        // Buscar automáticamente si no está asignado
        if (turnTypeFromPlayerPref == null)
        {
            turnTypeFromPlayerPref = FindObjectOfType<SetTurnTypeFromPlayerPref>();
            
            if (turnTypeFromPlayerPref == null)
            {
                Debug.LogError("No se encontró SetTurnTypeFromPlayerPref en la escena!");
            }
        }
    }

    public void SetGlobalVolume(float value)
    {
        AudioListener.volume = value;
    }

    public void SetTurnPlayerPref(int value)
    {
        PlayerPrefs.SetInt("turn", value);
        
        // Verificar antes de usar
        if (turnTypeFromPlayerPref != null)
        {
            turnTypeFromPlayerPref.ApplyPlayerPref();
        }
        else
        {
            Debug.LogWarning("turnTypeFromPlayerPref no está asignado. Las preferencias se guardaron pero no se aplicaron.");
        }
    }
}