using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ComboSkillList
{
    [SerializeField] private List<SkillDataObject> skillList = new List<SkillDataObject>();
    [SerializeField, Min(0f)] private float resetTime = 0.8f;
    [SerializeField, Min(0f)] private float endTimer = 0.5f;

    public int currentIndex { get; private set; }

    private float resetEndTime;
    private float comboEndTime;
    private bool isComboEnd;

    public bool tryGetCurrentSkill(out SkillDataObject skillDataObject)
    {
        if (isComboEnd)
        {
            if (comboEndTime <= 0f || Time.time < comboEndTime)
            {
                skillDataObject = null;
                return false;
            }

            comboEndTime = 0f;
            isComboEnd = false;
        }

        if (Time.time > resetEndTime)
        {
            currentIndex = 0;
        }

        if (currentIndex < 0 || currentIndex >= skillList.Count)
        {
            skillDataObject = null;
            return false;
        }

        skillDataObject = skillList[currentIndex];
        return skillDataObject != null;
    }

    public void moveNext()
    {
        isComboEnd = currentIndex == skillList.Count - 1;
        currentIndex = (currentIndex + 1) % skillList.Count;
    }

    public void startEndTimer()
    {
        if (isComboEnd)
        {
            comboEndTime = Time.time + endTimer;
        }
    }

    public void refreshResetTime()
    {
        resetEndTime = Time.time + resetTime;
    }

    public void reset()
    {
        currentIndex = 0;
        resetEndTime = 0f;
        comboEndTime = 0f;
        isComboEnd = false;
    }
}
