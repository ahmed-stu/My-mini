// FishingRodController.cs

using UnityEngine;
using UnityEngine.InputSystem;   // للاستفادة من Input System الجديد
using System.Collections.Generic;

/// <summary>
/// يتحكم بكامل منطق سنارة الصيد بما في ذلك الرمي، الفيزياء، والآلة الحالة.
/// </summary>
public class FishingRodController : MonoBehaviour
{
    #region References

   
    [Header("References")]
    [Tooltip("Line Renderer لعرض خيط السنارة")]
    [SerializeField] private LineRenderer lineRenderer;
    [Tooltip("Transform يمثل طرف السنارة")]
    [SerializeField] private Transform rodTipTransform;
    [Tooltip("Prefab للعوامة")]
    [SerializeField] private GameObject bobberPrefab;
    [Tooltip("نقطة انبثاق العوامة عند الرمي")]
    [SerializeField] private Transform bobberSpawnPoint;
    [Tooltip("Animator للاعب أو السنارة")]
    [SerializeField] private Animator playerAnimator;
    [Tooltip("منطق اللعبة المصغرة للسحب")]
    [SerializeField] private ReelingMinigameLogic reelingMinigameLogic;
    #endregion

    #region Layers
    [Header("Layers")]
    [Tooltip("طبقة الماء لكشف العوامة")]
    [SerializeField] private LayerMask waterLayer;
    [Tooltip("طبقة الأسماك للكشف عنها")]
    [SerializeField] private LayerMask fishLayer;
    #endregion

    #region Cast Settings
    [Header("Cast Settings")]
    [Tooltip("قوة رمي العوامة")]
    [SerializeField] private float castForce = 50f;
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
    [SerializeField] private float lineAdjustSpeed = 0.53f;
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
    private class LineParticle { public Vector3 Pos, OldPos, Acceleration; }
    private LineParticle[] lineParticles;
    private Vector3[] lineRendererPositions;
    private float currentSegmentLength;
    #endregion

    #region Bobber

    /// <summary>العوّامة الحالية في المشهد.</summary>
    public Bobber bobberInstance;

    #endregion

    #region Input Queries

    // نستخدم المفاتيح مباشرة من Input System:
    // Space للرمي، E للسحب، Q للتمديد [14, 15, 16, 17]
    public bool IsCastingInputPressed()  => Keyboard.current.spaceKey.wasPressedThisFrame;
    public bool IsReelingInputHeld()     => Keyboard.current.eKey.isPressed;
    public bool IsExtendingInputHeld()   => Keyboard.current.qKey.isPressed;

    #endregion

    private void Awake()
    {
        // تهيئة الآلة الحالة وحالاتها
        stateMachine            = new StateMachine<FishingRodBaseState>();
        idleState               = new IdleState(this, stateMachine, playerAnimator);
        castingState            = new CastingState(this, stateMachine, playerAnimator);
        waitingForBiteState     = new WaitingForBiteState(this, stateMachine, playerAnimator);
        reelingState            = new ReelingState(this, stateMachine, playerAnimator);
        fishCaughtState         = new FishCaughtState(this, stateMachine, playerAnimator);
        lineSnappedState        = new LineSnappedState(this, stateMachine, playerAnimator);

        // تهيئة بيانات الفيزياء للخيط [11, 12, 13]
        lineParticles           = new LineParticle[numberOfParticles];
        lineRendererPositions   = new Vector3[numberOfParticles];
        for (int i = 0; i < numberOfParticles; i++)
            lineParticles[i] = new LineParticle();

        currentSegmentLength = baseSegmentLength;

        // التأكد من الربط في الـ Inspector أو اجلب المكونات تلقائيًا
        lineRenderer         ??= GetComponent<LineRenderer>();
        playerAnimator       ??= GetComponent<Animator>();
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

    /// <summary>
    /// يقوم برمي العوامة وتفعيل فيزياء الخيط.
    /// </summary>
    public void CastBobber()
    {
        if (stateMachine.CurrentState!= castingState)
            return;

        Debug.Log(" CastBobber called. Enabling Line Renderer."); //
        lineRenderer.enabled = true; // [18]

        // إزالة العوامة القديمة إذا كانت موجودة
        if (bobberInstance!= null)
        {
            Debug.Log(" Destroying old bobber instance: " + bobberInstance.name);
            Destroy(bobberInstance.gameObject);
        }

        // التحقق من تعيين Prefab ونقطة الظهور
        if (bobberPrefab == null || bobberSpawnPoint == null)
        {
            Debug.LogError(" Bobber prefab or spawn point not assigned in Inspector!");
            return;
        }

        Debug.Log(" Attempting to instantiate bobber prefab: " + bobberPrefab.name + " at spawn point: " + bobberSpawnPoint.position);

        // إنشاء العوامة
        var go = Instantiate(bobberPrefab, bobberSpawnPoint.position, Quaternion.identity);

        // --- نقاط التحقق الحاسمة ---
        if (go == null)
        {
            Debug.LogError(" Failed to instantiate bobber GameObject!");
            return;
        }
        Debug.Log(" Bobber GameObject instantiated: " + go.name);

        // محاولة الحصول على مكون Bobber.cs من العوامة المنشأة
        bobberInstance = go.GetComponent<Bobber>();
        if (bobberInstance == null)
        {
            Debug.LogError(" Instantiated bobber does not have a Bobber.cs component! Please add Bobber.cs to your bobber prefab.");
            Destroy(go); // تدمير الكائن إذا لم يكن لديه المكون الصحيح
            return;
        }
        Debug.Log(" Bobber.cs component found on instantiated bobber.");

        bobberInstance?.Initialize(this, waterLayer, fishLayer);

        // تطبيق رمية الفيزياء على Rigidbody الخاص بالعوامة [19, 1]
        if (bobberInstance.TryGetComponent<Rigidbody>(out var rb))
        {
            // *** التعديل هنا: تفعيل الفيزياء فورًا بعد الرمي ***
            rb.isKinematic = false; // 
            Debug.Log(" Bobber has Rigidbody. Mass: " + rb.mass + ", IsKinematic: " + rb.isKinematic + ", UseGravity: " + rb.useGravity); // 
            Vector3 dir = (bobberSpawnPoint.forward +
                           Vector3.up * Mathf.Tan(castAngle * Mathf.Deg2Rad)).normalized;
            rb.AddForce(dir * castForce, ForceMode.VelocityChange); // ForceMode.VelocityChange لرمي فوري 
            Debug.Log(" Applied force: " + (dir * castForce) + " with ForceMode.VelocityChange.");
        }
        else
        {
            Debug.LogError(" Bobber Prefab is missing Rigidbody component! Please add Rigidbody to your bobber prefab."); //
        }

        // تهيئة نقاط الفيزياء للخيط [11, 12, 13]
        for (int i = 0; i < numberOfParticles; i++)
        {
            lineParticles[i].Pos          = rodTipTransform.position;
            lineParticles[i].OldPos       = rodTipTransform.position;
            lineParticles[i].Acceleration = Vector3.zero;
        }

        lineRenderer.positionCount = numberOfParticles; // 
        Debug.Log(" Line Renderer position count set to: " + numberOfParticles);
        Debug.Log(" CastBobber function finished.");
    }

    #endregion

    #region Line Physics (Verlet)

    /// <summary>
    /// يقوم بتحديث فيزياء الخيط باستخدام تكامل فيرليت ورسمه باستخدام Line Renderer.
    /// </summary>
    private void UpdateLinePhysics()
    {
        if (bobberInstance == null ||!lineRenderer.enabled)
            return;

        // تثبيت الأطراف: العقدة الأولى بطرف الصنارة، والعقدة الأخيرة بالعوامة [11, 12, 13]
        lineParticles[0].Pos = lineParticles[0].OldPos = rodTipTransform.position;
        lineParticles[numberOfParticles - 1].Pos = bobberInstance.transform.position; // [11, 12, 13]

        // تطبيق فيزياء فيرليت على جميع العقد 
        for (int i = 1; i < numberOfParticles - 1; i++)
        {
            lineParticles[i].Acceleration = gravity;
            Verlet(lineParticles[i], Time.fixedDeltaTime); // 
        }

        // تطبيق قيود الطول لعدة تكرارات لضمان صلابة الخيط [11, 12, 13]
        for (int iter = 0; iter < iterationsPerFrame; iter++)
            for (int i = 0; i < numberOfParticles - 1; i++)
                PoleConstraint(lineParticles[i], lineParticles[i + 1], currentSegmentLength); // [11, 12, 13]

        // رسم الخيط باستخدام Line Renderer 
        for (int i = 0; i < numberOfParticles; i++)
            lineRendererPositions[i] = lineParticles[i].Pos;
        lineRenderer.SetPositions(lineRendererPositions); // 
    }

    /// <summary>
    /// دالة تكامل فيرليت لحساب الموضع الجديد للجسيم. 
    /// </summary>
    private void Verlet(LineParticle p, float dt)
    {
        Vector3 temp = p.Pos;
        p.Pos += (p.Pos - p.OldPos) + p.Acceleration * dt * dt;
        p.OldPos = temp;
    }

    /// <summary>
    /// دالة تطبيق قيد المسافة بين جسيمين متجاورين. [11, 12, 13]
    /// </summary>
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

    /// <summary>
    /// تعديل طول الخيط ديناميكيًا.
    /// </summary>
    /// <param name="amount">المقدار الذي يجب تعديل الطول به.</param>
    private void AdjustLineLength(float amount)
    {
        currentSegmentLength = Mathf.Clamp(currentSegmentLength + amount, minSegmentLength, maxSegmentLength);
    }

    #endregion

    #region Reeling Minigame Integration

    // دمج مع منطق اللعبة المصغرة للسحب 
    public void StartReelingMinigame()      => reelingMinigameLogic?.StartMinigame();
    public void ReelIn(float dt)            => reelingMinigameLogic?.ApplyReelInput(dt);
    public void ReleaseReel(float dt)       => reelingMinigameLogic?.ReleaseReelInput(dt);
    public void StopReelingMinigame()       => reelingMinigameLogic?.StopMinigame();

    #endregion

    #region UI Methods

    // طرق لعرض وإخفاء عناصر واجهة المستخدم [20]
    public void DisplayFishCaughtUI()   => Debug.Log("UI: Fish Caught!");
    public void HideFishCaughtUI()      => Debug.Log("UI: Hide Fish Caught UI");
    public void DisplayLineSnappedUI()  => Debug.Log("UI: Line Snapped!");
    public void HideLineSnappedUI()     => Debug.Log("UI: Hide Line Snapped UI");

    #endregion

    #region Cleanup

    /// <summary>
    /// يخفي العوامة والخيط عند الحاجة (مثلاً عند سحب السنارة).
    /// </summary>
    public void HideBobberAndLine()
    {
        if (bobberInstance!= null)
        {
            Destroy(bobberInstance.gameObject);
            bobberInstance = null;
        }
        if (lineRenderer!= null)
            lineRenderer.enabled = false;
    }

    #endregion
}