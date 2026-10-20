using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class UnitSkillController
{
    #region Inspector

    [Header("Combo Skill Slots")]
    [SerializeField] private List<ComboSkillList> comboSkillList = new List<ComboSkillList>();

    [Header("Skill Slots")]
    [SerializeField] private List<SkillDataObject> skillList = new List<SkillDataObject>();

    #endregion

    [field: SerializeField] public SkillDataObject currentSkill { get; private set; }

    private readonly Dictionary<SkillDataObject,float> cooldownEndTimes = new Dictionary<SkillDataObject,float>();

    private Unit unit;
    private UnitStateData stateData;
    private Coroutine skillProcessCoroutine;
    private ComboSkillList currentComboSkillList;

    public void initialize(Unit targetUnit)
    {
        unit = targetUnit;
        stateData = targetUnit.unitStateController.stateData;
    }

    public bool executeComboSkill(int comboSkillIndex)
    {
        if (comboSkillIndex < 0 || comboSkillIndex >= comboSkillList.Count)
        {
            return false;
        }

        ComboSkillList targetComboSkillList = comboSkillList[comboSkillIndex];
        if (targetComboSkillList == null ||
            !targetComboSkillList.tryGetCurrentSkill(out SkillDataObject skillDataObject) ||
            !tryExecuteSkill(skillDataObject))
        {
            return false;
        }

        currentComboSkillList = targetComboSkillList;
        targetComboSkillList.moveNext();
        return true;
    }

    public bool executeSkill(int skillIndex)
    {
        if (!tryGetSkill(skillList,skillIndex,out SkillDataObject skillDataObject) ||
            !tryExecuteSkill(skillDataObject))
        {
            return false;
        }

        currentComboSkillList = null;
        return true;
    }
    
    public void finishSkill()
    {
        if (currentSkill == null)
        {
            return;
        }

        currentComboSkillList?.startEndTimer();
        clearCurrentSkill();
    }

    public void cancelSkill()
    {
        if (currentSkill == null)
        {
            return;
        }

        currentComboSkillList?.reset();
        clearCurrentSkill();
    }

    public void reset()
    {
        foreach (ComboSkillList targetComboSkillList in comboSkillList)
        {
            targetComboSkillList?.reset();
        }

        cooldownEndTimes.Clear();
        clearCurrentSkill();
    }

    private bool tryExecuteSkill(SkillDataObject skillDataObject)
    {
        if (isCooldown(skillDataObject) ||
            skillDataObject.withDownDash && stateData.isGround)
        {
            return false;
        }

        if (stateData.isSpawn ||
            stateData.isParry ||
            stateData.isParryEnd ||
            stateData.isHit ||
            stateData.isHitEnd ||
            stateData.isDead)
        {
            return false;
        }

        if (currentSkill != null && !stateData.isAttackEnd)
        {
            return false;
        }

        currentComboSkillList?.startEndTimer();
        startSkill(skillDataObject);
        return true;
    }

    private void startSkill(SkillDataObject skillDataObject)
    {
        stopSkillProcess();
        currentSkill = skillDataObject;
        stateData.isSuperArmor = skillDataObject.effectDelaySuperArmor;
        stateData.isAttackStart = true;
        stateData.isAttack = false;
        stateData.isAttackEnd = false;
        cooldownEndTimes[skillDataObject] = Time.time + skillDataObject.cooldown;
        skillProcessCoroutine = unit.StartCoroutine(processSkill(skillDataObject));
    }

    private IEnumerator processSkill(SkillDataObject skillDataObject)
    {
        yield return new WaitForSeconds(skillDataObject.effectDelay);
        
        stateData.isSuperArmor = skillDataObject.stunDurationSuperArmor;
        stateData.isAttackStart = false;
        stateData.isAttack = true;

        yield return new WaitForSeconds(skillDataObject.stunDuration);
        
        stateData.isSuperArmor = false;
        stateData.isAttack = false;
        stateData.isAttackEnd = true;
        currentComboSkillList?.refreshResetTime();

        float animationTime = skillDataObject.animationClip == null ? 0f : skillDataObject.animationClip.length;
        float remainingAnimationTime = animationTime - skillDataObject.effectDelay - skillDataObject.stunDuration;
        yield return new WaitForSeconds(Mathf.Max(0f,remainingAnimationTime));

        skillProcessCoroutine = null;
        finishSkill();
    }

    private void clearCurrentSkill()
    {
        stopSkillProcess();
        currentSkill = null;
        currentComboSkillList = null;
        stateData.isSuperArmor = false;
        stateData.isAttackStart = false;
        stateData.isAttack = false;
        stateData.isAttackEnd = false;
    }

    private void stopSkillProcess()
    {
        if (skillProcessCoroutine == null)
        {
            return;
        }

        unit.StopCoroutine(skillProcessCoroutine);
        skillProcessCoroutine = null;
    }

    private bool tryGetSkill(List<SkillDataObject> targetSkillList,int skillIndex,out SkillDataObject skillDataObject)
    {
        if (skillIndex < 0 || skillIndex >= targetSkillList.Count)
        {
            skillDataObject = null;
            return false;
        }

        skillDataObject = targetSkillList[skillIndex];
        return skillDataObject != null;
    }
    
    private bool isCooldown(SkillDataObject skillDataObject)
    {
        return cooldownEndTimes.TryGetValue(skillDataObject,out float cooldownEndTime) &&
               Time.time < cooldownEndTime;
    }
}
