using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    public sealed class UnitAnimator : UnitComponent
    {
        private const UnitActionStateType hitStateMask =
            UnitActionStateType.Hit |
            UnitActionStateType.HitEnd;

        private const UnitActionStateType attackExecutionStateMask =
            UnitActionStateType.Attack |
            UnitActionStateType.AttackEnd;

        private const int idleStateValue = 1;
        private const int moveStateValue = 2;
        private const int jumpStateValue = 3;
        private const int fallStateValue = 4;
        private const int attackStateValue = 6;
        private const int deadStateValue = 8;
        private const int wallStateValue = 9;
        private const int parryStateValue = 10;

        private static readonly int unitStateHash = Animator.StringToHash("UnitState");

        [Header("Direction")]
        [SerializeField] private bool reverseFlipX;

        [Header("Skill")]
        [SerializeField] private AnimationClip skillPlaceholder;
        [SerializeField] private string skillStateName = "Attack";

        private Animator animator;
        private AnimatorOverrideController overrideController;
        private SpriteRenderer spriteRenderer;
        private UnitStateData stateData;
        protected override void onInitialize()
        {
            animator = GetComponentInChildren<Animator>();
            RuntimeAnimatorController animatorController = animator.runtimeAnimatorController;
            overrideController = new AnimatorOverrideController(animatorController);
            animator.runtimeAnimatorController = overrideController;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            stateData = unit.unitStateController.stateData;
            UnitMovementStateType currentStateType = unit.unitStateController.currentMovementStateType;
            setMovementAnimatorState(
                currentStateType == UnitMovementStateType.None
                    ? UnitMovementStateType.Idle
                    : currentStateType);
        }

        private void LateUpdate()
        {
            if (unit.unitStateController.currentActionStateType != UnitActionStateType.None)
            {
                return;
            }

            applyFlipDirection();
        }

        protected override void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
            if (nextStateType == UnitMovementStateType.Dash)
            {
                applyFlipDirection();
                return;
            }

            if (unit.unitStateController.currentActionStateType != UnitActionStateType.None)
            {
                return;
            }

            setMovementAnimatorState(nextStateType);
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if ((nextStateType & hitStateMask) != 0)
            {
                return;
            }

            if ((nextStateType & attackExecutionStateMask) != 0)
            {
                return;
            }

            if (nextStateType == UnitActionStateType.AttackStart)
            {
                playCurrentSkillAnimation();
                return;
            }

            if (nextStateType == UnitActionStateType.None)
            {
                setMovementAnimatorState(unit.unitStateController.currentMovementStateType);
                return;
            }

            setActionAnimatorState(nextStateType);
        }

        private void playCurrentSkillAnimation()
        {
            SkillDataObject currentSkill = unit.unitSkillController.currentSkill;
            if (currentSkill == null || currentSkill.animationClip == null)
            {
                return;
            }

            applyFlipDirection();

            overrideController[skillPlaceholder.name] = currentSkill.animationClip;
            animator.SetInteger(unitStateHash,attackStateValue);
            animator.Play(skillStateName,0,0f);
        }

        private void setMovementAnimatorState(UnitMovementStateType stateType)
        {
            int stateValue;
            switch (stateType)
            {
                case UnitMovementStateType.Idle:
                    stateValue = idleStateValue;
                    break;
                case UnitMovementStateType.Move:
                    stateValue = moveStateValue;
                    break;
                case UnitMovementStateType.Jump:
                    stateValue = jumpStateValue;
                    break;
                case UnitMovementStateType.Fall:
                    stateValue = fallStateValue;
                    break;
                case UnitMovementStateType.Wall:
                    stateValue = wallStateValue;
                    break;
                default:
                    return;
            }

            animator.SetInteger(unitStateHash,stateValue);
        }

        private void setActionAnimatorState(UnitActionStateType stateType)
        {
            int stateValue;
            switch (stateType)
            {
                case UnitActionStateType.Parry:
                    stateValue = parryStateValue;
                    break;
                case UnitActionStateType.Dead:
                    stateValue = deadStateValue;
                    break;
                default:
                    return;
            }

            animator.SetInteger(unitStateHash,stateValue);
        }

        private void applyFlipDirection()
        {
            int visualDirection = stateData.getFacingDirection();
            bool flipX = visualDirection < 0;
            spriteRenderer.flipX = reverseFlipX ? !flipX : flipX;
        }

    }
}
