using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueCameraShake : MonoBehaviour
{
    [SerializeField] private Transform shakeTarget;

    private Coroutine shakeRoutine;
    private Vector3 baseLocalPosition;

    public void SetTarget(Transform target)
    {
        if (shakeTarget == target)
        {
            return;
        }

        StopShake();
        shakeTarget = target;

        if (shakeTarget != null)
        {
            baseLocalPosition = shakeTarget.localPosition;
        }
    }

    public void PlayShake(float duration, float magnitude, float frequency)
    {
        if (shakeTarget == null || duration <= 0f || magnitude <= 0f)
        {
            return;
        }

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
        }

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude, frequency));
    }

    public void StopShake()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (shakeTarget != null)
        {
            shakeTarget.localPosition = baseLocalPosition;
        }
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude, float frequency)
    {
        baseLocalPosition = shakeTarget.localPosition;
        float elapsed = 0f;
        float sampledFrequency = Mathf.Max(1f, frequency);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float sampleTime = elapsed * sampledFrequency;
            float offsetX = (Mathf.PerlinNoise(sampleTime, 0.17f) - 0.5f) * 2f * magnitude;
            float offsetY = (Mathf.PerlinNoise(0.31f, sampleTime + 19.7f) - 0.5f) * 2f * magnitude;
            shakeTarget.localPosition = baseLocalPosition + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        shakeTarget.localPosition = baseLocalPosition;
        shakeRoutine = null;
    }

    private void OnDisable()
    {
        StopShake();
    }
}
