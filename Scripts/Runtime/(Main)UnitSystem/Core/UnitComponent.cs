using UnityEngine;

namespace UnitComponents
{
    public abstract class UnitComponent : MonoBehaviour
    {
        protected Unit unit { get; private set; }

        private bool isStateSubscribed;

        public void initialize(Unit targetUnit)
        {
            unit = targetUnit;
            subscribeStateChanged();
            onInitialize();
        }

        protected virtual void onInitialize()
        {
        }

        protected virtual void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
        }

        protected virtual void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
        }

        protected virtual void onStatusChanged(UnitStatusType previousStatusType,UnitStatusType nextStatusType)
        {
        }

        protected virtual void OnEnable()
        {
            subscribeStateChanged();
        }

        protected virtual void OnDisable()
        {
            unsubscribeStateChanged();
        }

        protected virtual void OnDestroy()
        {
            unsubscribeStateChanged();
        }

        private void subscribeStateChanged()
        {
            if (isStateSubscribed || unit == null || !isActiveAndEnabled)
            {
                return;
            }

            unit.unitStateController.movementStateChanged += onMovementStateChanged;
            unit.unitStateController.actionStateChanged += onActionStateChanged;
            unit.unitStateController.statusChanged += onStatusChanged;
            isStateSubscribed = true;
        }

        private void unsubscribeStateChanged()
        {
            if (!isStateSubscribed || unit == null)
            {
                return;
            }

            unit.unitStateController.movementStateChanged -= onMovementStateChanged;
            unit.unitStateController.actionStateChanged -= onActionStateChanged;
            unit.unitStateController.statusChanged -= onStatusChanged;
            isStateSubscribed = false;
        }
    }
}
