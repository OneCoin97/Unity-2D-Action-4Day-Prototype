using System.Collections;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    public sealed class UnitEffectController : UnitComponent
    {
        private const UnitActionStateType hitStateMask =
            UnitActionStateType.Hit |
            UnitActionStateType.HitEnd;

        #region Inspector

        [Header("Hit")]
        [SerializeField, Min(0.01f)] private float hitBlinkInterval = 0.05f;
        [ColorUsage(true,true)]
        [SerializeField] private Color hitBlinkColor = new Color(3f,3f,3f,1f);
        [SerializeField] private Vector2 hitAngleRange = new Vector2(10f,20f);

        [Header("Super Armor")]
        [ColorUsage(true,true)]
        [SerializeField] private Color attackOutlineColor = new Color(1f,0.85f,0.2f,1f);
        [SerializeField, Min(1f)] private float attackOutlineScale = 1.05f;
        [SerializeField] private int outlineSortingOrderOffset = -1;

        [Header("Invincible")]
        [ColorUsage(true,true)]
        [SerializeField] private Color invincibleOutlineColor = new Color(0.2f,2f,3f,1f);
        [SerializeField, Min(1f)] private float invincibleOutlineScale = 1.06f;
        [SerializeField, Min(0f)] private float invinciblePulseSpeed = 6f;
        [SerializeField, Range(0f,1f)] private float invinciblePulseMinAlpha = 0.4f;

        [Header("Spawn")]
        [SerializeField, Min(0f)] private float spawnFadeDuration = 0.5f;

        #endregion

        private static readonly int effectColorHash = Shader.PropertyToID("_EffectColor");

        private Animator animator;
        private SpriteRenderer bodyRenderer;
        private Transform visualTransform;
        private SpriteRenderer outlineRenderer;
        private SpriteRenderer hitRenderer;
        private Material effectMaterial;
        private MaterialPropertyBlock effectPropertyBlock;
        private Coroutine outlineCoroutine;
        private Coroutine hitEffectCoroutine;
        private Coroutine spawnEffectCoroutine;
        private float animatorSpeed;
        private Quaternion visualRotation;
        private bool bodyRendererEnabledState;
        private bool isOutlineVisible;
        private bool isSuperArmorOutlineVisible;
        private bool isInvincibleOutlineVisible;
        private bool isHitEffectActive;

        protected override void onInitialize()
        {
            animator = GetComponentInChildren<Animator>();
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            visualTransform = bodyRenderer.transform;
            visualRotation = visualTransform.localRotation;
            effectMaterial = new Material(Shader.Find("Shader Graphs/UnitSpriteSilhouette"));
            effectPropertyBlock = new MaterialPropertyBlock();
            createEffectRenderers();
            applyCurrentState();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (unit == null)
            {
                return;
            }

            applyCurrentState();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stopSpawnEffect();
            stopHitEffect();
            setOutlineStatus(UnitStatusType.None);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(outlineRenderer.gameObject);
            Destroy(hitRenderer.gameObject);
            Destroy(effectMaterial);
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            if (nextStateType == UnitActionStateType.Spawn)
            {
                playSpawnEffect();
                return;
            }

            if (nextStateType == UnitActionStateType.Hit)
            {
                if (isHitEffectActive)
                {
                    stopHitEffect();
                }

                startHitEffect();
                return;
            }

            if (isHitEffectActive && nextStateType != UnitActionStateType.HitEnd)
            {
                stopHitEffect();
            }
        }

        protected override void onStatusChanged(UnitStatusType previousStatusType,UnitStatusType nextStatusType)
        {
            setOutlineStatus(nextStatusType);
        }

        public void playSpawnEffect()
        {
            stopSpawnEffect();

            Color bodyColor = bodyRenderer.color;
            bodyColor.a = 0f;
            bodyRenderer.color = bodyColor;
            spawnEffectCoroutine = StartCoroutine(processSpawnEffect());
        }

        private void applyCurrentState()
        {
            UnitActionStateType currentStateType = unit.unitStateController.currentActionStateType;
            setOutlineStatus(unit.unitStateController.currentStatusType);

            if ((currentStateType & hitStateMask) != 0)
            {
                startHitEffect();
            }
        }

        private void startHitEffect()
        {
            if (isHitEffectActive)
            {
                return;
            }

            isHitEffectActive = true;
            animatorSpeed = animator.speed;
            animator.speed = 0f;
            float angle = Random.Range(hitAngleRange.x,hitAngleRange.y);
            float hitAngle = Random.value < 0.5f ? -angle : angle;
            visualTransform.localRotation = visualRotation * Quaternion.Euler(0f,0f,hitAngle);
            bodyRendererEnabledState = bodyRenderer.enabled;
            bodyRenderer.enabled = false;
            hitEffectCoroutine = StartCoroutine(processHitEffect());
        }

        private void stopHitEffect()
        {
            if (!isHitEffectActive)
            {
                return;
            }

            if (hitEffectCoroutine != null)
            {
                StopCoroutine(hitEffectCoroutine);
                hitEffectCoroutine = null;
            }

            animator.speed = animatorSpeed;
            visualTransform.localRotation = visualRotation;
            isHitEffectActive = false;
            bodyRenderer.enabled = bodyRendererEnabledState;
            hitRenderer.enabled = false;
        }

        private IEnumerator processHitEffect()
        {
            bool isBlinkVisible = true;
            WaitForSeconds blinkInterval = new WaitForSeconds(hitBlinkInterval);
            while (isHitEffectActive)
            {
                updateHitEffectRenderer(isBlinkVisible);
                yield return blinkInterval;
                isBlinkVisible = !isBlinkVisible;
            }

            hitEffectCoroutine = null;
        }

        private IEnumerator processSpawnEffect()
        {
            float elapsedTime = 0f;
            while (elapsedTime < spawnFadeDuration)
            {
                elapsedTime += Time.deltaTime;
                Color bodyColor = bodyRenderer.color;
                bodyColor.a = Mathf.Clamp01(elapsedTime / spawnFadeDuration);
                bodyRenderer.color = bodyColor;
                yield return null;
            }

            Color finalBodyColor = bodyRenderer.color;
            finalBodyColor.a = 1f;
            bodyRenderer.color = finalBodyColor;
            spawnEffectCoroutine = null;
        }

        private void stopSpawnEffect()
        {
            if (spawnEffectCoroutine != null)
            {
                StopCoroutine(spawnEffectCoroutine);
                spawnEffectCoroutine = null;
            }

            Color bodyColor = bodyRenderer.color;
            bodyColor.a = 1f;
            bodyRenderer.color = bodyColor;
        }

        private void setOutlineStatus(UnitStatusType statusType)
        {
            isSuperArmorOutlineVisible = (statusType & UnitStatusType.SuperArmor) != 0;
            isInvincibleOutlineVisible = (statusType & UnitStatusType.ParrySuccess) != 0;
            isOutlineVisible = isSuperArmorOutlineVisible || isInvincibleOutlineVisible;
            if (isOutlineVisible)
            {
                if (outlineCoroutine == null)
                {
                    outlineCoroutine = StartCoroutine(processOutline());
                }

                return;
            }

            stopOutline();
        }

        private IEnumerator processOutline()
        {
            while (isOutlineVisible)
            {
                updateOutlineRenderer();
                yield return null;
            }

            outlineCoroutine = null;
        }

        private void stopOutline()
        {
            if (outlineCoroutine != null)
            {
                StopCoroutine(outlineCoroutine);
                outlineCoroutine = null;
            }

            outlineRenderer.enabled = false;
        }

        private void createEffectRenderers()
        {
            GameObject outlineObject = new GameObject($"{bodyRenderer.name}_Outline");
            outlineObject.layer = bodyRenderer.gameObject.layer;
            outlineObject.transform.SetParent(bodyRenderer.transform,false);
            outlineRenderer = outlineObject.AddComponent<SpriteRenderer>();
            outlineRenderer.sharedMaterial = effectMaterial;
            outlineRenderer.enabled = false;

            GameObject hitObject = new GameObject($"{bodyRenderer.name}_Hit");
            hitObject.layer = bodyRenderer.gameObject.layer;
            hitObject.transform.SetParent(bodyRenderer.transform,false);
            hitRenderer = hitObject.AddComponent<SpriteRenderer>();
            hitRenderer.enabled = false;
        }

        private void updateHitEffectRenderer(bool isBlinkVisible)
        {
            syncRenderer(hitRenderer,0);
            hitRenderer.sharedMaterial = isBlinkVisible
                ? effectMaterial
                : bodyRenderer.sharedMaterial;
            hitRenderer.color = isBlinkVisible ? Color.white : bodyRenderer.color;
            if (isBlinkVisible)
            {
                effectPropertyBlock.SetColor(effectColorHash,hitBlinkColor);
                hitRenderer.SetPropertyBlock(effectPropertyBlock);
            }
            else
            {
                effectPropertyBlock.Clear();
                hitRenderer.SetPropertyBlock(effectPropertyBlock);
            }

            hitRenderer.enabled = bodyRendererEnabledState && bodyRenderer.sprite != null;
        }

        private void updateOutlineRenderer()
        {
            float outlineScale = isInvincibleOutlineVisible
                ? invincibleOutlineScale
                : attackOutlineScale;

            syncRenderer(outlineRenderer,outlineSortingOrderOffset);
            outlineRenderer.transform.localScale = Vector3.one * outlineScale;
            effectPropertyBlock.SetColor(effectColorHash,getOutlineColor());
            outlineRenderer.SetPropertyBlock(effectPropertyBlock);
            bool isBodyVisible = isHitEffectActive
                ? bodyRendererEnabledState
                : bodyRenderer.enabled;
            outlineRenderer.enabled = isBodyVisible && bodyRenderer.sprite != null;
        }

        private void syncRenderer(SpriteRenderer targetRenderer,int sortingOrderOffset)
        {
            targetRenderer.sprite = bodyRenderer.sprite;
            targetRenderer.flipX = bodyRenderer.flipX;
            targetRenderer.flipY = bodyRenderer.flipY;
            targetRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            targetRenderer.sortingOrder = bodyRenderer.sortingOrder + sortingOrderOffset;
        }

        private Color getOutlineColor()
        {
            if (!isInvincibleOutlineVisible)
            {
                return attackOutlineColor;
            }

            float pulse = (Mathf.Sin(Time.unscaledTime * invinciblePulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            Color outlineColor = invincibleOutlineColor;
            outlineColor.a *= Mathf.Lerp(invinciblePulseMinAlpha,1f,pulse);
            return outlineColor;
        }

        private void OnValidate()
        {
            hitBlinkInterval = Mathf.Max(0.01f,hitBlinkInterval);
            hitAngleRange.x = Mathf.Max(0f,hitAngleRange.x);
            hitAngleRange.y = Mathf.Max(hitAngleRange.x,hitAngleRange.y);
            attackOutlineScale = Mathf.Max(1f,attackOutlineScale);
            invincibleOutlineScale = Mathf.Max(1f,invincibleOutlineScale);
            invinciblePulseSpeed = Mathf.Max(0f,invinciblePulseSpeed);
            spawnFadeDuration = Mathf.Max(0f,spawnFadeDuration);
        }
    }
}
