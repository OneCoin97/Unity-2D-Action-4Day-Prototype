using System;
using UnityEngine;

[Serializable]
public sealed class UnitStateController
{
    public event Action<UnitMovementStateType,UnitMovementStateType> movementStateChanged;
    public event Action<UnitActionStateType,UnitActionStateType> actionStateChanged;
    public event Action<UnitStatusType,UnitStatusType> statusChanged;

    [field: SerializeField] public UnitStateData stateData { get; private set; } = new UnitStateData();

    // Movement와 Action의 현재 상태 및 변경 이벤트에는 항상 하나의 상태값만 들어간다.
    // Flags 조합은 컴포넌트에서 여러 단일 상태를 비교하는 마스크로만 사용한다.
    [field: SerializeField] public UnitMovementStateType currentMovementStateType { get; private set; } = UnitMovementStateType.None;
    [field: SerializeField] public UnitActionStateType currentActionStateType { get; private set; } = UnitActionStateType.None;

    // Status는 여러 상태가 동시에 적용될 수 있는 실제 복합 플래그값이다.
    [field: SerializeField] public UnitStatusType currentStatusType { get; private set; } = UnitStatusType.None;

    public void updateState()
    {
        updateActionState();
        updateMovementState();
        updateStatus();
    }

    public bool isCurrentMovementState(UnitMovementStateType stateMask)
    {
        if (stateMask == UnitMovementStateType.None)
        {
            return currentMovementStateType == UnitMovementStateType.None;
        }

        return (currentMovementStateType & stateMask) != 0;
    }

    public bool isCurrentActionState(UnitActionStateType stateMask)
    {
        if (stateMask == UnitActionStateType.None)
        {
            return currentActionStateType == UnitActionStateType.None;
        }

        return (currentActionStateType & stateMask) != 0;
    }

    public bool hasStatus(UnitStatusType statusType)
    {
        return (currentStatusType & statusType) != 0;
    }

    public void reset()
    {
        stateData.reset();
        currentMovementStateType = UnitMovementStateType.None;
        currentActionStateType = UnitActionStateType.None;
        currentStatusType = UnitStatusType.None;
    }

    public void clear()
    {
        reset();
        movementStateChanged = null;
        actionStateChanged = null;
        statusChanged = null;
    }

    private void updateStatus()
    {
        UnitStatusType nextStatusType = evaluateStatus();
        if (currentStatusType == nextStatusType)
        {
            return;
        }

        UnitStatusType previousStatusType = currentStatusType;
        currentStatusType = nextStatusType;
        statusChanged?.Invoke(previousStatusType,nextStatusType);
    }

    private void updateMovementState()
    {
        UnitMovementStateType nextStateType = evaluateMovementState();
        if (currentMovementStateType == nextStateType)
        {
            return;
        }

        UnitMovementStateType previousStateType = currentMovementStateType;
        currentMovementStateType = nextStateType;
        movementStateChanged?.Invoke(previousStateType,nextStateType);
    }

    private void updateActionState()
    {
        UnitActionStateType nextStateType = evaluateActionState();
        if (currentActionStateType == nextStateType)
        {
            return;
        }

        UnitActionStateType previousStateType = currentActionStateType;
        currentActionStateType = nextStateType;
        actionStateChanged?.Invoke(previousStateType,nextStateType);
    }

    private UnitMovementStateType evaluateMovementState()
    {
        if (stateData.isDash)
        {
            return UnitMovementStateType.Dash;
        }

        if (stateData.isWall && !stateData.isGround)
        {
            return UnitMovementStateType.Wall;
        }

        if (stateData.isJump)
        {
            return UnitMovementStateType.Jump;
        }

        if (stateData.isFall)
        {
            return UnitMovementStateType.Fall;
        }

        return stateData.isMove ? UnitMovementStateType.Move : UnitMovementStateType.Idle;
    }

    private UnitActionStateType evaluateActionState()
    {
        if (stateData.isDead)
        {
            return UnitActionStateType.Dead;
        }

        if (stateData.isSpawn)
        {
            return UnitActionStateType.Spawn;
        }

        if (stateData.isHit)
        {
            return UnitActionStateType.Hit;
        }

        if (stateData.isHitEnd)
        {
            return UnitActionStateType.HitEnd;
        }

        if (stateData.isParry)
        {
            return UnitActionStateType.Parry;
        }

        if (stateData.isParryEnd)
        {
            return UnitActionStateType.ParryEnd;
        }

        if (stateData.isAttackStart)
        {
            return UnitActionStateType.AttackStart;
        }

        if (stateData.isAttack)
        {
            return UnitActionStateType.Attack;
        }

        return stateData.isAttackEnd
            ? UnitActionStateType.AttackEnd
            : UnitActionStateType.None;
    }

    private UnitStatusType evaluateStatus()
    {
        UnitStatusType statusType = UnitStatusType.None;
        if (stateData.isParrySuccess)
        {
            statusType |= UnitStatusType.ParrySuccess;
        }

        if (stateData.isSuperArmor)
        {
            statusType |= UnitStatusType.SuperArmor;
        }

        return statusType;
    }
}
