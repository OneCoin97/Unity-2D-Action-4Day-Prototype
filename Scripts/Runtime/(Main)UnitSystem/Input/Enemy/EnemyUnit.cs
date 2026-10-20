using UnityEngine;
using UnitComponents;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(UnitHitReceiver))]
[RequireComponent(typeof(UnitAudioController))]
[RequireComponent(typeof(UnitMoveController))]
[RequireComponent(typeof(UnitSkillLauncher))]
public abstract class EnemyUnit : Unit
{
    private const UnitActionStateType controlLockStateMask =
        UnitActionStateType.Spawn |
        UnitActionStateType.Hit |
        UnitActionStateType.HitEnd |
        UnitActionStateType.Dead |
        UnitActionStateType.Parry |
        UnitActionStateType.ParryEnd;

    #region Inspector

    [Header("Target")]
    [SerializeField, Min(0f)] private float targetHorizontalRange = 8f;
    [SerializeField, Min(0f)] private float targetHeightRange = 3f;
    [SerializeField, Min(0.02f)] private float targetSearchInterval = 0.2f;
    [SerializeField] private LayerMask targetUnitLayer = ~0;
    [SerializeField] private UnitTeamType targetTeamType = UnitTeamType.TeamA;

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackHeightRange = 1f;

    [Header("Patrol")]
    [SerializeField, Min(0f)] private float patrolRange = 3f;

    [Header("Runtime")]
    [SerializeField] protected Unit targetUnit;

    #endregion

    protected UnitMoveController moveController { get; private set; }

    private float spawnPositionX;
    private float nextTargetSearchTime;
    private int patrolDirection = 1;

    protected override void Awake()
    {
        base.Awake();
        moveController = getUnitComponent<UnitMoveController>();
        spawnPositionX = transform.position.x;
    }

    protected override void Update()
    {
        base.Update();

        if (Time.time >= nextTargetSearchTime) // 적 유닛이 많아질 때 발생하는 타깃 탐색 병목 방지용
        {
            nextTargetSearchTime = Time.time + targetSearchInterval;
            updateTarget();
        }

        if (moveController != null)
        {
            updateAI();
        }
    }

    protected override void OnDisable()
    {
        moveController.setMoveInput(0f);
        targetUnit = null;
        resetAI();
        base.OnDisable();
    }

    protected override void onSpawn()
    {
        spawnPositionX = transform.position.x;
        nextTargetSearchTime = 0f;
        patrolDirection = 1;
        targetUnit = null;
        resetAI();
    }

    protected abstract void updateAI();
    protected abstract void resetAI();

    protected bool isControlLocked()
    {
        return unitStateController.isCurrentActionState(controlLockStateMask);
    }

    protected void updateTarget()
    {
        if (isValidTarget(targetUnit) && isTargetRange(targetUnit))
        {
            return;
        }

        targetUnit = findNearestTarget();
    }

    protected bool isAttackHeight()
    {
        return targetUnit != null && getHeightDistance(targetUnit) <= attackHeightRange;
    }

    protected int getTargetDirection()
    {
        return targetUnit.transform.position.x >= transform.position.x ? 1 : -1;
    }

    protected float getHorizontalDistance(Unit target)
    {
        return Mathf.Abs(target.transform.position.x - transform.position.x);
    }

    protected void patrol()
    {
        UnitStateData stateData = unitStateController.stateData;
        if (stateData.isWall && stateData.wallDirection == patrolDirection)
        {
            patrolDirection *= -1;
        }
        else if (transform.position.x >= spawnPositionX + patrolRange)
        {
            patrolDirection = -1;
        }
        else if (transform.position.x <= spawnPositionX - patrolRange)
        {
            patrolDirection = 1;
        }

        moveController.setMoveInput(patrolDirection);
    }

    protected void moveToTarget()
    {
        moveController.setMoveInput(getTargetDirection());
    }

    private Unit findNearestTarget()
    {
        Vector2 targetAreaSize = new Vector2(targetHorizontalRange * 2f,targetHeightRange * 2f);
        Collider2D[] targetColliders = Physics2D.OverlapBoxAll(transform.position,targetAreaSize,0f,targetUnitLayer);
        Unit nearestTarget = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider2D targetCollider in targetColliders)
        {
            Unit detectedUnit = targetCollider.GetComponentInParent<Unit>();
            if (!isValidTarget(detectedUnit))
            {
                continue;
            }

            float distance = getTargetDistance(detectedUnit);
            if (distance >= nearestDistance)
            {
                continue;
            }

            nearestTarget = detectedUnit;
            nearestDistance = distance;
        }

        return nearestTarget;
    }

    private bool isValidTarget(Unit detectedUnit)
    {
        if (detectedUnit == null ||
            detectedUnit == this ||
            !detectedUnit.isActiveAndEnabled ||
            !detectedUnit.unitData.canBeHit ||
            detectedUnit.unitStateController.currentActionStateType == UnitActionStateType.Dead)
        {
            return false;
        }

        UnitTeamType detectedTeamType = detectedUnit.unitData.teamType;
        if (targetTeamType == UnitTeamType.Neutral)
        {
            return detectedTeamType == UnitTeamType.Neutral;
        }

        return (targetTeamType & detectedTeamType) != 0;
    }

    private bool isTargetRange(Unit target)
    {
        return getHorizontalDistance(target) <= targetHorizontalRange &&
               getHeightDistance(target) <= targetHeightRange;
    }

    private float getTargetDistance(Unit target)
    {
        return Vector2.Distance(transform.position,target.transform.position);
    }

    private float getHeightDistance(Unit target)
    {
        return Mathf.Abs(target.transform.position.y - transform.position.y);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 targetAreaSize = new Vector2(targetHorizontalRange * 2f,targetHeightRange * 2f);
        Gizmos.DrawWireCube(transform.position,targetAreaSize);
    }

    protected virtual void OnValidate()
    {
        targetHorizontalRange = Mathf.Max(0f,targetHorizontalRange);
        targetHeightRange = Mathf.Max(0f,targetHeightRange);
        targetSearchInterval = Mathf.Max(0.02f,targetSearchInterval);
        attackHeightRange = Mathf.Max(0f,attackHeightRange);
        patrolRange = Mathf.Max(0f,patrolRange);
    }
}
