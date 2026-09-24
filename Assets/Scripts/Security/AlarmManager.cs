using System;
using UnityEngine;

public class AlarmManager : MonoBehaviour
{
    public static AlarmManager Instance;

    // Evento global que se dispara cuando suena la alarma
    public static event Action<Transform> OnAlarmTriggered;

    [SerializeField] private bool isAlarmActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Metodo para activar la alarma
    public void TriggerAlarm(Transform intruder = null)
    {
        if (isAlarmActive)
        {
            return;
        }

        isAlarmActive = true;
        Debug.Log("Alarma Activada");

        if (intruder == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                intruder = player.transform;
            }
        }

        if (OnAlarmTriggered != null)
        {
            OnAlarmTriggered(intruder);
        }
    }

    public bool GetIsAlarmActive()
    {
        return isAlarmActive;
    }
}
