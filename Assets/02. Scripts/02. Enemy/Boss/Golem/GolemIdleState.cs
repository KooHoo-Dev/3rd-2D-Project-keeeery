using UnityEngine;

namespace Arcanist
{
    public class GolemIdleState : BossBaseState
    {
        [SerializeField] private float waitTime = 1.2f;
        [SerializeField] private float meleeRange = 3.0f;

        private float timer;
        protected override void OnEnable()
        {
            if (BossController == null) return;

            timer = 0.0f;
            BossController.StopMove();
            BossController.Animator.Play("Boss_Golem_Idle");

            Transform player = BossController.Player;

            if (player != null) BossController.LookAt(player.position);
        }

        private void Update()
        {
            if (BossController == null) return;

            Transform player = BossController.Player;

            if (player != null) BossController.LookAt(player.position);

            timer += Time.deltaTime;

            if (timer < waitTime) return;

            SelectPattern();
        }

        private void SelectPattern()
        {
            float distance = Mathf.Abs(
                BossController.Player.position.x - BossController.transform.position.x
            );

            if (distance <= meleeRange)
            {
                BossController.ChangeState(typeof(GolemMeleeState));
                return;
            }

            int random = Random.Range(0, 2);

            if (random == 0)
                BossController.ChangeState(typeof(GolemLaserState));
            else
                BossController.ChangeState(typeof(GolemShootState));
        }
    }
}