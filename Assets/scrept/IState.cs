// IState.cs

// واجهة (Interface) تمثل الحالة في نظام الحالة FSM
public interface IState
{
    // يتم استدعاؤه عند الدخول إلى الحالة
    void Enter();

    // يتم استدعاؤه عند الخروج من الحالة
    void Exit();

    // التحديث المنطقي (يتم استدعاؤه كل إطار في Update)
    void LogicUpdate();

    // التحديث الفيزيائي (يتم استدعاؤه في FixedUpdate)
    void PhysicsUpdate();
}
