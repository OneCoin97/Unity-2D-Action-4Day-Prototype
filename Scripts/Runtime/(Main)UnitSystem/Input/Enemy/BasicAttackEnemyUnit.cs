using UnityEngine;

public sealed class BasicAttackEnemyUnit : EnemyUnit
{
    private enum EnemyAIStateType
    {
        Patrol,
        Chase,
        Attack,
    }

    #region Inspector

    [Header("Attack")]
    [SerializeField, Min(0f)] private float attackRange = 1.5f;

    [Header("AI Runtime")]
    [SerializeField] private EnemyAIStateType currentAIStateType;

    #endregion

    protected override void updateAI()
    {
        if (isControlLocked())
        {
            moveController.setMoveInput(0f);
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
        }
    }

    protected override void resetAI()
    {
        currentAIStateType = EnemyAIStateType.Patrol;
    }

    private void updateAIState()
    {
        if (targetUnit == null)
        {
            currentAIStateType = EnemyAIStateType.Patrol;
            return;
        }

        if (!isAttackHeight() || getHorizontalDistance(targetUnit) > attackRange)
        {
            currentAIStateType = EnemyAIStateType.Chase;
            return;
        }

        currentAIStateType = EnemyAIStateType.Attack;
    }

    private void attack()
    {
        moveController.setMoveInput(getTargetDirection());
        if (!unitSkillController.executeSkill(0))
        {
            moveController.setMoveInput(0f);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,attackRange);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        attackRange = Mathf.Max(0f,attackRange);
    }
}
