using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [DefaultExecutionOrder(-100)]
    public sealed class UnitSurfaceChecker : UnitComponent
    {
        #region Inspector

        [Header("Check Point")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private Transform wallCheckPoint;

        [Header("Check Area")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f,0.1f);
        [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f,0.8f);
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer;

        #endregion

        public Collider2D groundCollider { get; private set; }

        private Rigidbody2D body;
        private Collider2D unitCollider;
        private UnitStateData stateData;

        protected override void onInitialize()
        {
            body = GetComponent<Rigidbody2D>();
            unitCollider = GetComponent<Collider2D>();
            stateData = unit.unitStateController.stateData;

            if (groundCheckPoint != null && wallCheckPoint != null)
            {
                return;
            }

            Debug.LogError($"{name}에 Ground 또는 Wall Check Point가 등록되어 있지 않습니다.",this);
            enabled = false;
        }

        private void FixedUpdate()
        {
            checkCollision();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stateData.isGround = false;
            stateData.isWall = false;
            stateData.isJump = false;
            stateData.isFall = false;
            stateData.wallDirection = 0;
            groundCollider = null;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;

            if (groundCheckPoint != null)
            {
                Gizmos.DrawWireCube(groundCheckPoint.position,groundCheckSize);
            }

            if (wallCheckPoint != null)
            {
                Gizmos.DrawWireCube(getWallCheckPosition(-1),wallCheckSize);
                Gizmos.DrawWireCube(getWallCheckPosition(1),wallCheckSize);
            }
        }

        private void checkCollision()
        {
            groundCollider = Physics2D.OverlapBox(
                groundCheckPoint.position,
                groundCheckSize,
                0f,
                groundLayer);
            if (!isValidGround(groundCollider))
            {
                groundCollider = null;
            }

            bool isGround = groundCollider != null;

            Collider2D leftWallCollider = Physics2D.OverlapBox(
                getWallCheckPosition(-1),
                wallCheckSize,
                0f,
                wallLayer);
            Collider2D rightWallCollider = Physics2D.OverlapBox(
                getWallCheckPosition(1),
                wallCheckSize,
                0f,
                wallLayer);

            bool isLeftWall = isValidWall(leftWallCollider);
            bool isRightWall = isValidWall(rightWallCollider);

            int wallDirection = isRightWall ? 1 : isLeftWall ? -1 : 0;
            bool isWall = wallDirection != 0;

            bool isAir = !isGround && !isWall;
            bool isJump = isAir && body.linearVelocityY > 0f;
            bool isFall = isAir && !isJump;

            stateData.isGround = isGround;
            stateData.isWall = isWall;
            stateData.isJump = isJump;
            stateData.isFall = isFall;
            stateData.wallDirection = wallDirection;
        }

        private bool isValidGround(Collider2D targetCollider)
        {
            if (targetCollider == null || targetCollider.transform.IsChildOf(transform))
            {
                return false;
            }

            if (Physics2D.GetIgnoreCollision(unitCollider,targetCollider))
            {
                return false;
            }

            if (targetCollider.GetComponent<OneWayPlatform>() == null)
            {
                return true;
            }

            return body.linearVelocityY <= 0f &&
                   unitCollider.bounds.min.y >= targetCollider.bounds.max.y - 0.1f;
        }

        private bool isValidWall(Collider2D targetCollider)
        {
            return targetCollider != null &&
                   !targetCollider.transform.IsChildOf(transform);
        }

        private Vector3 getWallCheckPosition(int direction)
        {
            Vector3 localPosition = transform.InverseTransformPoint(wallCheckPoint.position);
            localPosition.x = Mathf.Abs(localPosition.x) * direction;
            return transform.TransformPoint(localPosition);
        }

        private void OnValidate()
        {
            groundCheckSize.x = Mathf.Max(0f,groundCheckSize.x);
            groundCheckSize.y = Mathf.Max(0f,groundCheckSize.y);
            wallCheckSize.x = Mathf.Max(0f,wallCheckSize.x);
            wallCheckSize.y = Mathf.Max(0f,wallCheckSize.y);
        }
    }
}
