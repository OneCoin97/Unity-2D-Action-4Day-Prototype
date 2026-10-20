using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    public sealed class UnitCameraShakeController : UnitComponent
    {
        #region Inspector

        [SerializeField] private UnitCameraFollower cameraFollower;

        [Header("Hit Shake")]
        [SerializeField, Min(0f)] private float hitShakeDuration = 0.08f;
        [SerializeField, Min(0f)] private float hitShakePower = 0.05f;

        [Header("Parry Success Shake")]
        [SerializeField, Min(0f)] private float parryShakeDuration = 0.1f;
        [SerializeField, Min(0f)] private float parryShakePower = 0.08f;

        [Header("Down Dash Shake")]
        [SerializeField, Min(0f)] private float downDashShakeDuration = 0.12f;
        [SerializeField, Min(0f)] private float downDashShakePower = 0.06f;

        #endregion

        public void setCameraFollower(UnitCameraFollower targetCameraFollower)
        {
            cameraFollower = targetCameraFollower;
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if (nextStateType == UnitActionStateType.Hit)
            {
                cameraFollower.shake(hitShakeDuration,hitShakePower);
            }
        }

        protected override void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
            if (nextStateType == UnitMovementStateType.Dash &&
                unit.unitStateController.stateData.isDownDash)
            {
                cameraFollower.shake(downDashShakeDuration,downDashShakePower);
            }
        }

        protected override void onStatusChanged(UnitStatusType previousStatusType,UnitStatusType nextStatusType)
        {
            bool wasParrySuccess = (previousStatusType & UnitStatusType.ParrySuccess) != 0;
            bool isParrySuccess = (nextStatusType & UnitStatusType.ParrySuccess) != 0;
            if (!wasParrySuccess && isParrySuccess)
            {
                cameraFollower.shake(parryShakeDuration,parryShakePower);
            }
        }
    }
}
