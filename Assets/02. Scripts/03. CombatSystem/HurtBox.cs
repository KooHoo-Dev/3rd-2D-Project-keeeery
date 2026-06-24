using UnityEngine;

namespace Arcanist
{
    [RequireComponent(typeof(Collider2D))]
    public class HurtBox : MonoBehaviour
    {
        [field: SerializeField] public CombatEntity Owner { get; private set; }
        private Collider2D col2D;

        private void Awake()
        {
            Owner = GetComponent<CombatEntity>();
            if(Owner == null) Owner = GetComponentInParent<CombatEntity>();

            if (Owner == null)
            {
                Debug.LogError($"{name} : CombatEntity를 찾지 못했습니다.");
            }

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
    }
}
