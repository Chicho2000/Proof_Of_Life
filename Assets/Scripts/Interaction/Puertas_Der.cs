using System.Collections;
using UnityEngine;

public class Puertas_Der : Interactable
{
    [Header("Rotación")]
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float rotationDuration = 0.2f;

    private Quaternion closedRotation;
    private Coroutine rotationCoroutine;
    private bool isOpen;

    private void Awake()
    {
        closedRotation = transform.localRotation;
    }

    public override void Interact(GameObject interactor)
    {
        isOpen = !isOpen;

        Quaternion targetRotation = closedRotation * Quaternion.Euler(
            0f,
            isOpen ? openAngle : 0f,
            0f);

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine = StartCoroutine(RotateDoor(targetRotation));
    }

    public void OpenDoor()
    {
        if (!isOpen) Interact(null);
    }

    private IEnumerator RotateDoor(Quaternion targetRotation)
    {
        Quaternion initialRotation = transform.localRotation;
        float elapsedTime = 0f;

        while (elapsedTime < rotationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsedTime / rotationDuration);
            transform.localRotation = Quaternion.Slerp(
                initialRotation,
                targetRotation,
                progress);

            yield return null;
        }

        transform.localRotation = targetRotation;
        rotationCoroutine = null;
    }
}
