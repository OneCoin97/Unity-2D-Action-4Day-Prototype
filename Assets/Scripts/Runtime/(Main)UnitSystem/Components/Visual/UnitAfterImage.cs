using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class UnitAfterImage : MonoBehaviour
    {
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private SpriteRenderer sourceSpriteRenderer;
        private Transform poolRoot;
        private Color afterImageColor;
        private int sortingOrderOffset;
        private Coroutine fadeCoroutine;
        private PlayableGraph playableGraph;

        public bool isPlaying => spriteRenderer.enabled;

        public void initialize(SpriteRenderer targetSpriteRenderer,Transform targetPoolRoot,Color targetColor,int targetSortingOrderOffset)
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            sourceSpriteRenderer = targetSpriteRenderer;
            poolRoot = targetPoolRoot;
            afterImageColor = targetColor;
            sortingOrderOffset = targetSortingOrderOffset;
            animator.runtimeAnimatorController = null;
            animator.enabled = false;
            spriteRenderer.enabled = false;
        }

        public void playPose(float duration)
        {
            stop();
            transform.SetParent(null,true);
            copySourceRenderer();
            animator.enabled = false;
            spriteRenderer.enabled = true;
            fadeCoroutine = StartCoroutine(fade(duration));
        }

        public void playClip(AnimationClip animationClip,float normalizedTime,float playbackSpeed)
        {
            stop();
            transform.SetParent(null,true);
            copySourceRenderer();

            float startTime = animationClip.length * normalizedTime;
            float remainingAnimationTime =
                (animationClip.length - startTime) / playbackSpeed;

            animator.enabled = true;
            spriteRenderer.enabled = true;
            playableGraph = PlayableGraph.Create($"{name} Animation");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(
                playableGraph,
                animationClip);
            clipPlayable.SetTime(startTime);
            clipPlayable.SetSpeed(playbackSpeed);

            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(
                playableGraph,
                "Animation",
                animator);
            playableOutput.SetSourcePlayable(clipPlayable);
            playableGraph.Play();
            playableGraph.Evaluate(0f);
            spriteRenderer.color = afterImageColor;
            fadeCoroutine = StartCoroutine(fade(remainingAnimationTime));
        }

        public void stop()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }

            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }

            spriteRenderer.enabled = false;
            animator.enabled = false;
            transform.SetParent(poolRoot,false);
        }

        private void copySourceRenderer()
        {
            transform.SetPositionAndRotation(
                sourceSpriteRenderer.transform.position,
                sourceSpriteRenderer.transform.rotation);
            transform.localScale = sourceSpriteRenderer.transform.lossyScale;
            spriteRenderer.sprite = sourceSpriteRenderer.sprite;
            spriteRenderer.flipX = sourceSpriteRenderer.flipX;
            spriteRenderer.sharedMaterial = sourceSpriteRenderer.sharedMaterial;
            spriteRenderer.sortingLayerID = sourceSpriteRenderer.sortingLayerID;
            spriteRenderer.sortingOrder = sourceSpriteRenderer.sortingOrder + sortingOrderOffset;
            spriteRenderer.color = afterImageColor;
        }

        private IEnumerator fade(float duration)
        {
            float elapsedTime = 0f;
            Color color = afterImageColor;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                color.a = Mathf.Lerp(afterImageColor.a,0f,elapsedTime / duration);
                spriteRenderer.color = color;
                yield return null;
            }

            fadeCoroutine = null;
            stop();
        }

        private void OnDisable()
        {
            stop();
        }

        private void OnDestroy()
        {
            stop();
        }
    }
}
