using UnityEngine;

public sealed class WizardEnemyUnit : EnemyUnit
{
    private enum EnemyAIStateType
    {
        Patrol,
        Chase,
        Attack,
        Retreat,
    }

    #region Inspector

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackRange = 6f;
    [SerializeField, Min(0f)] private float retreatDashRange = 2f;
    [SerializeField, Min(0f)] private float minimumRetreatDashDistance = 0.5f;

    [Header("AI Runtime")]
    [SerializeField] private EnemyAIStateType currentAIStateType;

    #endregion

    private float retreatDashStartPositionX;
    private int retreatDashDirection;
    private bool isRetreatDashPending;
    private bool isReverseRetreatDashPending;
    private bool hasRetriedRetreatDash;

    protected override void updateAI()
    {
        if (isControlLocked())
        {
            moveController.setMoveInput(0f);
            isRetreatDashPending = false;
            isReverseRetreatDashPending = false;
            hasRetriedRetreatDash = false;
            return;
        }

        if (isRetreatDashPending)
        {
            processRetreatDash();
            return;
        }

        if (unitSkillController.currentSkill != null)
        {
            moveController.setMoveInput(0f);
            return;
        }

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
                attack();
                break;
            case EnemyAIStateType.Retreat:
                tryStartRetreatDash(-getTargetDirection(),false);
                break;
        }
    }

    protected override void resetAI()
    {
        currentAIStateType = EnemyAIStateType.Patrol;
        isRetreatDashPending = false;
        isReverseRetreatDashPending = false;
        hasRetriedRetreatDash = false;
    }

    private void updateAIState()
    {
        if (targetUnit == null)
        {
            currentAIStateType = EnemyAIStateType.Patrol;
            return;
        }

        if (!isAttackHeight())
        {
            currentAIStateType = EnemyAIStateType.Chase;
            return;
        }

        float targetDistance = getHorizontalDistance(targetUnit);
        if (targetDistance <= retreatDashRange)
        {
            currentAIStateType = EnemyAIStateType.Retreat;
            return;
        }

        currentAIStateType = targetDistance <= attackRange
            ? EnemyAIStateType.Attack
            : EnemyAIStateType.Chase;
    }

    private void attack()
    {
        moveController.setMoveInput(getTargetDirection());
        if (!unitSkillController.executeSkill(0))
        {
            moveController.setMoveInput(0f);
        }
    }

    private void processRetreatDash()
    {
        if (isReverseRetreatDashPending)
        {
            tryStartRetreatDash(retreatDashDirection,true);
            return;
        }

        if (unitStateController.stateData.isDash)
        {
            return;
        }

        float movedDistance = Mathf.Abs(transform.position.x - retreatDashStartPositionX);
        if (!hasRetriedRetreatDash && movedDistance < minimumRetreatDashDistance)
        {
            retreatDashDirection *= -1;
            isReverseRetreatDashPending = true;
            moveController.setMoveInput(0f);
            return;
        }

        isRetreatDashPending = false;
        hasRetriedRetreatDash = false;
        if (targetUnit == null || !isAttackHeight())
        {
            return;
        }

        moveController.setMoveInput(getTargetDirection());
        if (!unitSkillController.executeSkill(0))
        {
            moveController.setMoveInput(0f);
        }
    }

    private bool tryStartRetreatDash(int direction,bool retried)
    {
        moveController.setMoveInput(direction);
        if (!moveController.requestDash())
        {
            moveController.setMoveInput(0f);
            return false;
        }

        retreatDashStartPositionX = transform.position.x;
        retreatDashDirection = direction;
        isRetreatDashPending = true;
        isReverseRetreatDashPending = false;
        hasRetriedRetreatDash = retried;
        moveController.setMoveInput(0f);
        return true;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position,retreatDashRange);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        attackRange = Mathf.Max(0f,attackRange);
        retreatDashRange = Mathf.Clamp(retreatDashRange,0f,attackRange);
        minimumRetreatDashDistance = Mathf.Max(0f,minimumRetreatDashDistance);
    }
}
