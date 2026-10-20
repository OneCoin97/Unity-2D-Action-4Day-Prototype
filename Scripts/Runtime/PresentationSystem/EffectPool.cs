using System.Collections.Generic;
using UnityEngine;

namespace UnitComponents
{
    public sealed class EffectPool : MonoBehaviour
    {
        private static EffectPool currentInstance;

        public static EffectPool instance
        {
            get
            {
                if (currentInstance == null)
                {
                    GameObject poolObject = new GameObject(nameof(EffectPool));
                    currentInstance = poolObject.AddComponent<EffectPool>();
                }

                return currentInstance;
            }
        }

        private readonly Stack<EffectPlayer> effectPlayerPool = new Stack<EffectPlayer>();
        private int effectPlayerCount;

        private void Awake()
        {
            currentInstance = this;
        }

        public EffectPlayer getEffectPlayer(Transform parent,int layer)
        {
            EffectPlayer effectPlayer = effectPlayerPool.Count > 0
                ? effectPlayerPool.Pop()
                : createEffectPlayer();

            effectPlayer.gameObject.layer = layer;
            effectPlayer.transform.SetParent(parent,false);
            effectPlayer.transform.localPosition = Vector3.zero;
            effectPlayer.transform.localRotation = Quaternion.identity;
            effectPlayer.transform.localScale = Vector3.one;
            effectPlayer.gameObject.SetActive(true);
            return effectPlayer;
        }

        public void playEffect(AnimationClip animationClip,Vector2 position,int direction,int layer)
        {
            if (animationClip == null)
            {
                return;
            }

            EffectPlayer effectPlayer = getEffectPlayer(transform,layer);
            effectPlayer.transform.position = position;
            effectPlayer.play(animationClip,direction,true);
        }

        public void releaseEffectPlayer(EffectPlayer effectPlayer)
        {
            effectPlayer.stop();
            effectPlayer.transform.SetParent(transform,false);
            effectPlayer.gameObject.SetActive(false);
            effectPlayerPool.Push(effectPlayer);
        }

        private EffectPlayer createEffectPlayer()
        {
            GameObject effectObject = new GameObject($"SkillEffect_{effectPlayerCount++}");
            effectObject.transform.SetParent(transform,false);
            effectObject.AddComponent<SpriteRenderer>();
            effectObject.AddComponent<Animator>();
            EffectPlayer effectPlayer = effectObject.AddComponent<EffectPlayer>();
            effectPlayer.initialize(this);
            return effectPlayer;
        }

        private void OnDestroy()
        {
            if (currentInstance == this)
            {
                currentInstance = null;
            }

            effectPlayerPool.Clear();
        }
    }
}
