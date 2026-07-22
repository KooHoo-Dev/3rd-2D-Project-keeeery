using System.Collections;
using UnityEngine;

namespace Arcanist
{
    public class CardGaugeController : MonoBehaviour
    {
        private CardSystem cardSystem;
        private BuffController buffController;

        [Header("Gauge")]
        [SerializeField] private GameObject[] gaugeCells;
        [SerializeField] private float secondsPerCell = 1f;

        private int currentCell;
        private int overflowCells;
        private bool isGaugeFull;
        private Coroutine chargeCoroutine;

        public bool IsGaugeFull => isGaugeFull;

        private void Awake()
        {
            cardSystem = GetComponent<CardSystem>();
            buffController = GetComponent<BuffController>();
        }

        private void Start()
        {
            ResetGaugeOnly();
            StartCharging();
        }

        public void ResetGaugeAndRecharge()
        {
            overflowCells = 0;
            ResetGaugeOnly();
            StartCharging();
        }

        public void BeginNextGaugeCycle()
        {
            int carriedCells = overflowCells;
            overflowCells = 0;
            ResetGaugeOnly();
            currentCell = Mathf.Min(carriedCells, gaugeCells.Length);
            RefreshGaugeCells();
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
            while (currentCell < gaugeCells.Length)
            {
                float finalSecondsPerCell = secondsPerCell;

                if (buffController != null)
                    finalSecondsPerCell = buffController.ModifyCardGaugeChargeTime(secondsPerCell);

                yield return new WaitForSeconds(finalSecondsPerCell);

                if (currentCell < gaugeCells.Length && gaugeCells[currentCell] != null)
                    gaugeCells[currentCell].SetActive(true);

                currentCell++;
            }

            isGaugeFull = true;

            if (cardSystem != null)
                cardSystem.OnGaugeFull();
        }

        private void ResetGaugeOnly()
        {
            currentCell = 0;
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

            int totalCells = currentCell + amount;
            overflowCells = Mathf.Max(0, totalCells - gaugeCells.Length);
            currentCell = Mathf.Min(totalCells, gaugeCells.Length);
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

            isGaugeFull = true;

            if (cardSystem != null)
                cardSystem.OnGaugeFull();
        }
    }
}
