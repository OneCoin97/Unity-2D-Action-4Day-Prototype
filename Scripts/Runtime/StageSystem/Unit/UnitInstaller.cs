using UnityEngine;
using UnitComponents;

[DisallowMultipleComponent]
public sealed class UnitInstaller : MonoBehaviour
{
    #region Inspector

    [Header("Player Spawn")]
    [SerializeField] private Vector2 playerSpawnPosition;

    [Header("Spawn Area")]
    [SerializeField] private Vector2 spawnAreaOffset;
    [SerializeField, Min(0f)] private float spawnAreaWidth = 10f;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer = 1 << 8;
    [SerializeField, Min(0f)] private float groundCheckDistance = 10f;
    [SerializeField, Min(0f)] private float unitHeightFromGround = 0.5f;

    [Header("Hierarchy")]
    [SerializeField] private Transform installedUnitParent;

    [Header("Spawn Effect")]
    [SerializeField] private AnimationClip spawnEffectClip;
    [SerializeField] private Vector2 spawnEffectOffset;

    #endregion

    public void installPlayer(Unit unit)
    {
        install(unit,playerSpawnPosition);
    }

    public bool tryInstall(Unit unit)
    {
        Vector2 groundCheckPosition = getGroundCheckPosition();
        RaycastHit2D groundHit = Physics2D.Raycast(
            groundCheckPosition,
            Vector2.down,
            groundCheckDistance,
            groundLayer);

        if (groundHit.collider == null)
        {
            return false;
        }

        Vector2 installPosition = groundHit.point + Vector2.up * unitHeightFromGround;
        install(unit,installPosition);
        return true;
    }

    private void install(Unit unit,Vector2 installPosition)
    {
        unit.transform.SetParent(installedUnitParent);
        unit.transform.SetPositionAndRotation(installPosition,Quaternion.identity);
        unit.gameObject.SetActive(true);

        Vector2 spawnEffectPosition = installPosition + spawnEffectOffset;
        EffectPool.instance.playEffect(spawnEffectClip,spawnEffectPosition,1,unit.gameObject.layer);
    }

    private Vector2 getGroundCheckPosition()
    {
        Vector2 installAreaCenter = (Vector2)transform.position + spawnAreaOffset;
        float positionX = Random.Range(-spawnAreaWidth * 0.5f,spawnAreaWidth * 0.5f);
        return installAreaCenter + Vector2.right * positionX;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 installAreaCenter = (Vector2)transform.position + spawnAreaOffset;
        Vector2 leftGroundCheckPosition = installAreaCenter + Vector2.left * spawnAreaWidth * 0.5f;
        Vector2 rightGroundCheckPosition = installAreaCenter + Vector2.right * spawnAreaWidth * 0.5f;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(leftGroundCheckPosition,rightGroundCheckPosition);
        Gizmos.DrawLine(leftGroundCheckPosition,leftGroundCheckPosition + Vector2.down * groundCheckDistance);
        Gizmos.DrawLine(rightGroundCheckPosition,rightGroundCheckPosition + Vector2.down * groundCheckDistance);
    }

    private void OnValidate()
    {
        spawnAreaWidth = Mathf.Max(0f,spawnAreaWidth);
        groundCheckDistance = Mathf.Max(0f,groundCheckDistance);
        unitHeightFromGround = Mathf.Max(0f,unitHeightFromGround);
    }
}
