using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(UnitSurfaceChecker))]
    [DefaultExecutionOrder(-50)]
    public sealed class UnitJumpController : UnitComponent
    {
        private const UnitMovementStateType jumpStateMask =
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

        [Header("Jump Power")]
        [FormerlySerializedAs("groundJumpSpeed")]
        [SerializeField, Min(0f)] private float groundJumpPower = 14f;
        [FormerlySerializedAs("airJumpSpeed")]
        [SerializeField, Min(0f)] private float airJumpPower = 13f;

        [Header("Variable Height")]
        [SerializeField] private bool useVariableJumpHeight = true;
        [SerializeField, Range(0f,1f)] private float jumpCutMultiplier = 0.5f;

        [Header("Air Jump")]
        [SerializeField, Min(0)] private int maxAirJumps = 1;

        [Header("Input Assist")]
        [SerializeField, Min(0f)] private float coyoteTime = 0.1f;

        [Header("Gravity")]
        [SerializeField, Min(1f)] private float fallGravityMultiplier = 2f;

        [Header("Wall")]
        [SerializeField, Min(0f)] private float wallSlideSpeed = 2f;
        [SerializeField, Min(0f)] private float wallJumpPower = 14f;
        [SerializeField, Range(0f,90f)] private float wallJumpAngle = 70f;

        [Header("Air Attack Stall")]
        [SerializeField, Min(0f)] private float airAttackStallDuration = 0.08f;
        [SerializeField, Range(0f,1f)] private float airAttackGravityMultiplier = 0.1f;
        [SerializeField, Min(0f)] private float airAttackLiftPower = 1.5f;

        [Header("Drop Down")]
        [SerializeField, Min(0f)] private float dropThroughDuration = 2f;
        [SerializeField, Min(0f)] private float dropDownLiftPower = 2f;

        #endregion

        private Rigidbody2D body;
        private Collider2D unitCollider;
        private Collider2D dropThroughCollider;
        private UnitSurfaceChecker surfaceChecker;
        private UnitStateData stateData;
        private float defaultGravityScale;
        private float coyoteTimeRemaining;
        private float airStallTimeRemaining;
        private float airStallGravityMultiplier;
        private int remainingAirJumpCount;
        private bool isJumpHeld;
        private bool canApplyJumpCut;
        private Coroutine dropThroughRestoreCoroutine;
        
        protected override void onInitialize()
        {
            body = GetComponent<Rigidbody2D>();
            unitCollider = GetComponent<Collider2D>();
            surfaceChecker = unit.getUnitComponent<UnitSurfaceChecker>();
            stateData = unit.unitStateController.stateData;
            defaultGravityScale = body.gravityScale;
            remainingAirJumpCount = maxAirJumps;
        }

        public void requestJump()
        {
            isJumpHeld = true;

            if (!canControlJump())
            {
                return;
            }

            if (canUseWallJump())
            {
                executeWallJump();
            }
            else if (canUseGroundJump())
            {
                executeGroundJump();
            }
            else if (remainingAirJumpCount > 0)
            {
                executeAirJump();
            }
            else
            {
                return;
            }

            clearAirStall();
            canApplyJumpCut = useVariableJumpHeight;
        }

        public void releaseJump()
        {
            isJumpHeld = false;
        }

        public bool requestDropDown()
        {
            if (!stateData.isGround || surfaceChecker.groundCollider == null)
            {
                return false;
            }

            Collider2D targetCollider = surfaceChecker.groundCollider;
            if (targetCollider.GetComponent<OneWayPlatform>() == null)
            {
                return false;
            }

            restoreDropThrough();
            dropThroughCollider = targetCollider;
            Physics2D.IgnoreCollision(unitCollider,dropThroughCollider,true);
            dropThroughRestoreCoroutine = StartCoroutine(restoreDropThroughAfterDelay());
            clearAirStall();
            canApplyJumpCut = false;
            stateData.isGround = false;
            body.linearVelocityY = 0f;
            body.AddForce(
                Vector2.up * dropDownLiftPower,
                ForceMode2D.Impulse);
            return true;
        }

        private void FixedUpdate()
        {
            if (stateData.isDash)
            {
                if (!stateData.isDownDash)
                {
                    body.linearVelocityY = 0f;
                }

                body.gravityScale = 0f;
                return;
            }

            updateJumpAvailability();
            applyWallSlide();
            applyJumpCut();
            applyGravity();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            isJumpHeld = false;
            canApplyJumpCut = false;
            clearAirStall();
            restoreDropThrough();

            if (body != null)
            {
                body.gravityScale = defaultGravityScale;
            }
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if (nextStateType != UnitActionStateType.AttackStart || stateData.isGround)
            {
                return;
            }

            startAirAttackStall();
        }

        private void startAirAttackStall()
        {
            if (airAttackStallDuration <= 0f)
            {
                return;
            }

            if (body.linearVelocityY < 0f)
            {
                body.linearVelocityY = 0f;
            }

            body.AddForce(
                Vector2.up * airAttackLiftPower,
                ForceMode2D.Impulse);
            airStallTimeRemaining = airAttackStallDuration;
            airStallGravityMultiplier = airAttackGravityMultiplier;
        }

        private void updateJumpAvailability()
        {
            if (isStableGround())
            {
                clearAirStall();
                restoreDropThrough();
                coyoteTimeRemaining = coyoteTime;
                remainingAirJumpCount = maxAirJumps;
                return;
            }

            coyoteTimeRemaining = Mathf.Max(0f,coyoteTimeRemaining - Time.fixedDeltaTime);
        }

        private void applyWallSlide()
        {
            if (stateData.isGround || !stateData.isWall || body.linearVelocityY >= -wallSlideSpeed)
            {
                return;
            }

            body.linearVelocityY = -wallSlideSpeed;
        }

        private void executeWallJump()
        {
            int wallJumpDirection = -stateData.wallDirection;
            float wallJumpRadian = wallJumpAngle * Mathf.Deg2Rad;
            Vector2 wallJumpVector = new Vector2(
                Mathf.Cos(wallJumpRadian) * wallJumpDirection,
                Mathf.Sin(wallJumpRadian));

            body.linearVelocity = Vector2.zero;
            body.AddForce(
                wallJumpVector * wallJumpPower,
                ForceMode2D.Impulse);

            coyoteTimeRemaining = 0f;
        }

        private void executeGroundJump()
        {
            body.linearVelocityY = 0f;
            body.AddForce(
                Vector2.up * groundJumpPower,
                ForceMode2D.Impulse);

            coyoteTimeRemaining = 0f;
        }

        private void executeAirJump()
        {
            body.linearVelocityY = 0f;
            body.AddForce(
                Vector2.up * airJumpPower,
                ForceMode2D.Impulse);
            remainingAirJumpCount--;
        }

        private void applyJumpCut()
        {
            if (!canApplyJumpCut || isJumpHeld)
            {
                return;
            }

            canApplyJumpCut = false;
            if (body.linearVelocityY > 0f)
            {
                body.linearVelocityY *= jumpCutMultiplier;
            }
        }

        private void applyGravity()
        {
            if (airStallTimeRemaining > 0f)
            {
                body.gravityScale = defaultGravityScale * airStallGravityMultiplier;
                airStallTimeRemaining = Mathf.Max(
                    0f,
                    airStallTimeRemaining - Time.fixedDeltaTime);
                return;
            }

            float gravityMultiplier = body.linearVelocityY < 0f
                ? fallGravityMultiplier
                : 1f;
            body.gravityScale = defaultGravityScale * gravityMultiplier;
        }

        private void clearAirStall()
        {
            airStallTimeRemaining = 0f;
            airStallGravityMultiplier = 0f;
        }

        private IEnumerator restoreDropThroughAfterDelay()
        {
            yield return new WaitForSeconds(dropThroughDuration);

            dropThroughRestoreCoroutine = null;
            restoreDropThrough();
        }

        private void restoreDropThrough()
        {
            if (dropThroughRestoreCoroutine != null)
            {
                StopCoroutine(dropThroughRestoreCoroutine);
                dropThroughRestoreCoroutine = null;
            }

            if (dropThroughCollider != null)
            {
                Physics2D.IgnoreCollision(unitCollider,dropThroughCollider,false);
                dropThroughCollider = null;
            }
        }

        private bool canUseGroundJump()
        {
            return isStableGround() || coyoteTimeRemaining > 0f;
        }

        private bool canUseWallJump()
        {
            return stateData.isWall && !stateData.isGround;
        }

        private bool isStableGround()
        {
            return stateData.isGround && body.linearVelocityY <= 0f;
        }

        private bool canControlJump()
        {
            if (unit.unitSkillController.currentSkill != null &&
                !unit.unitSkillController.currentSkill.canJump)
            {
                return false;
            }

            if (unit.unitStateController.isCurrentActionState(controlLockStateMask))
            {
                return false;
            }

            return unit.unitStateController.isCurrentMovementState(jumpStateMask);
        }

        private void OnValidate()
        {
            wallSlideSpeed = Mathf.Max(0f,wallSlideSpeed);
            wallJumpPower = Mathf.Max(0f,wallJumpPower);
            wallJumpAngle = Mathf.Clamp(wallJumpAngle,0f,90f);
            airAttackStallDuration = Mathf.Max(0f,airAttackStallDuration);
            airAttackGravityMultiplier = Mathf.Clamp01(airAttackGravityMultiplier);
            airAttackLiftPower = Mathf.Max(0f,airAttackLiftPower);
            dropThroughDuration = Mathf.Max(0f,dropThroughDuration);
            dropDownLiftPower = Mathf.Max(0f,dropDownLiftPower);
            groundJumpPower = Mathf.Max(0f,groundJumpPower);
            airJumpPower = Mathf.Max(0f,airJumpPower);
            jumpCutMultiplier = Mathf.Clamp01(jumpCutMultiplier);
            maxAirJumps = Mathf.Max(0,maxAirJumps);
            coyoteTime = Mathf.Max(0f,coyoteTime);
            fallGravityMultiplier = Mathf.Max(1f,fallGravityMultiplier);
        }
    }
}
