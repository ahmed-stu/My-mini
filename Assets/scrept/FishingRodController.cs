// FishingRodController.cs

using UnityEngine;
using UnityEngine.InputSystem;   // للاستفادة من Input System الجديد
using System.Collections.Generic;

/// <summary>
/// يتحكم بكامل منطق سنارة الصيد بما في ذلك الرمي، الفيزياء، والآلة الحالة.
/// </summary>
public class FishingRodController : MonoBehaviour
{
    #region References

    [Header("References")]
    [Tooltip("LineRenderer لعرض خيط السنارة")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("Transform لنقطة خروج السنارة من الصنارة")]
    [SerializeField] private Transform rodTipTransform;
    [Tooltip("Prefab للعوامة")]
    [SerializeField] private GameObject bobberPrefab;
    [Tooltip("نقطة ظهور العوامة عند الرمي")]
    [SerializeField] private Transform bobberSpawnPoint;
    [Tooltip("Animator للاعب أو السنارة")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("منطق اللعبة المصغرة للسحب")]
    [SerializeField] private ReelingMinigameLogic reelingMinigameLogic;

    [Header("Layers")]
    [Tooltip("طبقة الماء لكشف العوامة")]
    [SerializeField] private LayerMask waterLayer;
    [Tooltip("طبقة الأسماك (اختياري)")]
    [SerializeField] private LayerMask fishLayer;

    #endregion

    #region Cast Settings

    [Header("Cast Settings")]
    [Tooltip("قوة رمي العوامة")]
    [SerializeField] private float castForce = 10f;
    [Tooltip("زاوية الرمي بالدرجات")]
    [SerializeField] private float castAngle = 45f;

    #endregion

    #region Line Physics Settings

    [Header("Line Physics Settings")]
    [Tooltip("عدد نقاط فيزياء الخيط")]
    [SerializeField] private int numberOfParticles = 30;
    [Tooltip("الطول الأساسي لكل مقطع من الخيط")]
    [SerializeField] private float baseSegmentLength = 0.1f;
    [Tooltip("الحد الأدنى لطول مقطع الخيط")]
    [SerializeField] private float minSegmentLength = 0.05f;
    [Tooltip("الحد الأقصى لطول مقطع الخيط")]
    [SerializeField] private float maxSegmentLength = 0.2f;
    [Tooltip("سرعة تعديل طول الخيط أثناء السحب أو الإطالة")]
    [SerializeField] private float lineAdjustSpeed = 0.01f;
    [Tooltip("عدد تكرارات قيود الفيزياء في كل FixedUpdate")]
    [SerializeField] private int iterationsPerFrame = 8;
    [Tooltip("تسارع الجاذبية المطبق على الخيط")]
    [SerializeField] private Vector3 gravity = new Vector3(0, -9.81f, 0);

    #endregion

    #region State Machine

    private StateMachine<FishingRodBaseState> stateMachine;

    public IdleState           idleState;
    public CastingState        castingState;
    public WaitingForBiteState waitingForBiteState;
    public ReelingState        reelingState;
    public FishCaughtState     fishCaughtState;
    public LineSnappedState    lineSnappedState;

    #endregion

    #region Internal Line Data

    private class LineParticle
    {
        public Vector3 Pos;
        public Vector3 OldPos;
        public Vector3 Acceleration;
    }

    private LineParticle[] lineParticles;
    private Vector3[]      lineRendererPositions;
    private float          currentSegmentLength;

    #endregion

    #region Bobber

    /// <summary>العوّامة الحالية في المشهد</summary>
    public Bobber bobberInstance;

    #endregion

    #region Input Queries

    // نستخدم المفاتيح مباشرة من Input System:
    // Space للرمي، E للسحب، Q للتمديد
    public bool IsCastingInputPressed()   => Keyboard.current.spaceKey.wasPressedThisFrame;
    public bool IsReelingInputHeld()      => Keyboard.current.eKey.isPressed;
    public bool IsExtendingInputHeld()    => Keyboard.current.qKey.isPressed;

    #endregion

    private void Awake()
    {
        // تهيئة الآلة الحالة وحالاتها
        stateMachine        = new StateMachine<FishingRodBaseState>();
        idleState           = new IdleState(this, stateMachine, playerAnimator);
        castingState        = new CastingState(this, stateMachine, playerAnimator);
        waitingForBiteState = new WaitingForBiteState(this, stateMachine, playerAnimator);
        reelingState        = new ReelingState(this, stateMachine, playerAnimator);
        fishCaughtState     = new FishCaughtState(this, stateMachine, playerAnimator);
        lineSnappedState    = new LineSnappedState(this, stateMachine, playerAnimator);

        // تهيئة بيانات الفيزياء للخيط
        lineParticles         = new LineParticle[numberOfParticles];
        lineRendererPositions = new Vector3[numberOfParticles];
        for (int i = 0; i < numberOfParticles; i++)
            lineParticles[i] = new LineParticle();

        currentSegmentLength = baseSegmentLength;

        // التأكد من الربط في الـ Inspector أو اجلب المكونات تلقائيًا
        lineRenderer       ??= GetComponent<LineRenderer>();
        playerAnimator     ??= GetComponent<Animator>();
        reelingMinigameLogic ??= FindAnyObjectByType<ReelingMinigameLogic>();
    }

    private void Start()
    {
        stateMachine.Initialize(idleState);
        HideBobberAndLine();
    }

    private void Update()
    {
        // 1. رمي السنارة (Switch to Casting)
        if (IsCastingInputPressed() && stateMachine.CurrentState == idleState)
            stateMachine.ChangeState(castingState);

        // 2. تحديث المنطق للحالة الحالية
        stateMachine.CurrentState.LogicUpdate();

        // 3. تعديل طول الخيط في الخمول أو انتظار اللدغة
        if (stateMachine.CurrentState == idleState ||
            stateMachine.CurrentState == waitingForBiteState)
        {
            if (IsExtendingInputHeld())
                AdjustLineLength(lineAdjustSpeed * Time.deltaTime);
            else if (IsReelingInputHeld())
                AdjustLineLength(-lineAdjustSpeed * Time.deltaTime);
        }
    }

    private void FixedUpdate()
    {
        stateMachine.CurrentState.PhysicsUpdate();
        UpdateLinePhysics();
    }

    #region Casting Logic

    public void CastBobber()
    {
        if (stateMachine.CurrentState != castingState)
            return;

        // تفعيل الرسم
        lineRenderer.enabled = true;

        // إزالة العوامة القديمة
        if (bobberInstance != null)
            Destroy(bobberInstance.gameObject);

        if (bobberPrefab == null || bobberSpawnPoint == null)
        {
            Debug.LogError("Bobber prefab or spawn point not assigned!");
            return;
        }

        // إنشاء العوامة
        var go = Instantiate(bobberPrefab, bobberSpawnPoint.position, Quaternion.identity);
        bobberInstance = go.GetComponent<Bobber>();
        bobberInstance?.Initialize(this, waterLayer, fishLayer);

        // تطبيق رمية الفيزياء
        if (bobberInstance.TryGetComponent<Rigidbody>(out var rb))
        {
            Vector3 dir = (bobberSpawnPoint.forward +
                          Vector3.up * Mathf.Tan(castAngle * Mathf.Deg2Rad)).normalized;
            rb.AddForce(dir * castForce, ForceMode.VelocityChange);
        }

        // تهيئة نقاط الفيزياء
        for (int i = 0; i < numberOfParticles; i++)
        {
            lineParticles[i].Pos         = rodTipTransform.position;
            lineParticles[i].OldPos      = rodTipTransform.position;
            lineParticles[i].Acceleration = Vector3.zero;
        }

        lineRenderer.positionCount = numberOfParticles;
    }

    #endregion

    #region Line Physics (Verlet)

    private void UpdateLinePhysics()
    {
        if (bobberInstance == null || !lineRenderer.enabled)
            return;

        // تثبيت الأطراف
        lineParticles[0].Pos = lineParticles[0].OldPos = rodTipTransform.position;
        lineParticles[^1].Pos = bobberInstance.transform.position;

        // Verlet
        for (int i = 1; i < numberOfParticles - 1; i++)
        {
            lineParticles[i].Acceleration = gravity;
            Verlet(lineParticles[i], Time.fixedDeltaTime);
        }

        // قيود الطول
        for (int iter = 0; iter < iterationsPerFrame; iter++)
            for (int i = 0; i < numberOfParticles - 1; i++)
                PoleConstraint(lineParticles[i], lineParticles[i + 1], currentSegmentLength);

        // رسم الخيط
        for (int i = 0; i < numberOfParticles; i++)
            lineRendererPositions[i] = lineParticles[i].Pos;
        lineRenderer.SetPositions(lineRendererPositions);
    }

    private void Verlet(LineParticle p, float dt)
    {
        Vector3 temp = p.Pos;
        p.Pos += (p.Pos - p.OldPos) + p.Acceleration * dt * dt;
        p.OldPos = temp;
    }

    private void PoleConstraint(LineParticle p1, LineParticle p2, float restLength)
    {
        Vector3 delta = p2.Pos - p1.Pos;
        float dist    = delta.magnitude;
        float diff    = (dist - restLength) / dist;
        p1.Pos += delta * diff * 0.5f;
        p2.Pos -= delta * diff * 0.5f;
    }

    #endregion

    #region Line Length Adjustment

    private void AdjustLineLength(float amount)
    {
        currentSegmentLength = Mathf.Clamp(currentSegmentLength + amount, minSegmentLength, maxSegmentLength);
    }

    #endregion

    #region Reeling Minigame Integration

    public void StartReelingMinigame()       => reelingMinigameLogic?.StartMinigame();
    public void ReelIn(float dt)             => reelingMinigameLogic?.ApplyReelInput(dt);
    public void ReleaseReel(float dt)        => reelingMinigameLogic?.ReleaseReelInput(dt);
    public void StopReelingMinigame()        => reelingMinigameLogic?.StopMinigame();

    #endregion

    #region UI Methods

    public void DisplayFishCaughtUI()   => Debug.Log("UI: Fish Caught!");
    public void HideFishCaughtUI()      => Debug.Log("UI: Hide Fish Caught UI");
    public void DisplayLineSnappedUI()  => Debug.Log("UI: Line Snapped!");
    public void HideLineSnappedUI()     => Debug.Log("UI: Hide Line Snapped UI");

    #endregion

    #region Cleanup

    public void HideBobberAndLine()
    {
        if (bobberInstance != null)
        {
            Destroy(bobberInstance.gameObject);
            bobberInstance = null;
        }
        if (lineRenderer != null)
            lineRenderer.enabled = false;
    }

    #endregion
}
