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
        private CharacterController2D Controller2D { get; set; }

        public int FacingDirection { get; private set; } = 1;

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

        private float nextDashTime;
        private float nextParryTime;
        private float hurtEndTime;
        private float staminaRecoveryStartTime;
        private float sprintInputGraceEndTime;
        private bool isSprint;
        private GameObject sprintLoopFx;
        private bool isHurt;
        private bool isCastingSkill;
        private bool isDead;
        private Dictionary<int, PlayerAnimStateBase> StateDic { get; set; } = new();

        public bool IsParrying { get; set; } = false;
        public bool IsParryWindowOpen { get; private set; } = false;

        private PlayerAnimStateBase defaultState;
        private PlayerAnimStateBase currentState;


        private void Awake()
        {
            Animator = GetComponentInChildren<Animator>();
            Controller2D = GetComponent<CharacterController2D>();

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
            UpdateStaminaRecovery();
        }

        private void UpdateAnimState()
        {
            AnimatorStateInfo stateInfo = Animator.GetCurrentAnimatorStateInfo(0);

            if (StateDic.TryGetValue(stateInfo.tagHash, out PlayerAnimStateBase nextState) == false)
                nextState = defaultState;
            
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

            Vector2 inputVector = SimpleInput.GetMoveAxisRaw();
            float absMovement = Mathf.Abs(inputVector.x);
            bool hasMoveInput = absMovement > 0.0f;
            bool canSprint = UpdateSprintState(hasMoveInput, Keyboard.current.leftShiftKey.isPressed);

            Animator.SetFloat(MOVEMENT, canSprint ? 2.0f : absMovement);
            ApplyFacing(inputVector.x);
            ApplyMoveInput(inputVector.x, canSprint);

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

            float horizontal = GetHorizontalInput(moveScale);
            bool hasMoveInput = Mathf.Abs(horizontal) > 0.0f;
            bool canSprint = UpdateSprintState(hasMoveInput, Keyboard.current.leftShiftKey.isPressed);

            ApplyFacing(horizontal);
            ApplyMoveInput(horizontal, canSprint);
        }

        private float GetHorizontalInput(float moveScale)
        {
            if (Keyboard.current.leftArrowKey.isPressed) return -moveScale;

            if (Keyboard.current.rightArrowKey.isPressed) return moveScale;

            return 0.0f;
        }

        private bool UpdateSprintState(bool hasMoveInput, bool shiftPressed)
        {
            if (shiftPressed && hasMoveInput)
                sprintInputGraceEndTime = Time.time + sprintInputGraceTime;

            bool wantsSprint = Controller2D.IsGrounded && shiftPressed && (hasMoveInput || Time.time < sprintInputGraceEndTime);
            bool canSprint = TrySprint(wantsSprint);

            SetSprintState(canSprint, Controller2D.IsGrounded);
            return canSprint;
        }

        private void ApplyFacing(float horizontal)
        {
            if (Mathf.Abs(horizontal) <= 0.0f) return;

            FacingDirection = horizontal < 0.0f ? -1 : 1;

            Vector3 scale = Animator.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            Animator.transform.localScale = scale;
        }

        private void ApplyMoveInput(float horizontal, bool canSprint)
        {
            if (canSprint) horizontal *= sprintMultiplier;

            Controller2D.SetMoveInput(horizontal);
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
            Controller2D.SetMoveInput(0.0f);
            SetSprintState(false);
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
            if (IsParrying || isHurt || isDead || isCastingSkill) return;

            if (SimpleInput.GetKeyDown(Key.Space) == false) return;

            bool wasGrounded = Controller2D.IsGrounded;
            bool jumpAccepted = Controller2D.RequestJump();

            if (jumpAccepted && wasGrounded == false)
            {
                Animator.SetTrigger(DOUBLE_JUMP);
                SpawnFx(doubleJumpFxPrefab);
            }
        }

        private void UpdateDashInput()
        {
            if (IsParrying || isHurt || isDead || isCastingSkill) return;

            if (SimpleInput.GetKeyDown(Key.A) == false) return;

            if (Time.time < nextDashTime) return;

            bool dashAccepted = Controller2D.RequestDash(FacingDirection, dashPower, dashDuration);

            if (dashAccepted == false) return;

            nextDashTime = Time.time + dashCooldown;
            Animator.SetTrigger(DASH);
            SpawnFx(dashFxPrefab);
        }

        private void UpdateCooldownUI()
        {
            if (Player.LocalPlayer == null) return;

            Player.LocalPlayer.UpdateParryCooldownUI(Mathf.Max(0.0f, nextParryTime - Time.time), parryCooldown);
        }

        private bool TrySprint(bool wantsSprint)
        {
            if (wantsSprint == false || Player.LocalPlayer == null)
                return false;

            float staminaCost = staminaCostPerSec * Time.deltaTime;
            staminaRecoveryStartTime = Time.time + staminaRecoveryDelay;

            if (Player.LocalPlayer.HasStamina(staminaCost) == false)
            {
                sprintInputGraceEndTime = 0.0f;
                SetSprintState(false);
                return false;
            }

            if (Player.LocalPlayer.TryUseStamina(staminaCost) == false)
            {
                sprintInputGraceEndTime = 0.0f;
                SetSprintState(false);
                return false;
            }

            return true;
        }

        private void UpdateStaminaRecovery()
        {
            if (isSprint || Player.LocalPlayer == null) return;

            if (Time.time < staminaRecoveryStartTime) return;

            Player.LocalPlayer.RecoverStamina(staminaRecoveryPerSec * Time.deltaTime);
        }

        private void SetSprintState(bool value, bool playEndFx = true)
        {
            if (isSprint == value)
            {
                Animator.SetBool(IS_SPRINT, value);
                return;
            }

            isSprint = value;
            Animator.SetBool(IS_SPRINT, value);

            if (value)
            {
                StartSprintLoopFx();
                return;
            }

            StopSprintLoopFx();

            if (playEndFx && Controller2D.IsGrounded)
                SpawnFx(sprintEndFxPrefab, sprintFxSpawnPoint);
        }

        private void StartSprintLoopFx()
        {
            if (sprintLoopFx != null || sprintLoopFxPrefab == null)
                return;

            Transform spawnPoint = sprintFxSpawnPoint != null ? sprintFxSpawnPoint : transform;
            sprintLoopFx = Instantiate(sprintLoopFxPrefab, spawnPoint.position, Quaternion.identity, spawnPoint);
        }

        private void StopSprintLoopFx()
        {
            if (sprintLoopFx == null) return;

            Destroy(sprintLoopFx);
            sprintLoopFx = null;
        }

        private void SpawnFx(GameObject prefab)
        {
            SpawnFx(prefab, fxSpawnPoint);
        }

        private void SpawnFx(GameObject prefab, Transform customSpawnPoint)
        {
            if (prefab == null) return;

            Transform spawnPoint = customSpawnPoint != null ? customSpawnPoint : transform;
            GameObject fx = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

            Vector3 scale = fx.transform.localScale;
            scale.x = Mathf.Abs(scale.x) * FacingDirection;
            fx.transform.localScale = scale;
        }

        private bool prevIsGrounded = false;

        private void UpdateGroundedAnimation()
        {
            bool isGrounded = Controller2D.IsGrounded;
            Animator.SetBool(IS_GROUNDED, isGrounded);

            if (isGrounded == false) StopSprintLoopFx();

            if (IsParrying || isHurt || isDead)
            {
                prevIsGrounded = isGrounded;
                return;
            }

            if (prevIsGrounded && isGrounded == false)
            {
                if (Controller2D.VerticalVelocity > 0.0f)
                    Animator.SetTrigger(JUMP);
                else 
                    Animator.SetTrigger(DESCENDING);
            }

            prevIsGrounded = isGrounded;
        }
    }
}