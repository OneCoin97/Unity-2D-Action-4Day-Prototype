using UnityEngine;

[CreateAssetMenu(fileName = "SkillData",menuName = "Unit/Skill Data")]
public sealed class SkillDataObject : ScriptableObject
{
    [Header("Timing")]
    [Min(0f)] public float effectDelay;
    [Min(0f)] public float stunDuration;

    [Header("Animation")]
    public AnimationClip animationClip;

    [Header("Control")]
    [Range(0f,1f)] public float moveSpeedMultiplier = 0.6f;
    public bool withDownDash;
    public bool canJump;
    public bool canDash = true;

    [Header("Super Armor")]
    public bool effectDelaySuperArmor;
    public bool stunDurationSuperArmor;

    [Header("Sound")]
    public AudioClip castingSound;
    public AudioClip sound;
    public AudioClip hitSound;

    [Header("Effect")]
    public AnimationClip actionEffectClip;
    public AnimationClip projectileEffectClip;
    public AnimationClip hitEffectClip;

    [Header("Cooldown")]
    [Min(0f)] public float cooldown;

    [Header("Hit Data")]
    public SkillHitData hitData = new SkillHitData();
}
