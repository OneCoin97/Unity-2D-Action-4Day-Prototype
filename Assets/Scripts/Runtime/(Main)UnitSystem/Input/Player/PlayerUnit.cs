using UnityEngine;
using UnityEngine.InputSystem;
using UnitComponents;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitMoveController))]
[RequireComponent(typeof(UnitJumpController))]
[RequireComponent(typeof(UnitAnimator))]
[RequireComponent(typeof(UnitHitReceiver))]
[RequireComponent(typeof(UnitSkillLauncher))]
[RequireComponent(typeof(UnitParryController))]
[RequireComponent(typeof(UnitAudioController))]
[RequireComponent(typeof(UnitWorldHealthBar))]
public sealed class PlayerUnit : Unit
{
    [Header("Parry Attack")]
    [SerializeField, Min(0)] private int parryAttackSkillIndex = 2;

    private enum BufferedInputType
    {
        None,
        BasicAttack,
        BasicHeavyAttack,
        Skill,
        Dash,
    }

    private UnitMoveController moveController;
    private UnitJumpController jumpController;
    private UnitParryController parryController;
    private PlayerInputActions inputActions;
    private BufferedInputType bufferedInputType;
    private int bufferedInputIndex;

    protected override void Awake()
    {
        base.Awake();

        moveController = getUnitComponent<UnitMoveController>();
        jumpController = getUnitComponent<UnitJumpController>();
        parryController = getUnitComponent<UnitParryController>();
        inputActions = new PlayerInputActions();
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        inputActions.Player.Move.performed += onMoveInput;
        inputActions.Player.Move.canceled += onMoveInput;
        inputActions.Player.Jump.performed += onJumpInput;
        inputActions.Player.Jump.canceled += onJumpReleaseInput;
        inputActions.Player.Attack.performed += onBasicAttackInput;
        inputActions.Player.HeavyAttack.performed += onBasicHeavyAttackInput;
        inputActions.Player.Previous.performed += onSkill1Input;
        inputActions.Player.Next.performed += onSkill2Input;
        inputActions.Player.Dash.performed += onDashInput;
        inputActions.Player.Parry.performed += onParryInput;
        unitStateController.actionStateChanged += onActionStateChanged;
        unitStateController.movementStateChanged += onMovementStateChanged;
        unitStateController.statusChanged += onStatusChanged;
        if (!unitStateController.stateData.isSpawn)
        {
            inputActions.Player.Enable();
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        inputActions.Player.Move.performed -= onMoveInput;
        inputActions.Player.Move.canceled -= onMoveInput;
        inputActions.Player.Jump.performed -= onJumpInput;
        inputActions.Player.Jump.canceled -= onJumpReleaseInput;
        inputActions.Player.Attack.performed -= onBasicAttackInput;
        inputActions.Player.HeavyAttack.performed -= onBasicHeavyAttackInput;
        inputActions.Player.Previous.performed -= onSkill1Input;
        inputActions.Player.Next.performed -= onSkill2Input;
        inputActions.Player.Dash.performed -= onDashInput;
        inputActions.Player.Parry.performed -= onParryInput;
        unitStateController.actionStateChanged -= onActionStateChanged;
        unitStateController.movementStateChanged -= onMovementStateChanged;
        unitStateController.statusChanged -= onStatusChanged;
        inputActions.Player.Disable();

        clearInputBuffer();
        moveController.setMoveInput(0f);
        jumpController.releaseJump();
    }

    protected override void OnDestroy()
    {
        inputActions.Dispose();
        base.OnDestroy();
    }

    private void onMoveInput(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        moveController.setMoveInput(moveInput.x);
    }

    private void onJumpInput(InputAction.CallbackContext context)
    {
        float verticalInput = inputActions.Player.Move.ReadValue<Vector2>().y;
        if (verticalInput < 0f && jumpController.requestDropDown())
        {
            return;
        }

        jumpController.requestJump();
    }

    private void onJumpReleaseInput(InputAction.CallbackContext context)
    {
        jumpController.releaseJump();
    }

    private void onBasicAttackInput(InputAction.CallbackContext context)
    {
        requestInput(BufferedInputType.BasicAttack);
    }

    private void onBasicHeavyAttackInput(InputAction.CallbackContext context)
    {
        requestInput(BufferedInputType.BasicHeavyAttack);
    }

    private void onSkill1Input(InputAction.CallbackContext context)
    {
        requestInput(BufferedInputType.Skill,0);
    }

    private void onSkill2Input(InputAction.CallbackContext context)
    {
        requestInput(BufferedInputType.Skill,1);
    }

    private void onDashInput(InputAction.CallbackContext context)
    {
        float verticalInput = inputActions.Player.Move.ReadValue<Vector2>().y;
        if (unitStateController.isCurrentActionState(UnitActionStateType.AttackStart))
        {
            bufferedInputType = BufferedInputType.Dash;
            bufferedInputIndex = verticalInput < 0f ? -1 : 0;
            return;
        }

        if (verticalInput < 0f && moveController.requestDownDash())
        {
            return;
        }

        moveController.requestDash();
    }

    private void onParryInput(InputAction.CallbackContext context)
    {
        parryController.requestParry();
    }

    private void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
    {
        if (nextStateType == UnitActionStateType.Spawn)
        {
            clearInputBuffer();
            moveController.setMoveInput(0f);
            inputActions.Player.Disable();
            return;
        }

        if (previousStateType == UnitActionStateType.Spawn &&
            nextStateType == UnitActionStateType.None)
        {
            inputActions.Player.Enable();
            return;
        }

        if (nextStateType == UnitActionStateType.Attack &&
            bufferedInputType == BufferedInputType.Dash)
        {
            executeBufferedInput();
            return;
        }

        if (nextStateType == UnitActionStateType.AttackEnd)
        {
            executeBufferedInput();
        }
    }

    private void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
    {
        if (previousStateType == UnitMovementStateType.Dash &&
            nextStateType != UnitMovementStateType.Dash)
        {
            bool canExecuteBufferedInput =
                unitSkillController.currentSkill == null ||
                unitStateController.stateData.isAttackEnd;
            if (!canExecuteBufferedInput)
            {
                return;
            }

            executeBufferedInput();
        }
    }

    private void onStatusChanged(UnitStatusType previousStatusType,UnitStatusType nextStatusType)
    {
        bool wasParrySuccess = (previousStatusType & UnitStatusType.ParrySuccess) != 0;
        bool isParrySuccess = (nextStatusType & UnitStatusType.ParrySuccess) != 0;
        if (wasParrySuccess || !isParrySuccess)
        {
            return;
        }

        clearInputBuffer();
        unitSkillController.executeSkill(parryAttackSkillIndex);
    }

    private void requestInput(BufferedInputType inputType,int inputIndex = 0)
    {
        bool isSkillProcessing =
            unitSkillController.currentSkill != null &&
            !unitStateController.stateData.isAttackEnd;
        bool isDash =
            unitStateController.stateData.isDash ||
            unitStateController.isCurrentMovementState(UnitMovementStateType.Dash);

        if (!isSkillProcessing && !isDash)
        {
            executeInput(inputType,inputIndex);
            return;
        }

        bufferedInputType = inputType;
        bufferedInputIndex = inputIndex;
    }

    private void executeBufferedInput()
    {
        if (bufferedInputType == BufferedInputType.None)
        {
            return;
        }

        BufferedInputType inputType = bufferedInputType;
        int inputIndex = bufferedInputIndex;
        clearInputBuffer();
        executeInput(inputType,inputIndex);
    }

    private void executeInput(BufferedInputType inputType,int inputIndex)
    {
        switch (inputType)
        {
            case BufferedInputType.BasicAttack:
                unitSkillController.executeComboSkill(0);
                break;
            case BufferedInputType.BasicHeavyAttack:
                unitSkillController.executeComboSkill(1);
                break;
            case BufferedInputType.Skill:
                unitSkillController.executeSkill(inputIndex);
                break;
            case BufferedInputType.Dash:
                if (inputIndex < 0 && moveController.requestDownDash())
                {
                    break;
                }

                moveController.requestDash();
                break;
        }
    }

    private void clearInputBuffer()
    {
        bufferedInputType = BufferedInputType.None;
        bufferedInputIndex = 0;
    }

}
