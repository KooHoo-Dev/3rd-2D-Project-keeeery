using UnityEngine;

namespace Arcanist
{
    // JumpState는 Animator의 AnyState(모든 상태) 기능 활용하여 구현

    // AnyState (모든 상태)
    // - 항상 표시(출력, 재생) 가능한 특수한 상태
    //   현재 존재하는 상태와 상관 없이 특정 상태로 이동하려는 상황에 사용
    // - 상태 머신(Animator)에 특수한 전환을 제어하는 방법
    // - AnyState는 반복적인 애니메이션 상태 제어에는 사용하지 않음
    //   e.g. Idle, Run 등 지속적으로 Loop되는 애니메이션에는 사용 안 함

    public class JumpState : PlayerAnimStateBase
    {
        public JumpState(PlayerController owner) : base(owner) { }

        public override void UpdateState(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.normalizedTime < INPUT_RESET_TIME)
                Animator.SetBool(PlayerController.IS_ATTACK, false);

            // 점프 상태에서도 입력을 허용해준다.
            Owner.HandleMovement();
        }
    }
}
