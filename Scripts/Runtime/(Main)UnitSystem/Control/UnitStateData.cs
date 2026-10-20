[System.Serializable]
public sealed class UnitStateData
{
    public bool isMove;
    public bool isJump;
    public bool isFall;
    public bool isDash;
    public bool isDownDash;
    public bool isAttackStart;
    public bool isAttack;
    public bool isAttackEnd;
    public bool isParry;
    public bool isParryEnd;
    public bool isParrySuccess;
    public bool isSuperArmor;
    public bool isHit;
    public bool isHitEnd;
    public bool isDead;
    public bool isSpawn;
    public bool isGround;
    public bool isWall;
    public int moveDirection = 1;
    public int dashDirection;
    public int wallDirection;

    public int getFacingDirection()
    {
        if (isDash && dashDirection != 0)
        {
            return dashDirection;
        }

        return wallDirection != 0 ? wallDirection : moveDirection;
    }

    public void reset()
    {
        isMove = false;
        isJump = false;
        isFall = false;
        isDash = false;
        isDownDash = false;
        isAttackStart = false;
        isAttack = false;
        isAttackEnd = false;
        isParry = false;
        isParryEnd = false;
        isParrySuccess = false;
        isSuperArmor = false;
        isHit = false;
        isHitEnd = false;
        isDead = false;
        isSpawn = false;
        isGround = false;
        isWall = false;
        moveDirection = 1;
        dashDirection = 0;
        wallDirection = 0;
    }
}
