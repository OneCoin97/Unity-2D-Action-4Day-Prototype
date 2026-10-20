using System.Collections.Generic;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnitHitReceiver))]
    public sealed partial class UnitSkillLauncher : UnitComponent
    {
        private const UnitActionStateType attackExecutionStateMask =
            UnitActionStateType.Attack |
            UnitActionStateType.AttackEnd;

        #region Inspector

        [Header("Skill Engine")]
        [SerializeField, Range(0,31)] private int hitBoxLayer;
        [SerializeField] private LayerMask targetLayer = ~0;

        #endregion

        private readonly HashSet<UnitSkillEngine> activeSkillEngines = new HashSet<UnitSkillEngine>();
        private readonly List<UnitSkillEngine> releaseSkillEngines = new List<UnitSkillEngine>();
        private readonly Stack<UnitSkillEngine> skillEnginePool = new Stack<UnitSkillEngine>();

        private UnitSkillEngine currentSkillEngine;
        private int skillEngineCount;

        protected override void OnDisable()
        {
            base.OnDisable();
            releaseAllSkillEngines();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            releaseAllSkillEngines();
            skillEnginePool.Clear();
        }

        public void releaseSkillEngine(UnitSkillEngine skillEngine)
        {
            if (!activeSkillEngines.Contains(skillEngine))
            {
                return;
            }

            if (currentSkillEngine == skillEngine &&
                skillEngine.skillAttackType == SkillAttackType.Melee)
            {
                if (unit.unitStateController.stateData.isDash)
                {
                    return;
                }

                UnitActionStateType actionStateType = unit.unitStateController.currentActionStateType;
                if ((actionStateType & attackExecutionStateMask) != 0)
                {
                    skillEngine.setActive(false);
                    return;
                }
            }

            returnSkillEngine(skillEngine);
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if (nextStateType == UnitActionStateType.AttackStart)
            {
                prepareCurrentSkillEngine();
                return;
            }

            if (nextStateType == UnitActionStateType.Attack)
            {
                executeCurrentSkillEngine();
                return;
            }

            if (nextStateType != UnitActionStateType.AttackEnd &&
                !unit.unitStateController.stateData.isDash)
            {
                releaseCurrentMeleeSkillEngine();
            }
        }

        protected override void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
            if (nextStateType == UnitMovementStateType.Dash)
            {
                executeDashSkillEngine();
                return;
            }

            if (previousStateType != UnitMovementStateType.Dash ||
                currentSkillEngine == null ||
                currentSkillEngine.skillAttackType != SkillAttackType.Melee)
            {
                return;
            }

            returnSkillEngine(currentSkillEngine);
        }

        private void prepareCurrentSkillEngine()
        {
            SkillDataObject currentSkill = unit.unitSkillController.currentSkill;
            if (currentSkill == null || currentSkill.hitData == null)
            {
                return;
            }

            releaseCurrentMeleeSkillEngine();
            currentSkillEngine = prepareSkillEngine(currentSkill);
        }

        private void executeCurrentSkillEngine()
        {
            if (currentSkillEngine == null)
            {
                prepareCurrentSkillEngine();
            }

            if (currentSkillEngine == null)
            {
                return;
            }

            if (currentSkillEngine.skillAttackType == SkillAttackType.Melee)
            {
                currentSkillEngine.executeMelee();
                return;
            }

            currentSkillEngine.executeProjectile();
        }

        private void executeDashSkillEngine()
        {
            UnitActionStateType actionStateType = unit.unitStateController.currentActionStateType;
            if (currentSkillEngine == null ||
                currentSkillEngine.skillAttackType != SkillAttackType.Melee ||
                actionStateType == UnitActionStateType.AttackStart)
            {
                return;
            }

            currentSkillEngine.setActive(true);
        }

        private void releaseCurrentMeleeSkillEngine()
        {
            if (currentSkillEngine == null ||
                currentSkillEngine.skillAttackType != SkillAttackType.Melee)
            {
                return;
            }

            returnSkillEngine(currentSkillEngine);
        }

        private void returnSkillEngine(UnitSkillEngine skillEngine)
        {
            if (!activeSkillEngines.Remove(skillEngine))
            {
                return;
            }

            if (currentSkillEngine == skillEngine)
            {
                currentSkillEngine = null;
            }

            skillEngine.deactivate();
            if (gameObject.activeInHierarchy)
            {
                skillEngine.transform.SetParent(transform);
            }

            skillEnginePool.Push(skillEngine);
        }

        private UnitSkillEngine prepareSkillEngine(SkillDataObject skillDataObject)
        {
            UnitSkillEngine skillEngine = getSkillEngine();
            int direction = unit.unitStateController.stateData.getFacingDirection();

            activeSkillEngines.Add(skillEngine);
            skillEngine.prepare(skillDataObject,direction,targetLayer);
            return skillEngine;
        }

        private UnitSkillEngine getSkillEngine()
        {
            while (skillEnginePool.Count > 0)
            {
                UnitSkillEngine skillEngine = skillEnginePool.Pop();
                if (skillEngine != null)
                {
                    return skillEngine;
                }
            }

            return createSkillEngine();
        }

        private UnitSkillEngine createSkillEngine()
        {
            GameObject skillEngineObject = new GameObject($"SkillEngine_{skillEngineCount++}");
            skillEngineObject.layer = hitBoxLayer;
            skillEngineObject.transform.SetParent(transform);

            Rigidbody2D skillEngineBody = skillEngineObject.AddComponent<Rigidbody2D>();
            skillEngineBody.bodyType = RigidbodyType2D.Kinematic;
            skillEngineBody.gravityScale = 0f;
            skillEngineBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            BoxCollider2D boxCollider = skillEngineObject.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            CircleCollider2D circleCollider = skillEngineObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;

            UnitSkillEngine skillEngine = skillEngineObject.AddComponent<UnitSkillEngine>();
            skillEngine.initialize(this);
            skillEngine.deactivate();
            return skillEngine;
        }

        private void releaseAllSkillEngines()
        {
            currentSkillEngine = null;
            releaseSkillEngines.Clear();
            foreach (UnitSkillEngine skillEngine in activeSkillEngines)
            {
                if (skillEngine == null)
                {
                    continue;
                }

                releaseSkillEngines.Add(skillEngine);
            }

            for (int i = 0; i < releaseSkillEngines.Count; i++)
            {
                returnSkillEngine(releaseSkillEngines[i]);
            }

            releaseSkillEngines.Clear();
        }

        private void OnValidate()
        {
            hitBoxLayer = Mathf.Clamp(hitBoxLayer,0,31);
        }
    }
}
