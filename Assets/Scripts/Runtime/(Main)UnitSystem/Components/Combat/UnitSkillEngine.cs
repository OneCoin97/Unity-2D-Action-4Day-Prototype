using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class UnitSkillEngine : MonoBehaviour
    {
        private readonly HashSet<Unit> hitUnits = new HashSet<Unit>();

        private UnitSkillLauncher skillLauncher;
        private Rigidbody2D body;
        private BoxCollider2D boxCollider;
        private CircleCollider2D circleCollider;
        private EffectPool effectPool;
        private EffectPlayer effectPlayer;
        private SkillDataObject skillDataObject;
        private LayerMask targetLayer;
        private float range;
        private float projectileSpeed;
        private float movedDistance;
        private int direction;
        private Coroutine processCoroutine;

        public SkillAttackType skillAttackType { get; private set; }

        public void initialize(UnitSkillLauncher targetSkillLauncher)
        {
            skillLauncher = targetSkillLauncher;
            body = GetComponent<Rigidbody2D>();
            boxCollider = GetComponent<BoxCollider2D>();
            circleCollider = GetComponent<CircleCollider2D>();
            effectPool = EffectPool.instance;
        }

        public void prepare(SkillDataObject targetSkillDataObject,int targetDirection,LayerMask targetLayerMask)
        {
            skillDataObject = targetSkillDataObject;
            targetLayer = targetLayerMask;
            skillAttackType = skillDataObject.hitData.attackType;
            range = skillDataObject.hitData.range;
            projectileSpeed = skillDataObject.hitData.projectileSpeed;
            movedDistance = 0f;
            direction = targetDirection;
            hitUnits.Clear();
            stopProcess();

            if (skillAttackType == SkillAttackType.Melee)
            {
                transform.SetParent(skillLauncher.transform,false);
                transform.localPosition = Vector2.right * direction * range;
            }
            else
            {
                transform.SetParent(null);
                transform.position = skillLauncher.transform.position;
            }

            body.position = transform.position;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            setCollider(skillDataObject.hitData);
            if (skillDataObject.castingSound != null)
            {
                AudioSource.PlayClipAtPoint(skillDataObject.castingSound,skillLauncher.transform.position);
            }
        }

        public void executeMelee()
        {
            startExecution();
            processCoroutine = StartCoroutine(processMelee());
        }

        public void executeProjectile()
        {
            startExecution();
            processCoroutine = StartCoroutine(processProjectile());
        }

        private void startExecution()
        {
            gameObject.SetActive(true);
            stopProcess();

            AnimationClip effectClip = skillAttackType == SkillAttackType.Projectile
                ? skillDataObject.projectileEffectClip
                : skillDataObject.actionEffectClip;
            playSkillEffect(effectClip);

            if (skillDataObject.sound != null)
            {
                AudioSource.PlayClipAtPoint(skillDataObject.sound,transform.position);
            }
        }

        public void setActive(bool value)
        {
            gameObject.SetActive(value);
        }

        public void deactivate()
        {
            stopProcess();
            releaseSkillEffect();
            gameObject.SetActive(false);
            skillDataObject = null;
            hitUnits.Clear();
        }

        private IEnumerator processMelee()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForEndOfFrame();
            processCoroutine = null;
            skillLauncher.releaseSkillEngine(this);
        }

        private IEnumerator processProjectile()
        {
            while (projectileSpeed > 0f && movedDistance < range)
            {
                float moveDistance = Mathf.Min(
                    projectileSpeed * Time.fixedDeltaTime,
                    range - movedDistance);
                body.MovePosition(body.position + Vector2.right * direction * moveDistance);
                movedDistance += moveDistance;
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForEndOfFrame();
            processCoroutine = null;
            playHitEffect(getEffectPosition());
            skillLauncher.releaseSkillEngine(this);
        }

        private void stopProcess()
        {
            if (processCoroutine == null)
            {
                return;
            }

            StopCoroutine(processCoroutine);
            processCoroutine = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!gameObject.activeInHierarchy || skillDataObject == null)
            {
                return;
            }

            if ((targetLayer.value & 1 << other.gameObject.layer) == 0)
            {
                return;
            }

            Unit targetUnit = other.GetComponent<Unit>();
            if (targetUnit == null ||
                !isTargetTeam(targetUnit.unitData.teamType))
            {
                return;
            }

            if (!targetUnit.tryGetUnitComponent(out UnitHitReceiver hitReceiver) ||
                !hitUnits.Add(targetUnit))
            {
                return;
            }

            Vector2 hitPosition = other.ClosestPoint(transform.position);
            bool isHitApplied = hitReceiver.applyHit(skillDataObject,direction);
            if (isHitApplied)
            {
                if (skillDataObject.hitSound != null)
                {
                    AudioSource.PlayClipAtPoint(skillDataObject.hitSound,hitPosition);
                }

                playHitEffect(hitPosition);
            }

            if (skillAttackType == SkillAttackType.Projectile)
            {
                skillLauncher.releaseSkillEngine(this);
            }

        }

        private void setCollider(SkillHitData hitData)
        {
            bool isBox = hitData.shapeType == SkillHitShapeType.Box;
            Vector2 hitBoxOffset = hitData.offset;
            hitBoxOffset.x *= direction;
            boxCollider.enabled = isBox;
            circleCollider.enabled = !isBox;
            boxCollider.offset = hitBoxOffset;
            circleCollider.offset = hitBoxOffset;

            if (isBox)
            {
                boxCollider.size = new Vector2(
                    Mathf.Max(0.01f,Mathf.Abs(hitData.size.x)),
                    Mathf.Max(0.01f,Mathf.Abs(hitData.size.y)));
                return;
            }

            circleCollider.radius = Mathf.Max(
                0.01f,
                Mathf.Max(Mathf.Abs(hitData.size.x),Mathf.Abs(hitData.size.y)) * 0.5f);
        }

        private bool isTargetTeam(UnitTeamType teamType)
        {
            UnitTeamType targetTeamType = skillDataObject.hitData.targetTeamType;
            if (targetTeamType == UnitTeamType.Neutral)
            {
                return teamType == UnitTeamType.Neutral;
            }

            return (targetTeamType & teamType) != 0;
        }

        private void playHitEffect(Vector2 hitPosition)
        {
            effectPool.playEffect(
                skillDataObject.hitEffectClip,
                hitPosition,
                direction,
                gameObject.layer);
        }

        private void playSkillEffect(AnimationClip effectClip)
        {
            releaseSkillEffect();
            if (effectClip == null)
            {
                return;
            }

            effectPlayer = effectPool.getEffectPlayer(transform,gameObject.layer);
            effectPlayer.setOffset(skillDataObject.hitData.offset,direction);
            effectPlayer.play(effectClip,direction,false);
        }

        private void releaseSkillEffect()
        {
            if (effectPlayer == null)
            {
                return;
            }

            effectPool.releaseEffectPlayer(effectPlayer);
            effectPlayer = null;
        }

        private Vector2 getEffectPosition()
        {
            Vector2 effectOffset = skillDataObject.hitData.offset;
            effectOffset.x *= direction;
            return transform.TransformPoint(effectOffset);
        }

    }
}
