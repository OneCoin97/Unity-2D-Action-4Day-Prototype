using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitComponents
{
    [DisallowMultipleComponent]
    public sealed class UnitAudioController : UnitComponent
    {
        #region Inspector

        [Header("Audio Source")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource footstepAudioSource;

        [Header("Spawn")]
        [SerializeField] private AudioClip spawnSound;
        [SerializeField, Range(0f,1f)] private float spawnSoundVolume = 1f;

        [Header("Voice")]
        [SerializeField] private List<AudioClip> attackVoiceList = new List<AudioClip>();
        [SerializeField, Range(0f,1f)] private float attackVoiceVolume = 1f;
        [SerializeField] private AudioClip jumpSound;
        [SerializeField, Range(0f,1f)] private float jumpSoundVolume = 1f;

        [Header("Footstep")]
        [SerializeField] private List<AudioClip> footstepList = new List<AudioClip>();
        [SerializeField, Range(0f,1f)] private float footstepVolume = 1f;
        [SerializeField] private AudioClip dashSound;
        [SerializeField, Range(0f,1f)] private float dashSoundVolume = 1f;
        [SerializeField, Min(0.05f)] private float footstepInterval = 0.32f;

        [Header("Parry")]
        [SerializeField] private AudioClip parrySuccessSound;
        [SerializeField, Range(0f,1f)] private float parrySuccessSoundVolume = 1f;

        #endregion

        private Coroutine footstepCoroutine;

        protected override void onInitialize()
        {
            prepareAudioSources();
            refreshFootstep();
        }

        private void Reset()
        {
            prepareAudioSources();
        }

        public void playJumpSound()
        {
            if (jumpSound == null)
            {
                return;
            }

            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(jumpSound,jumpSoundVolume);
        }

        public void playFootstep()
        {
            AudioClip footstep = getRandomClip(footstepList);
            if (footstep == null)
            {
                return;
            }

            footstepAudioSource.PlayOneShot(footstep,footstepVolume);
        }

        public void playDashSound()
        {
            if (dashSound == null)
            {
                return;
            }

            footstepAudioSource.Stop();
            footstepAudioSource.PlayOneShot(dashSound,dashSoundVolume);
        }

        public void playParrySuccessSound()
        {
            if (parrySuccessSound == null)
            {
                return;
            }

            footstepAudioSource.PlayOneShot(parrySuccessSound,parrySuccessSoundVolume);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            stopFootstep();
            voiceAudioSource.Stop();
            footstepAudioSource.Stop();
        }

        protected override void onActionStateChanged(UnitActionStateType previousStateType,UnitActionStateType nextStateType)
        {
            refreshFootstep();

            if (nextStateType == UnitActionStateType.Spawn && spawnSound != null)
            {
                voiceAudioSource.PlayOneShot(spawnSound,spawnSoundVolume);
                return;
            }

            if (nextStateType == UnitActionStateType.AttackStart)
            {
                playVoice(attackVoiceList,attackVoiceVolume);
            }
        }

        protected override void onMovementStateChanged(UnitMovementStateType previousStateType,UnitMovementStateType nextStateType)
        {
            refreshFootstep();

            if (nextStateType == UnitMovementStateType.Jump)
            {
                playJumpSound();
                return;
            }

            if (nextStateType == UnitMovementStateType.Dash)
            {
                playDashSound();
            }
        }

        protected override void onStatusChanged(UnitStatusType previousStatusType,UnitStatusType nextStatusType)
        {
            bool wasParrySuccess = (previousStatusType & UnitStatusType.ParrySuccess) != 0;
            bool isParrySuccess = (nextStatusType & UnitStatusType.ParrySuccess) != 0;
            if (!wasParrySuccess && isParrySuccess)
            {
                playParrySuccessSound();
            }
        }

        private void refreshFootstep()
        {
            bool canPlayFootstep = unit != null &&
                                   unit.unitStateController.stateData.isGround &&
                                   unit.unitStateController.stateData.isMove &&
                                   unit.unitStateController.currentActionStateType == UnitActionStateType.None;
            if (canPlayFootstep)
            {
                if (footstepCoroutine == null)
                {
                    footstepCoroutine = StartCoroutine(playFootstepLoop());
                }

                return;
            }

            stopFootstep();
        }

        private IEnumerator playFootstepLoop()
        {
            while (unit.unitStateController.stateData.isGround &&
                   unit.unitStateController.stateData.isMove &&
                   unit.unitStateController.currentActionStateType == UnitActionStateType.None)
            {
                playFootstep();
                yield return new WaitForSeconds(footstepInterval);
            }

            footstepCoroutine = null;
        }

        private void stopFootstep()
        {
            if (footstepCoroutine == null)
            {
                return;
            }

            StopCoroutine(footstepCoroutine);
            footstepCoroutine = null;
        }

        private void playVoice(List<AudioClip> voiceList,float volume)
        {
            AudioClip voice = getRandomClip(voiceList);
            if (voice == null)
            {
                return;
            }

            voiceAudioSource.Stop();
            voiceAudioSource.PlayOneShot(voice,volume);
        }

        private AudioClip getRandomClip(List<AudioClip> audioClipList)
        {
            if (audioClipList.Count == 0)
            {
                return null;
            }

            return audioClipList[Random.Range(0,audioClipList.Count)];
        }

        private void prepareAudioSources()
        {
            AudioSource[] audioSources = GetComponents<AudioSource>();
            if (voiceAudioSource == null && audioSources.Length > 0)
            {
                voiceAudioSource = audioSources[0];
            }

            if (footstepAudioSource == null)
            {
                for (int i = 0; i < audioSources.Length; i++)
                {
                    if (audioSources[i] != voiceAudioSource)
                    {
                        footstepAudioSource = audioSources[i];
                        break;
                    }
                }
            }

            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }

            if (footstepAudioSource == null)
            {
                footstepAudioSource = gameObject.AddComponent<AudioSource>();
            }

            voiceAudioSource.playOnAwake = false;
            footstepAudioSource.playOnAwake = false;
        }

        private void OnValidate()
        {
            footstepInterval = Mathf.Max(0.05f,footstepInterval);
        }
    }
}
