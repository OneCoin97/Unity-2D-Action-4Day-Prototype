using UnityEngine;

public sealed class PatrolEnemyUnit : EnemyUnit
{
    private enum EnemyAIStateType
    {
        Patrol,
        Chase,
        Attack,
        DashAttack,
    }

    #region Inspector

    [Header("Attack Range")]
    [SerializeField, Min(0f)] private float normalAttackRange = 1.5f;
    [SerializeField, Min(0f)] private float dashAttackMinRange = 2.5f;
    [SerializeField, Min(0f)] private float dashAttackMaxRange = 4f;

    [Header("AI Runtime")]
    [SerializeField] private EnemyAIStateType currentAIStateType;

    #endregion

    private int attackDirection = 1;
    private bool isDashPending;
    private bool isDashAttackSelected;

    protected override void updateAI()
    {
        if (isControlLocked())
        {
            moveController.setMoveInput(0f);
            isDashPending = false;
            isDashAttackSelected = false;
            return;
        }

        if (unitSkillController.currentSkill != null)
        {
            updateSkillAction();
            return;
        }

        isDashPending = false;
        updateAIState();

        switch (currentAIStateType)
        {
            case EnemyAIStateType.Patrol:
                patrol();
                break;
            case EnemyAIStateType.Chase:
                moveToTarget();
                break;
            case EnemyAIStateType.Attack:
                executeAttack(false);
                break;
            case EnemyAIStateType.DashAttack:
                executeAttack(true);
                break;
        }
    }

    protected override void resetAI()
    {
        isDashPending = false;
        isDashAttackSelected = false;
    }

    private void updateSkillAction()
    {
        if (!isAttackHeight())
        {
            unitSkillController.cancelSkill();
            moveController.setMoveInput(0f);
            isDashPending = false;
            return;
        }

        if (!isDashPending)
        {
            if (shouldChangeToDashAttack())
            {
                isDashPending = true;
            }
            else
            {
                moveController.setMoveInput(attackDirection);
                return;
            }
        }

        moveController.setMoveInput(0f);
        if (!unitStateController.isCurrentActionState(UnitActionStateType.Attack))
        {
            return;
        }

        if (moveController.requestDash())
        {
            isDashPending = false;
        }
    }

    private bool shouldChangeToDashAttack()
    {
        if (!unitStateController.isCurrentActionState(UnitActionStateType.Attack))
        {
            return false;
        }

        float targetDistance = getHorizontalDistance(targetUnit);
        return targetDistance > normalAttackRange && targetDistance <= dashAttackMaxRange;
    }

    private void updateAIState()
    {
        if (targetUnit == null)
        {
            isDashAttackSelected = false;
            currentAIStateType = EnemyAIStateType.Patrol;
            return;
        }

        if (!isAttackHeight())
        {
            isDashAttackSelected = false;
            currentAIStateType = EnemyAIStateType.Chase;
            return;
        }

        if (isDashAttackSelected)
        {
            currentAIStateType = EnemyAIStateType.DashAttack;
            return;
        }

        float targetDistance = getHorizontalDistance(targetUnit);
        if (targetDistance <= normalAttackRange)
        {
            currentAIStateType = EnemyAIStateType.Attack;
            return;
        }

        if (targetDistance >= dashAttackMinRange && targetDistance <= dashAttackMaxRange)
        {
            isDashAttackSelected = true;
            currentAIStateType = EnemyAIStateType.DashAttack;
            return;
        }

        currentAIStateType = EnemyAIStateType.Chase;
    }

    private void executeAttack(bool dashAttack)
    {
        if (!isAttackHeight())
        {
            return;
        }

        attackDirection = getTargetDirection();
        moveController.setMoveInput(attackDirection);

        if (!unitSkillController.executeSkill(0))
        {
            moveController.setMoveInput(0f);
            return;
        }

        isDashAttackSelected = false;
        isDashPending = dashAttack;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,normalAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position,dashAttackMinRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position,dashAttackMaxRange);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        normalAttackRange = Mathf.Max(0f,normalAttackRange);
        dashAttackMinRange = Mathf.Max(normalAttackRange,dashAttackMinRange);
        dashAttackMaxRange = Mathf.Max(dashAttackMinRange,dashAttackMaxRange);
    }
}
