using System.Collections;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class UnitHitReceiver : UnitComponent
    {
        #region Inspector

        [Header("Floating Message")]
        [SerializeField] private Color damageMessageColor = Color.red;
        [SerializeField] private Color healMessageColor = Color.green;

        #endregion

        private Rigidbody2D body;
        private Coroutine hitStateCoroutine;

        protected override void onInitialize()
        {
            body = GetComponent<Rigidbody2D>();
        }

        public bool applyHit(SkillDataObject skillDataObject,int attackDirection)
        {
            if (!unit.unitData.canBeHit ||
                unit.unitStateController.stateData.isSpawn ||
                unit.unitStateController.stateData.isDead ||
                skillDataObject == null ||
                skillDataObject.hitData == null)
            {
                return false;
            }

            SkillHitData hitData = skillDataObject.hitData;
            
            switch (hitData.hitType)
            {
                case SkillHitType.Damage:
                    return applyDamage(hitData,attackDirection);
                case SkillHitType.Heal:
                    return applyHeal(hitData);
            }

            return false;
        }

        private bool applyHeal(SkillHitData hitData)
        {
            int previousHp = unit.unitData.hp;
            unit.unitData.hp = Mathf.Min(unit.unitData.maxHp,unit.unitData.hp + hitData.value);
            int healValue = unit.unitData.hp - previousHp;
            if (healValue > 0)
            {
                FloatingMessagePool.instance.play(
                    $"+{healValue}",
                    healMessageColor,
                    transform.position);
            }

            return true;
        }

        private bool applyDamage(SkillHitData hitData,int attackDirection)
        {
            if (unit.unitStateController.hasStatus(UnitStatusType.ParrySuccess))
            {
                return false;
            }

            if (unit.tryGetUnitComponent(out UnitParryController parryController) &&
                parryController.tryParry())
            {
                return false;
            }

            bool hasSuperArmor = unit.unitStateController.hasStatus(UnitStatusType.SuperArmor);
            if (!hasSuperArmor)
            {
                unit.unitSkillController.cancelSkill();
            }

            unit.unitData.hp = Mathf.Max(0,unit.unitData.hp - hitData.value);
            FloatingMessagePool.instance.play(
                hitData.value,
                damageMessageColor,
                transform.position);

            if (unit.unitData.hp == 0)
            {
                unit.unitSkillController.cancelSkill();
                stopHitState();
                unit.unitStateController.stateData.isHit = false;
                unit.unitStateController.stateData.isHitEnd = false;
                unit.unitStateController.stateData.isDead = true;
                return true;
            }

            if (hasSuperArmor)
            {
                return true;
            }

            applyKnockback(hitData,attackDirection);
            startHitState(hitData.hitStunDuration);
            return true;
        }

        private void applyKnockback(SkillHitData hitData,int attackDirection)
        {
            if (hitData.knockbackPower <= 0f || hitData.knockbackDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            Vector2 knockbackDirection = hitData.knockbackDirection;
            knockbackDirection.x *= attackDirection;
            body.AddForce(knockbackDirection.normalized * hitData.knockbackPower,ForceMode2D.Impulse);
        }

        private void startHitState(float duration)
        {
            stopHitState();
            unit.unitStateController.stateData.isHitEnd = false;
            unit.unitStateController.stateData.isHit = true;
            hitStateCoroutine = StartCoroutine(processHitState(duration));
        }

        private IEnumerator processHitState(float duration)
        {
            float hitEndTime = Time.time + duration;
            yield return null;

            unit.unitStateController.stateData.isHit = false;
            unit.unitStateController.stateData.isHitEnd = true;

            float remainingTime = hitEndTime - Time.time;
            if (remainingTime > 0f)
            {
                yield return new WaitForSeconds(remainingTime);
            }

            unit.unitStateController.stateData.isHitEnd = false;
            hitStateCoroutine = null;
        }

        private void stopHitState()
        {
            if (hitStateCoroutine == null)
            {
                return;
            }

            StopCoroutine(hitStateCoroutine);
            hitStateCoroutine = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stopHitState();
            if (unit != null)
            {
                unit.unitStateController.stateData.isHit = false;
                unit.unitStateController.stateData.isHitEnd = false;
            }
        }
    }
}
