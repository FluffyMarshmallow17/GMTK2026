using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform player;
    public float smoothSpeed = 5f;

    Transform overrideTarget;
    bool locked;

    bool easingLock;
    Vector3 easeFrom;
    float easeDuration;
    float easeElapsed;

    public void LockOnto(Transform target, float speed = 22f)
    {
        // Kept for simple callers; prefer LockOntoEased for cinematics.
        overrideTarget = target;
        locked = true;
        easingLock = false;
        _ = speed;
    }

    /// <summary>
    /// Whip the camera onto <paramref name="target"/> over <paramref name="duration"/>
    /// realtime seconds with a strong ease-out (fast launch, soft settle).
    /// </summary>
    public void LockOntoEased(Transform target, float duration)
    {
        overrideTarget = target;
        locked = true;
        easingLock = true;
        easeFrom = transform.position;
        easeDuration = Mathf.Max(0.01f, duration);
        easeElapsed = 0f;
    }

    public void ClearLock()
    {
        locked = false;
        easingLock = false;
        overrideTarget = null;
    }

    void LateUpdate()
    {
        Transform target = locked && overrideTarget != null ? overrideTarget : player;
        if (target == null)
            return;

        Vector3 targetPosition = target.position;
        targetPosition.z = transform.position.z;

        if (easingLock && locked)
        {
            easeElapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(easeElapsed / easeDuration);
            // Ease-out expo: snaps hard toward the subject, then crawls into place.
            float t = u >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * u);
            // Keep the end anchored to a moving target (boss/player).
            transform.position = Vector3.LerpUnclamped(easeFrom, targetPosition, t);
            if (u >= 1f)
                easingLock = false;
            return;
        }

        float speed = locked ? 10f : smoothSpeed;
        float dt = locked ? Time.unscaledDeltaTime : Time.deltaTime;
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            1f - Mathf.Exp(-speed * dt)
        );
    }
}
