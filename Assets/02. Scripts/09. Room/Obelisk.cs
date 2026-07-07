using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

namespace Arcanist
{
    public class Obelisk : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float breakAnimationDuration = 0.6f;

        [Header("Deck Edit")]
        [SerializeField] private bool canOpenDeckEdit = true;
        [SerializeField] private OverlayController overlayController;
        [SerializeField] private InventoryWindow inventoryWindow;

        private readonly HashSet<Collider2D> playerCollidersInside = new HashSet<Collider2D>();
        private Coroutine breakRoutine;
        private bool isPlayerInside;

        private static readonly int ACTIVE = Animator.StringToHash("Active");
        private static readonly int BREAK = Animator.StringToHash("Break");

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!canOpenDeckEdit || !isPlayerInside)
                return;

            if (IsOverlayOpen())
                return;

            if (SimpleInput.GetKeyDown(Key.DownArrow))
                TryOpenDeckEditInventory();
        }

        public void TryOpenDeckEditInventory()
        {
            if (!canOpenDeckEdit)
                return;

            ResolveInventoryReferences();

            if (overlayController == null)
                return;

            if (overlayController.CurrentState != OverlayController.OverlayState.None)
                return;

            if (inventoryWindow != null)
                inventoryWindow.SetDeckEditMode(true);

            overlayController.ShowInventory();
        }

        private bool IsOverlayOpen()
        {
            ResolveInventoryReferences();
            return overlayController != null && overlayController.CurrentState != OverlayController.OverlayState.None;
        }

        private void ResolveInventoryReferences()
        {
            if (overlayController == null)
                overlayController = FindFirstObjectByType<OverlayController>(FindObjectsInactive.Include);

            if (inventoryWindow == null)
                inventoryWindow = FindFirstObjectByType<InventoryWindow>(FindObjectsInactive.Include);
        }

        public void SetActiveState(bool active)
        {
            if (breakRoutine != null)
            {
                StopCoroutine(breakRoutine);
                breakRoutine = null;
            }

            if (animator == null)
                return;

            animator.ResetTrigger(BREAK);
            animator.SetBool(ACTIVE, active);
        }

        public void PlayBreak(Action onComplete)
        {
            if (breakRoutine != null)
                StopCoroutine(breakRoutine);

            breakRoutine = StartCoroutine(BreakRoutine(onComplete));
        }

        private IEnumerator BreakRoutine(Action onComplete)
        {
            if (animator != null)
                animator.SetTrigger(BREAK);

            if (breakAnimationDuration > 0.0f)
                yield return new WaitForSeconds(breakAnimationDuration);

            breakRoutine = null;
            onComplete?.Invoke();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerCollidersInside.Add(other);
            isPlayerInside = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsPlayerCollider(other))
                return;

            playerCollidersInside.Remove(other);
            isPlayerInside = playerCollidersInside.Count > 0;
        }

        private void OnDisable()
        {
            playerCollidersInside.Clear();
            isPlayerInside = false;
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            if (other.CompareTag("Player"))
                return true;

            return other.GetComponentInParent<PlayerController>() != null;
        }
    }
}
