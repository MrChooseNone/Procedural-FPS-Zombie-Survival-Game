using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    

    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.1f;
    public float shakeSpeed = 10f; // Speed of noise movement

    private Vector3 originalPosition;
    private float timeElapsed = 0f;
    public Camera camera;
     private Coroutine shakeCoroutine;

    public void Shake(float shakeMagnitude, float shakeDuration )
    {
         // Stop any ongoing shake to prevent stacking issues
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            camera.transform.localPosition = originalPosition; // Ensure reset before a new shake
        }

        originalPosition = camera.transform.localPosition;
        shakeCoroutine = StartCoroutine(ShakeCoroutine(shakeMagnitude,shakeDuration));
    }

    private IEnumerator ShakeCoroutine(float shakeMagnitude, float shakeDuration )
    {
        float timeElapsed = 0f;
        float randomStart = Random.value * 100f;

        while (timeElapsed < shakeDuration)
        {
            float xOffset = (Mathf.PerlinNoise(randomStart, timeElapsed * shakeSpeed) - 0.5f) * shakeMagnitude * 2;
            float yOffset = (Mathf.PerlinNoise(randomStart + 1, timeElapsed * shakeSpeed) - 0.5f) * shakeMagnitude * 2;

            camera.transform.localPosition = originalPosition + new Vector3(xOffset, yOffset, 0);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the camera always resets back
        camera.transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}
