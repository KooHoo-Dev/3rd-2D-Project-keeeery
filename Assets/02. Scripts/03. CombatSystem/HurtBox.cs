using UnityEngine;

namespace Arcanist
{
    [RequireComponent(typeof(Collider2D))]
    public class HurtBox : MonoBehaviour
    {
        [field: SerializeField] public CombatEntity Owner { get; private set; }
        [SerializeField] private GameObject hitFxPrefab;
        [SerializeField] private float hitFxLocalYOffset = 0.0f;
        [SerializeField] private bool playHitFx = true;

        private EnemyController enemyController;
        private Collider2D col2D;

        private void Awake()
        {
            Owner = GetComponent<CombatEntity>();
            if(Owner == null) Owner = GetComponentInParent<CombatEntity>();

            if (Owner == null)
            {
                Debug.LogError($"{name} : CombatEntity를 찾지 못했습니다.");
            }

            enemyController = GetComponentInParent<EnemyController>();
            col2D = GetComponent<Collider2D>();
        }
        
        private void OnEnable()
        {
            if (col2D == null)
                col2D = GetComponent<Collider2D>();

            if (CombatSystem.Instance == null)
                return;

            CombatSystem.Instance.AddHurtBox(col2D, this);
        }

        private void OnDisable()
        {
            if (CombatSystem.Instance == null || col2D == null)
                return;

            CombatSystem.Instance.RemoveHurtBox(col2D);
        }

        public void PlayHitFx(Vector3 position)
        {
            if (hitFxPrefab == null) return;
            if (enemyController.isDead) return;
            if (Owner == null) return;

            Quaternion rotation = Quaternion.Euler(0.0f, 0.0f, Random.Range(0.0f, 360.0f));
            Vector3 offset = new Vector3(0.0f, hitFxLocalYOffset, 0.0f);
            GameObject fx = Instantiate(hitFxPrefab, position + offset, rotation);

            float direction = Owner.transform.lossyScale.x >= 0.0f ? 1.0f : -1.0f;
            fx.transform.localScale = new Vector3(direction, 1.0f, 1.0f);
        }
    }
}
