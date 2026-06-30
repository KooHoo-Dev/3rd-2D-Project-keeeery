using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class CardGaugeController : MonoBehaviour
    {
        private CardSystem cardSystem;

        [Header("Gauge")]
        [SerializeField] private GameObject[] gaugeCells;
        [SerializeField] private float secondsPerCell = 1f;

        private int currentCell;
        private bool isCharging;
        private bool isGaugeFull;
        private Coroutine chargeCoroutine;

        public bool IsGaugeFull => isGaugeFull;

        private void Awake()
        {
            if (cardSystem == null)
                cardSystem = GetComponent<CardSystem>();
        }

        private void Start()
        {
            ResetGaugeOnly();
            StartCharging();
        }

        public void ResetGaugeAndRecharge()
        {
            ResetGaugeOnly();
            StartCharging();
        }

        public void SetSecondsPerCell(float value)
        {
            secondsPerCell = Mathf.Max(0.01f, value);
        }

        private void StartCharging()
        {
            if (chargeCoroutine != null)
                StopCoroutine(chargeCoroutine);

            chargeCoroutine = StartCoroutine(ChargeLoop());
        }

        private IEnumerator ChargeLoop()
        {
            isCharging = true;

            while (currentCell < gaugeCells.Length)
            {
                yield return new WaitForSeconds(secondsPerCell);

                if (currentCell < gaugeCells.Length && gaugeCells[currentCell] != null)
                    gaugeCells[currentCell].SetActive(true);

                currentCell++;
            }

            isCharging = false;
            isGaugeFull = true;

            if (cardSystem != null)
                cardSystem.OnGaugeFull();
        }

        private void ResetGaugeOnly()
        {
            currentCell = 0;
            isCharging = false;
            isGaugeFull = false;

            foreach (GameObject cell in gaugeCells)
            {
                if (cell != null)
                    cell.SetActive(false);
            }
        }

        public void AddCells(int amount)
        {
            if (amount <= 0 || isGaugeFull) return;

            currentCell = Mathf.Min(currentCell + amount, gaugeCells.Length);
            RefreshGaugeCells();

            if (currentCell >= gaugeCells.Length)
            {
                CompleteGauge();
                return;
            }

            StartCharging();
        }

        private void RefreshGaugeCells()
        {
            for (int i = 0; i < gaugeCells.Length; i++)
            {
                if (gaugeCells[i] != null)
                    gaugeCells[i].SetActive(i < currentCell);
            }
        }

        private void CompleteGauge()
        {
            if (chargeCoroutine != null)
            {
                StopCoroutine(chargeCoroutine);
                chargeCoroutine = null;
            }

            currentCell = gaugeCells.Length;
            RefreshGaugeCells();

            isCharging = false;
            isGaugeFull = true;

            cardSystem.OnGaugeFull();
        }
    }
}
