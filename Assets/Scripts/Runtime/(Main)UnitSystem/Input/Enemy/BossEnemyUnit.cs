using UnityEngine;
using UnitComponents;

[RequireComponent(typeof(UnitWorldHealthBar))]
public sealed class BossEnemyUnit : EnemyUnit
{
    private enum EnemyAIStateType
    {
        Patrol,
        Chase,
        ComboAttack,
        DashAttack,
    }

    #region Inspector

    [Header("Combo Attack")]
    [SerializeField, Min(0f)] private float comboAttackRange = 1.5f;
    [SerializeField, Min(0)] private int comboSkillIndex;

    [Header("Dash Attack")]
    [SerializeField, Min(0f)] private float dashAttackMinRange = 2.5f;
    [SerializeField, Min(0f)] private float dashAttackMaxRange = 4f;
    [SerializeField, Min(0)] private int dashSkillIndex;

    [Header("AI Runtime")]
    [SerializeField] private EnemyAIStateType currentAIStateType;

    #endregion

    private int attackDirection = 1;
    private bool isComboAttackActive;
    private bool isDashAttackActive;
    private bool isDashPending;
    private bool isDashAttackSelected;

    protected override void updateAI()
    {
        if (isControlLocked())
        {
            moveController.setMoveInput(0f);
            resetAttack();
            return;
        }

        if (unitSkillController.currentSkill != null)
        {
            updateSkillAction();
            return;
        }

        isComboAttackActive = false;
        isDashAttackActive = false;
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
            case EnemyAIStateType.ComboAttack:
                executeComboAttack();
                break;
            case EnemyAIStateType.DashAttack:
                executeDashAttack();
                break;
        }
    }

    protected override void resetAI()
    {
        resetAttack();
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
        if (targetDistance <= comboAttackRange)
        {
            currentAIStateType = EnemyAIStateType.ComboAttack;
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

    private void updateSkillAction()
    {
        moveController.setMoveInput(0f);

        if (isDashAttackActive)
        {
            updateDashAttack();
            return;
        }

        if (isComboAttackActive)
        {
            updateComboAttack();
        }
    }

    private void updateDashAttack()
    {
        if (targetUnit == null || !isAttackHeight())
        {
            unitSkillController.cancelSkill();
            isDashAttackActive = false;
            isDashPending = false;
            return;
        }

        if (!isDashPending ||
            !unitStateController.isCurrentActionState(UnitActionStateType.Attack))
        {
            return;
        }

        if (moveController.requestDash())
        {
            isDashPending = false;
        }
    }

    private void updateComboAttack()
    {
        if (!unitStateController.isCurrentActionState(UnitActionStateType.AttackEnd) ||
            targetUnit == null ||
            !isAttackHeight() ||
            getHorizontalDistance(targetUnit) > comboAttackRange)
        {
            return;
        }

        attackDirection = getTargetDirection();
        moveController.setMoveInput(attackDirection);
        unitSkillController.executeComboSkill(comboSkillIndex);
        moveController.setMoveInput(0f);
    }

    private void executeComboAttack()
    {
        attackDirection = getTargetDirection();
        moveController.setMoveInput(attackDirection);
        if (!unitSkillController.executeComboSkill(comboSkillIndex))
        {
            moveController.setMoveInput(0f);
            return;
        }

        moveController.setMoveInput(0f);
        isComboAttackActive = true;
    }

    private void executeDashAttack()
    {
        attackDirection = getTargetDirection();
        moveController.setMoveInput(attackDirection);
        if (!unitSkillController.executeSkill(dashSkillIndex))
        {
            moveController.setMoveInput(0f);
            return;
        }

        moveController.setMoveInput(0f);
        isDashAttackSelected = false;
        isDashAttackActive = true;
        isDashPending = true;
    }

    private void resetAttack()
    {
        isComboAttackActive = false;
        isDashAttackActive = false;
        isDashPending = false;
        isDashAttackSelected = false;
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,comboAttackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position,dashAttackMinRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position,dashAttackMaxRange);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        comboAttackRange = Mathf.Max(0f,comboAttackRange);
        dashAttackMinRange = Mathf.Max(comboAttackRange,dashAttackMinRange);
        dashAttackMaxRange = Mathf.Max(dashAttackMinRange,dashAttackMaxRange);
        comboSkillIndex = Mathf.Max(0,comboSkillIndex);
        dashSkillIndex = Mathf.Max(0,dashSkillIndex);
    }
}
