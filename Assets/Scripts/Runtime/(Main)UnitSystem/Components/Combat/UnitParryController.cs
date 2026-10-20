using System.Collections;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    public sealed class UnitParryController : UnitComponent
    {
        #region Inspector

        [Header("Parry")]
        [SerializeField, Min(0f)] private float parryDuration = 0.15f;

        [Header("Failure")]
        [SerializeField, Min(0f)] private float failRecoveryDuration = 0.4f;
        [SerializeField, Min(0f)] private float failCooldown = 0.6f;

        [Header("Success")]
        [SerializeField, Min(0f)] private float successCooldown = 0.15f;

        #endregion

        private UnitStateData stateData;
        private Coroutine parryProcessCoroutine;
        private float cooldownEndTime;
        private bool isParryWindowActive;

        protected override void onInitialize()
        {
            stateData = unit.unitStateController.stateData;
        }

        public bool requestParry()
        {
            if (!canParry())
            {
                return false;
            }

            stateData.isParry = true;
            stateData.isParryEnd = false;
            stateData.isParrySuccess = false;
            isParryWindowActive = true;
            parryProcessCoroutine = StartCoroutine(processParry());
            return true;
        }

        public bool tryParry()
        {
            if (stateData.isParrySuccess)
            {
                return true;
            }

            if (!isParryWindowActive || !stateData.isParry)
            {
                return false;
            }

            stopParryProcess();
            isParryWindowActive = false;
            stateData.isParry = false;
            stateData.isParryEnd = false;
            stateData.isParrySuccess = true;
            cooldownEndTime = Time.time + successCooldown;
            parryProcessCoroutine = StartCoroutine(processParrySuccess());
            return true;
        }

        private IEnumerator processParrySuccess()
        {
            yield return new WaitForSeconds(successCooldown);

            stateData.isParrySuccess = false;
            parryProcessCoroutine = null;
        }

        private IEnumerator processParry()
        {
            yield return new WaitForSeconds(parryDuration);

            isParryWindowActive = false;
            stateData.isParry = false;
            stateData.isParryEnd = true;
            stateData.isParrySuccess = false;
            cooldownEndTime = Time.time + failCooldown;

            yield return new WaitForSeconds(failRecoveryDuration);

            stateData.isParryEnd = false;
            parryProcessCoroutine = null;
        }

        private bool canParry()
        {
            if (Time.time < cooldownEndTime ||
                !unit.unitStateController.isCurrentActionState(UnitActionStateType.None))
            {
                return false;
            }

            return !unit.unitStateController.isCurrentMovementState(UnitMovementStateType.Dash);
        }

        private void stopParryProcess()
        {
            if (parryProcessCoroutine == null)
            {
                return;
            }

            StopCoroutine(parryProcessCoroutine);
            parryProcessCoroutine = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stopParryProcess();
            isParryWindowActive = false;
            cooldownEndTime = 0f;

            stateData.isParry = false;
            stateData.isParryEnd = false;
            stateData.isParrySuccess = false;
        }

        private void OnValidate()
        {
            parryDuration = Mathf.Max(0f,parryDuration);
            failRecoveryDuration = Mathf.Max(0f,failRecoveryDuration);
            failCooldown = Mathf.Max(0f,failCooldown);
            successCooldown = Mathf.Max(0f,successCooldown);
        }
    }
}
