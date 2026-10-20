using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StageUnitData
{
    public string unitName;
    [Min(0)] public int count = 1;
}

[CreateAssetMenu(fileName = "StageData",menuName = "Stage/Stage Data")]
public sealed class StageDataObject : ScriptableObject
{
    [Min(0f)] public float stageDuration = 10f;
    public List<StageUnitData> unitList = new List<StageUnitData>();
}
