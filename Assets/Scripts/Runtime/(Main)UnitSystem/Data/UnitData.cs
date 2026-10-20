using System;
using UnityEngine;

[Flags]
public enum UnitTeamType
{
    Neutral = 0,
    TeamA = 1 << 0,
    TeamB = 1 << 1,
}

[Serializable]
public sealed class UnitData
{
    public string unitName;
    [Min(1)] public int maxHp = 100;
    [Min(0)] public int hp = 100;
    public bool canBeHit = true;
    [SingleFlag]
    public UnitTeamType teamType;
}
