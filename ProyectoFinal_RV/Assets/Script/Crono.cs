using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Crono : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI textoCrono;
    [SerializeField] private float tiempo;
    [SerializeField] private GameObject tiempoTerminado;

    private bool tiempoDetenido = false;

    private int tiempoMinutos, tiemposegundos, tiempoDecimasSegundo;

    void Start()
    {
        tiempoDetenido = false;
        if (tiempoTerminado != null)
        {
            tiempoTerminado.SetActive(false);
        }
    }

    void Cronometro()
    {
        if (!tiempoDetenido && tiempo > 0)
        {
            tiempo -= Time.deltaTime;
            
            if (tiempo < 0)
            {
                tiempo = 0;
            }
        }

        tiempoMinutos = Mathf.FloorToInt(tiempo / 60);
        tiemposegundos = Mathf.FloorToInt(tiempo % 60);
        tiempoDecimasSegundo = Mathf.FloorToInt((tiempo % 1) * 100);

        textoCrono.text = string.Format("{0:00}:{1:00}:{2:00}", tiempoMinutos, tiemposegundos, tiempoDecimasSegundo);

        if (tiempo <= 0 && !tiempoDetenido)
        {
            tiempoDetenido = true;
            tiempo = 0;
            if (tiempoTerminado != null)
            {
                tiempoTerminado.SetActive(true);
            }
        }
    }

    void Update()
    {
        Cronometro();
    }
}