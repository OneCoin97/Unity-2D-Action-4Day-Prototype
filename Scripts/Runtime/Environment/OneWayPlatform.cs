using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class OneWayPlatform : MonoBehaviour
{
    private Collider2D platformCollider;

    private void Awake()
    {
        platformCollider = GetComponent<Collider2D>();

        PlatformEffector2D platformEffector = GetComponent<PlatformEffector2D>();
        if (platformEffector == null)
        {
            platformEffector = gameObject.AddComponent<PlatformEffector2D>();
        }

        platformEffector.useOneWay = true;
        platformEffector.surfaceArc = 170f;
        platformCollider.usedByEffector = true;
    }
}
