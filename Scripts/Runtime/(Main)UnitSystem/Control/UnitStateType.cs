[System.Flags]
public enum UnitMovementStateType
{
    None = 0,
    Idle = 1 << 0,
    Move = 1 << 1,
    Jump = 1 << 2,
    Fall = 1 << 3,
    Dash = 1 << 4,
    Wall = 1 << 5,
}

[System.Flags]
public enum UnitActionStateType
{
    None = 0,
    AttackStart = 1 << 0,
    Attack = 1 << 1,
    AttackEnd = 1 << 2,
    Parry = 1 << 3,
    ParryEnd = 1 << 4,
    Hit = 1 << 5,
    HitEnd = 1 << 6,
    Dead = 1 << 7,
    Spawn = 1 << 8,
}

[System.Flags]
public enum UnitStatusType
{
    None = 0,
    ParrySuccess = 1 << 0,
    SuperArmor = 1 << 1,
}
