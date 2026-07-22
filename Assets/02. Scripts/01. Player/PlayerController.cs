using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{

    public class PlayerController : MonoBehaviour 
    {
        private const string ANIM_TAG_ATTACK = "Attack";
        private const string ANIM_TAG_ATTACK_END = "Attack_End";
        private const string ANIM_TAG_MOVEMENT = "Movement";
        private const string ANIM_TAG_JUMP = "Jump";
        private const string ANIM_TAG_PARRY = "Parry";

        public static readonly int MOVEMENT = Animator.StringToHash("Movement");
        public static readonly int IS_SPRINT = Animator.StringToHash("IsSprint");
        public static readonly int IS_ATTACK = Animator.StringToHash("IsAttack");
        public static readonly int IS_GROUNDED = Animator.StringToHash("IsGrounded");
        public static readonly int JUMP = Animator.StringToHash("Jump");
        public static readonly int DOUBLE_JUMP = Animator.StringToHash("DoubleJump");
        public static readonly int DESCENDING = Animator.StringToHash("Descending");
        public static readonly int DASH = Animator.StringToHash("Dash");
        public static readonly int PARRY = Animator.StringToHash("Parry");
        public static readonly int PARRY_SUCCESS = Animator.StringToHash("ParrySuccess");
        public static readonly int IS_HURT = Animator.StringToHash("IsHurt");
        public static readonly int IS_CASTING_SKILL = Animator.StringToHash("IsCastingSkill");
        public static readonly int DEATH = Animator.StringToHash("Death");


        public Animator Animator { get; private set; }
        public int FacingDirection => locomotion?.FacingDirection ?? 1;

        [Header("Dash")]
        [SerializeField] private float dashPower = 10.0f;
        [SerializeField] private float dashDuration = 0.15f;
        [SerializeField] private float dashCooldown = 0.8f;

        [Header("Parry")]
        [SerializeField] private float parryCooldown = 1.2f;
        [SerializeField] private ParryLightEffect parryLightEffect;

        [Header("Sprint")]
        [SerializeField] private float sprintMultiplier = 1.5f;
        [SerializeField] private float staminaCostPerSec = 20.0f;
        [SerializeField] private float staminaRecoveryPerSec = 10.0f;
        [SerializeField] private float staminaRecoveryDelay = 0.6f;
        [SerializeField] private float sprintInputGraceTime = 0.08f;

        [Header("Hurt")]
        [SerializeField] private float hurtDuration = 0.15f;

        [Header("FX")]
        [SerializeField] private GameObject doubleJumpFxPrefab;
        [SerializeField] private GameObject dashFxPrefab;
        [SerializeField] private GameObject sprintLoopFxPrefab;
        [SerializeField] private GameObject sprintEndFxPrefab;
        [SerializeField] private Transform fxSpawnPoint;
        [SerializeField] private Transform sprintFxSpawnPoint;

        [SerializeField] private ScreenEffectController screenEffectController;

        [Header("UI")]
        [SerializeField] private OverlayController overlayController;

        [Header("Card")]
        [SerializeField] private CardSystem cardSystem;
        [SerializeField] private CardGaugeController cardGaugeController;
        [SerializeField] private int parryGaugeCellReward = 3;

        private float nextParryTime;
        private float hurtEndTime;
        private bool isHurt;
        private bool isCastingSkill;
        private bool isDead;
        private Dictionary<int, PlayerAnimStateBase> StateDic { get; } = new();

        public bool IsParrying { get; set; }
        public bool IsParryWindowOpen { get; private set; }

        private PlayerAnimStateBase defaultState;
        private PlayerAnimStateBase currentState;
        private PlayerLocomotionRuntime locomotion;


        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            CharacterController2D controller2D = GetComponent<CharacterController2D>();

            PlayerLocomotionSettings locomotionSettings = new(
                dashPower,
                dashDuration,
                dashCooldown,
                sprintMultiplier,
                staminaCostPerSec,
                staminaRecoveryPerSec,
                staminaRecoveryDelay,
                sprintInputGraceTime);
            locomotion = new PlayerLocomotionRuntime(
                transform,
                Animator,
                controller2D,
                locomotionSettings,
                doubleJumpFxPrefab,
                dashFxPrefab,
                sprintLoopFxPrefab,
                sprintEndFxPrefab,
                fxSpawnPoint,
                sprintFxSpawnPoint);

            defaultState = new MovementState(this);
            StateDic.Add(Animator.StringToHash(ANIM_TAG_MOVEMENT), defaultState);
            StateDic.Add(Animator.StringToHash(ANIM_TAG_ATTACK), new AttackState(this));
            StateDic.Add(Animator.StringToHash(ANIM_TAG_ATTACK_END), new AttackEndState(this));
            StateDic.Add(Animator.StringToHash(ANIM_TAG_JUMP), new JumpState(this));
            StateDic.Add(Animator.StringToHash(ANIM_TAG_PARRY), new ParryState(this));
        }

        private void Update()
        {
            UpdateCooldownUI();

            if (isDead) return;

            if (isHurt)
            {
                if (Time.time >= hurtEndTime) EndHurt();
                else return;
            }

            UpdateAnimState();
            UpdateJumpInput();
            UpdateDashInput();
            UpdateGroundedAnimation();
            locomotion.UpdateStaminaRecovery();
        }

        private void UpdateAnimState()
        {
            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);

            PlayerAnimStateBase nextState = StateDic.GetValueOrDefault(stateInfo.tagHash, defaultState);
            
            if (currentState != nextState)
            {
                currentState?.Exit();
                currentState = nextState;
                nextState.Enter();
            }

            currentState.UpdateState(stateInfo);
        }

        public void HandleMovement()
        {
            if (isCastingSkill)
            {
                StopMovement();
                Animator.SetFloat(MOVEMENT, 0.0f);
                return;
            }

            if (isHurt || isDead)
            {
                StopMovement();
                return;
            }

            locomotion.HandleMovement(SimpleInput.GetMoveAxisRaw().x, true);

            if (SimpleInput.GetKeyDown(Key.D))
                Animator.SetBool(IS_ATTACK, true);

            if (SimpleInput.GetKeyDown(Key.S) && Time.time >= nextParryTime)
                StartParry();
        }

        public void HandleMovementWithoutAnimation(float moveScale = 0.75f)
        {
            if (isCastingSkill)
            {
                StopMovement();
                return;
            }

            if (isHurt || isDead)
            {
                StopMovement();
                return;
            }

            float horizontal = locomotion.GetArrowHorizontalInput(moveScale);
            locomotion.HandleMovement(horizontal, false);
        }

        private void StartParry()
        {
            nextParryTime = Time.time + parryCooldown;
            IsParrying = true;
            StopMovement();
            OpenParryWindow();
            Animator.SetTrigger(PARRY);
            Debug.Log("[Parry] Start");
        }

        public void StopMovement()
        {
            locomotion.StopMovement();
        }

        public void OpenParryWindow()
        {
            if (IsParryWindowOpen) return;

            IsParryWindowOpen = true;
        }

        public void CloseParryWindow()
        {
            if (IsParryWindowOpen == false) return;

            IsParryWindowOpen = false;
        }

        public void EndParry()
        {
            IsParrying = false;
            CloseParryWindow();
        }

        public void SuccessParry()
        {
            IsParrying = false;
            CloseParryWindow();

            cardGaugeController?.AddCells(parryGaugeCellReward);
            PlayParrySuccessEffects();

            Animator.ResetTrigger(PARRY);
            Animator.SetTrigger(PARRY_SUCCESS);
        }

        private void PlayParrySuccessEffects()
        {
            if (parryLightEffect != null)
            {
                parryLightEffect.Play();
            }

            if (screenEffectController != null)
            {
                screenEffectController.PlayParryGlow();
            }
        }

        public void Hurt()
        {
            if (isDead) return;

            screenEffectController.PlayDamageGlow();

            if (isCastingSkill)
                return;

            isHurt = true;
            hurtEndTime = Time.time + hurtDuration;
            IsParrying = false;
            CloseParryWindow();
            StopMovement();
            Animator.SetBool(IS_HURT, true);
        }

        private void EndHurt()
        {
            isHurt = false;
            Animator.SetBool(IS_HURT, false);
        }

        public void Dead()
        {
            if (isDead) return;

            isDead = true;
            isHurt = false;
            IsParrying = false;
            CloseParryWindow();
            StopMovement();
            screenEffectController.PlayDamageGlow();
            Animator.SetBool(IS_HURT, false);
            Animator.SetTrigger(DEATH);

            overlayController.StartDeathOverlay();
        }

        public void Respawn(Vector3 position, int hp)
        {
            Checkpoint.BlockSaveForSeconds(1.0f);
            transform.position = position;

            isDead = false;
            isHurt = false;
            isCastingSkill = false;
            IsParrying = false;
            CloseParryWindow();
            StopMovement();
            locomotion.ResetForRespawn();

            Animator.Rebind();
            Animator.Update(0.0f);

            Animator.ResetTrigger(DEATH);
            Animator.ResetTrigger(PARRY);
            Animator.ResetTrigger(PARRY_SUCCESS);
            Animator.ResetTrigger(DASH);
            Animator.ResetTrigger(JUMP);
            Animator.ResetTrigger(DOUBLE_JUMP);
            Animator.ResetTrigger(DESCENDING);
            Animator.SetBool(IS_HURT, false);
            Animator.SetBool(IS_CASTING_SKILL, false);
            Animator.SetBool(IS_ATTACK, false);
            Animator.SetBool(IS_SPRINT, false);
            Animator.SetBool(IS_GROUNDED, locomotion.IsGrounded);
            Animator.SetFloat(MOVEMENT, 0.0f);
            Animator.Play(ANIM_TAG_MOVEMENT, 0, 0.0f);
            Animator.Update(0.0f);

            currentState?.Exit();
            currentState = null;

            nextParryTime = 0.0f;
            hurtEndTime = 0.0f;

            Player.LocalPlayer?.RestoreHealthTo(hp);
            cardSystem?.ResetRuntimeCardsForRespawn();
                
            Debug.Log($"[Player] Respawned at {position} / HP {hp}");
        }

        public void ShowDeathWindow()
        {
            overlayController.ShowDeathWindow();
        }

        public void ExecutePendingCardSkill()
        {
            if (cardSystem == null)
                return;

            cardSystem.ExecutePendingCardSkill();
        }

        public void BeginCardSkill()
        {
            isCastingSkill = true;
            Animator.SetBool(IS_CASTING_SKILL, true);
        }

        public void EndCardSkill()
        {
            isCastingSkill = false;
            Animator.SetBool(IS_CASTING_SKILL, false);
        }

        private void UpdateJumpInput()
        {
            locomotion.UpdateJumpInput(IsPlayerActionBlocked());
        }

        private void UpdateDashInput()
        {
            locomotion.UpdateDashInput(IsPlayerActionBlocked());
        }

        private bool IsPlayerActionBlocked()
        {
            return IsParrying || isHurt || isDead || isCastingSkill;
        }

        private void UpdateCooldownUI()
        {
            if (Player.LocalPlayer == null) return;

            Player.LocalPlayer.UpdateParryCooldownUI(Mathf.Max(0.0f, nextParryTime - Time.time), parryCooldown);
        }

        private void UpdateGroundedAnimation()
        {
            locomotion.UpdateGroundedAnimation(IsParrying || isHurt || isDead);
        }
    }
}
