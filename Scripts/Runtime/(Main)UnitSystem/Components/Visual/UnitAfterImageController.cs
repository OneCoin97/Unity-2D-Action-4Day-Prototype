using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class UnitAfterImageController : UnitComponent
    {
        #region Inspector

        [SerializeField] private Color afterImageColor = new Color(0.3f,0.8f,1f,0.6f);
        [SerializeField, Min(1)] private int poolSize = 4;
        [SerializeField] private int sortingOrderOffset = -1;

        [Header("Dash")]
        [SerializeField, Min(0.01f)] private float dashAfterImageInterval = 0.05f;
        [SerializeField, Min(0.01f)] private float dashAfterImageDuration = 0.15f;

        #endregion

        private readonly List<UnitAfterImage> afterImagePool = new List<UnitAfterImage>();

        private Animator sourceAnimator;
        private SpriteRenderer sourceSpriteRenderer;
        private Transform poolRoot;
        private Coroutine dashAfterImageCoroutine;
        private int nextAfterImageIndex;

        protected override void onInitialize()
        {
            sourceAnimator = GetComponentInChildren<Animator>();
            sourceSpriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stopDashAfterImage();

            foreach (UnitAfterImage afterImage in afterImagePool)
            {
                afterImage.stop();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            stopDashAfterImage();

            if (poolRoot != null)
            {
                Destroy(poolRoot.gameObject);
            }
        }

        protected override void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
            if (previousStateType == UnitMovementStateType.Dash)
            {
                stopDashAfterImage();
            }

            if (nextStateType == UnitMovementStateType.Dash)
            {
                stopDashAfterImage();
                dashAfterImageCoroutine = StartCoroutine(spawnDashAfterImages());
            }
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if (previousStateType == UnitActionStateType.AttackEnd &&
                nextStateType == UnitActionStateType.AttackStart)
            {
                playCurrentAnimation();
            }
        }

        private IEnumerator spawnDashAfterImages()
        {
            while (unit.unitStateController.isCurrentMovementState(UnitMovementStateType.Dash))
            {
                playCurrentPose();
                yield return new WaitForSeconds(dashAfterImageInterval);
            }

            dashAfterImageCoroutine = null;
        }

        private void stopDashAfterImage()
        {
            if (dashAfterImageCoroutine == null)
            {
                return;
            }

            StopCoroutine(dashAfterImageCoroutine);
            dashAfterImageCoroutine = null;
        }

        private void playCurrentPose()
        {
            UnitAfterImage afterImage = getAfterImage();
            afterImage.playPose(dashAfterImageDuration);
        }

        private void playCurrentAnimation()
        {
            bool isTransition = sourceAnimator.IsInTransition(0);
            AnimatorStateInfo stateInfo = isTransition
                ? sourceAnimator.GetNextAnimatorStateInfo(0)
                : sourceAnimator.GetCurrentAnimatorStateInfo(0);
            AnimatorClipInfo[] clipInfos = isTransition
                ? sourceAnimator.GetNextAnimatorClipInfo(0)
                : sourceAnimator.GetCurrentAnimatorClipInfo(0);

            if (clipInfos.Length == 0 || clipInfos[0].clip == null)
            {
                return;
            }

            AnimationClip animationClip = clipInfos[0].clip;
            float normalizedTime = stateInfo.normalizedTime;
            if (stateInfo.loop)
            {
                normalizedTime %= 1f;
            }
            else if (normalizedTime >= 1f)
            {
                return;
            }

            float playbackSpeed = Mathf.Abs(
                stateInfo.speed * stateInfo.speedMultiplier * sourceAnimator.speed);
            if (Mathf.Approximately(playbackSpeed,0f))
            {
                playbackSpeed = 1f;
            }

            UnitAfterImage afterImage = getAfterImage();
            afterImage.playClip(animationClip,normalizedTime,playbackSpeed);
        }

        private UnitAfterImage getAfterImage()
        {
            if (afterImagePool.Count == 0)
            {
                createPool();
            }

            for (int offset = 0; offset < afterImagePool.Count; offset++)
            {
                int index = (nextAfterImageIndex + offset) % afterImagePool.Count;
                if (!afterImagePool[index].isPlaying)
                {
                    nextAfterImageIndex = (index + 1) % afterImagePool.Count;
                    return afterImagePool[index];
                }
            }

            UnitAfterImage afterImage = afterImagePool[nextAfterImageIndex];
            nextAfterImageIndex = (nextAfterImageIndex + 1) % afterImagePool.Count;
            return afterImage;
        }

        private void createPool()
        {
            poolRoot = new GameObject($"{name} After Image Pool").transform;
            poolRoot.SetParent(unit.transform,false);

            for (int index = 0; index < Mathf.Max(1,poolSize); index++)
            {
                GameObject afterImageObject = new GameObject($"After Image {index + 1}");
                afterImageObject.layer = sourceSpriteRenderer.gameObject.layer;
                afterImageObject.transform.SetParent(poolRoot,false);
                UnitAfterImage afterImage = afterImageObject.AddComponent<UnitAfterImage>();
                afterImage.initialize(sourceSpriteRenderer,poolRoot,afterImageColor,sortingOrderOffset);
                afterImagePool.Add(afterImage);
            }
        }

        private void OnValidate()
        {
            poolSize = Mathf.Max(1,poolSize);
            dashAfterImageInterval = Mathf.Max(0.01f,dashAfterImageInterval);
            dashAfterImageDuration = Mathf.Max(0.01f,dashAfterImageDuration);
        }
    }
}
