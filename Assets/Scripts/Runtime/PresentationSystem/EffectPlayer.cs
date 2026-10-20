using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EffectPlayer : MonoBehaviour
    {
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private EffectPool effectPool;
        private PlayableGraph playableGraph;
        private Coroutine releaseCoroutine;

        public void initialize(EffectPool targetEffectPool)
        {
            effectPool = targetEffectPool;
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.enabled = false;
        }

        public void play(AnimationClip animationClip,int direction,bool releaseAfterPlay)
        {
            stop();
            if (animationClip == null)
            {
                return;
            }

            spriteRenderer.flipX = direction < 0;
            spriteRenderer.enabled = true;
            playableGraph = PlayableGraph.Create($"{name} Effect");
            playableGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            AnimationClipPlayable clipPlayable = AnimationClipPlayable.Create(
                playableGraph,
                animationClip);
            AnimationPlayableOutput playableOutput = AnimationPlayableOutput.Create(
                playableGraph,
                "Animation",
                animator);
            playableOutput.SetSourcePlayable(clipPlayable);
            playableGraph.Play();

            if (releaseAfterPlay)
            {
                releaseCoroutine = StartCoroutine(release(animationClip.length));
            }
        }

        public void setOffset(Vector2 offset,int direction)
        {
            offset.x *= direction;
            transform.localPosition = offset;
        }

        public void stop()
        {
            if (releaseCoroutine != null)
            {
                StopCoroutine(releaseCoroutine);
                releaseCoroutine = null;
            }

            if (playableGraph.IsValid())
            {
                playableGraph.Destroy();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

        private IEnumerator release(float animationTime)
        {
            yield return new WaitForSeconds(animationTime);

            releaseCoroutine = null;
            effectPool.releaseEffectPlayer(this);
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
