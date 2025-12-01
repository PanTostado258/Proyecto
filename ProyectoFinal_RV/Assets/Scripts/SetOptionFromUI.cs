using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class SetOptionFromUI : MonoBehaviour
{
    public Scrollbar volumeSlider;
    public TMP_Dropdown turnDropdown;
    public TMP_Dropdown locomotionDropdown;
    public SetTurnTypeFromPlayerPref turnTypeFromPlayerPref;
    public ComfortOptionsManager comfortOptionsManager;

    private void Start()
    {
        volumeSlider.onValueChanged.AddListener(SetGlobalVolume);
        turnDropdown.onValueChanged.AddListener(SetTurnPlayerPref);
        if (locomotionDropdown != null)
        {
            locomotionDropdown.onValueChanged.AddListener(SetLocomotionPref);
        }

        if (PlayerPrefs.HasKey("turn"))
            turnDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("turn"));

        if (locomotionDropdown != null)
        {
            locomotionDropdown.SetValueWithoutNotify(PlayerPrefs.GetInt("locomotionMode", 0));
        }
        
        // Buscar automáticamente si no está asignado
        if (turnTypeFromPlayerPref == null)
        {
            turnTypeFromPlayerPref = FindObjectOfType<SetTurnTypeFromPlayerPref>();
            
            if (turnTypeFromPlayerPref == null)
            {
                Debug.LogError("No se encontró SetTurnTypeFromPlayerPref en la escena!");
            }
        }

        if (comfortOptionsManager == null)
        {
            comfortOptionsManager = FindObjectOfType<ComfortOptionsManager>();
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

    public void SetLocomotionPref(int value)
    {
        PlayerPrefs.SetInt("locomotionMode", value);

        if (comfortOptionsManager != null)
        {
            comfortOptionsManager.SetMode(value);
        }
        else
        {
            Debug.LogWarning("comfortOptionsManager no está asignado. La preferencia se guardó pero no se aplicó.");
        }
    }
}