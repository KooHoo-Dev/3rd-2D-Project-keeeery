namespace Arcanist
{
    public class GolemDeathState : BossBaseState
    {
        protected override void OnEnable()
        {
            if (BossController == null)
                return;

            BossController.StopMove();

            BossController.Animator.Play("Boss_Golem_Death");
        }
    }
}