using System.Collections;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(-60)]
    public sealed class UnitMoveController : UnitComponent
    {
        private const UnitMovementStateType moveStateMask =
            UnitMovementStateType.Idle |
            UnitMovementStateType.Move |
            UnitMovementStateType.Jump |
            UnitMovementStateType.Fall |
            UnitMovementStateType.Wall;

        private const UnitActionStateType controlLockStateMask =
            UnitActionStateType.Spawn |
            UnitActionStateType.Hit |
            UnitActionStateType.HitEnd |
            UnitActionStateType.Dead |
            UnitActionStateType.Parry |
            UnitActionStateType.ParryEnd;

        #region Inspector

        [Header("Horizontal Move")]
        [SerializeField, Min(0f)] private float runSpeed = 8f;
        [SerializeField, Min(0f)] private float acceleration = 70f;
        [SerializeField, Min(0f)] private float deceleration = 90f;
        [SerializeField, Range(0f,1f)] private float groundControl = 1f;
        [SerializeField, Range(0f,1f)] private float airControl = 0.6f;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashSpeed = 14f;
        [SerializeField, Min(0f)] private float dashDuration = 0.2f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.1f;
        [SerializeField, Min(0f)] private float downDashSpeed = 14f;
        [SerializeField, Min(0)] private int maxAirDashCount = 1;

        #endregion

        private Rigidbody2D body;
        private UnitStateData stateData;
        private Coroutine dashProcessCoroutine;
        private float moveInput;
        private float dashTimeRemaining;
        private float dashCooldownEndTime;
        private int airDashCount;
        private bool isSkillDash;
        protected override void onInitialize()
        {
            body = GetComponent<Rigidbody2D>();
            stateData = unit.unitStateController.stateData;
        }

        public void setMoveInput(float input)
        {
            moveInput = Mathf.Clamp(input,-1f,1f);
            if (!Mathf.Approximately(moveInput,0f))
            {
                stateData.moveDirection = moveInput > 0f ? 1 : -1;
            }

            stateData.isMove = !Mathf.Approximately(moveInput,0f);
        }

        public bool requestDash()
        {
            if (stateData.isDash ||
                unit.unitStateController.isCurrentMovementState(UnitMovementStateType.Dash) ||
                !canControlDash())
            {
                return false;
            }

            if (!tryUseAirDash())
            {
                return false;
            }

            dashTimeRemaining = dashDuration;
            stateData.dashDirection = stateData.moveDirection;
            stateData.isDash = true;
            stateData.isDownDash = false;
            isSkillDash = unit.unitSkillController.currentSkill != null;
            return true;
        }

        public bool requestDownDash()
        {
            if (stateData.isDash ||
                unit.unitStateController.isCurrentMovementState(UnitMovementStateType.Dash) ||
                stateData.isGround ||
                !canControlDash())
            {
                return false;
            }

            dashTimeRemaining = 0f;
            stateData.dashDirection = stateData.moveDirection;
            stateData.isDash = true;
            stateData.isDownDash = true;
            isSkillDash = unit.unitSkillController.currentSkill != null;
            return true;
        }

        private void FixedUpdate()
        {
            if (stateData.isGround)
            {
                airDashCount = 0;
            }

            if (!canControlMove())
            {
                return;
            }

            float control = stateData.isGround ? groundControl : airControl;
            float skillMoveSpeedMultiplier = !stateData.isGround || unit.unitSkillController.currentSkill == null
                ? 1f
                : unit.unitSkillController.currentSkill.moveSpeedMultiplier;
            float targetSpeed = moveInput * runSpeed * skillMoveSpeedMultiplier;
            float changeSpeed = Mathf.Approximately(moveInput,0f) ? deceleration : acceleration;
            body.linearVelocityX = Mathf.MoveTowards(
                body.linearVelocityX,
                targetSpeed,
                changeSpeed * control * Time.fixedDeltaTime);
        }

        private IEnumerator processDash()
        {
            if (!stateData.isDownDash)
            {
                body.linearVelocityY = 0f;
            }

            while (stateData.isDash)
            {
                if (stateData.isDownDash)
                {
                    if (stateData.isGround)
                    {
                        break;
                    }

                    body.linearVelocity = Vector2.down * downDashSpeed;
                }
                else
                {
                    body.linearVelocityX = stateData.dashDirection * dashSpeed;
                    dashTimeRemaining -= Time.fixedDeltaTime;
                    if (dashTimeRemaining <= 0f)
                    {
                        break;
                    }
                }

                yield return new WaitForFixedUpdate();
            }

            dashProcessCoroutine = null;
            if (stateData.isDash)
            {
                finishDash();
            }
        }

        private void finishDash()
        {
            dashTimeRemaining = 0f;
            if (!isSkillDash)
            {
                dashCooldownEndTime = Time.time + dashCooldown;
            }

            stateData.isDash = false;
            stateData.isDownDash = false;
            stateData.dashDirection = 0;
            isSkillDash = false;
        }

        private void cancelDash()
        {
            if (!stateData.isDash)
            {
                return;
            }

            stopDashProcess();
            body.linearVelocity = Vector2.zero;
            finishDash();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stopDashProcess();
            moveInput = 0f;
            dashTimeRemaining = 0f;
            dashCooldownEndTime = 0f;
            airDashCount = 0;
            isSkillDash = false;

            stateData.isMove = false;
            stateData.isDash = false;
            stateData.isDownDash = false;
            stateData.dashDirection = 0;
        }

        protected override void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
            if (nextStateType == UnitMovementStateType.Dash)
            {
                stopDashProcess();
                dashProcessCoroutine = StartCoroutine(processDash());
                return;
            }

            if (previousStateType == UnitMovementStateType.Dash)
            {
                stopDashProcess();
            }
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if (nextStateType == UnitActionStateType.Spawn)
            {
                body.linearVelocity = Vector2.zero;
                return;
            }

            if (nextStateType == UnitActionStateType.Hit)
            {
                cancelDash();
                return;
            }

            if (nextStateType == UnitActionStateType.Dead)
            {
                cancelDash();
                return;
            }

            SkillDataObject currentSkill = unit.unitSkillController.currentSkill;
            if (nextStateType == UnitActionStateType.Attack &&
                currentSkill != null &&
                currentSkill.withDownDash)
            {
                requestDownDash();
            }
        }

        private void stopDashProcess()
        {
            if (dashProcessCoroutine == null)
            {
                return;
            }

            StopCoroutine(dashProcessCoroutine);
            dashProcessCoroutine = null;
        }

        private bool canControlMove()
        {
            if (unit.unitStateController.isCurrentActionState(controlLockStateMask))
            {
                return false;
            }

            return unit.unitStateController.isCurrentMovementState(moveStateMask);
        }

        private bool canControlDash()
        {
            if (unit.unitSkillController.currentSkill == null &&
                Time.time < dashCooldownEndTime)
            {
                return false;
            }

            return canControlDashState();
        }

        private bool canControlDashState()
        {
            if (unit.unitStateController.isCurrentActionState(controlLockStateMask))
            {
                return false;
            }

            return unit.unitSkillController.currentSkill == null ||
                   unit.unitSkillController.currentSkill.canDash;
        }

        private bool tryUseAirDash()
        {
            if (stateData.isGround)
            {
                return true;
            }

            if (airDashCount >= maxAirDashCount)
            {
                return false;
            }

            airDashCount++;
            return true;
        }

        private void OnValidate()
        {
            runSpeed = Mathf.Max(0f,runSpeed);
            acceleration = Mathf.Max(0f,acceleration);
            deceleration = Mathf.Max(0f,deceleration);
            dashSpeed = Mathf.Max(0f,dashSpeed);
            dashDuration = Mathf.Max(0f,dashDuration);
            dashCooldown = Mathf.Max(0f,dashCooldown);
            downDashSpeed = Mathf.Max(0f,downDashSpeed);
            maxAirDashCount = Mathf.Max(0,maxAirDashCount);
        }
    }
}
