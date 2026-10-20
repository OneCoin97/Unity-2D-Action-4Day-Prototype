using UnityEngine;

public enum SkillHitType
{
    Damage,
    Heal,
}

public enum SkillHitShapeType
{
    Box,
    Circle,
}

public enum SkillAttackType
{
    Melee,
    Projectile,
}

[System.Serializable]
public sealed class SkillHitData
{
    [Header("Attack")]
    public SkillAttackType attackType;

    [Header("Hit")]
    public SkillHitType hitType;
    [Min(0)] public int value;
    [Min(0f)] public float hitStunDuration = 0.1f;

    [Header("Knockback")]
    public Vector2 knockbackDirection = Vector2.right;
    [Min(0f)] public float knockbackPower;

    [Header("Target")]
    public UnitTeamType targetTeamType;

    [Header("Range")]
    public SkillHitShapeType shapeType;
    public Vector2 size = Vector2.one;
    public Vector2 offset;
    [Min(0f)] public float range = 1f;
    [Min(0.01f)] public float projectileSpeed = 10f;
}
