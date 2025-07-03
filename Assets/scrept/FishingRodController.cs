// ## REALISM UPDATE ##
// FishingRodController.cs

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FishingRodController : MonoBehaviour
{
    public enum FishingState { Idle, ChargingCast, Casting, WaitingForBite, FightingFish, ReelingInFish, ReelingInEmpty, LineSnapped, Cooldown }
    public FishingState CurrentState { get; private set; }

    #region Components & References
    [Header("Core Components")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] public Transform rodTipTransform;
    [SerializeField] private GameObject bobberPrefab;
    [SerializeField] private Transform bobberSpawnPoint;
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Transform catchZoneTransform;
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioSource tensionAudioSource;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI chargeAmountText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Slider fishHealthSlider;
    [SerializeField] private Slider tensionSlider;

    [Header("UI Colors & Gradients")]
    [SerializeField] private Gradient healthColorGradient;
    [SerializeField] private Color safeColor = new Color(0, 1, 0, 0.8f);
    [SerializeField] private Color warningColor = new Color(1, 1, 0, 0.8f);
    [SerializeField] private Color criticalColor = new Color(1, 0, 0, 0.8f);
    
    [Header("Effects & Audio Clips")]
    [SerializeField] public GameObject bobberSplashEffect;
    [SerializeField] private AudioClip bobberSplashSound;
    [SerializeField] private AudioClip lineTensionSound;
    [SerializeField] private AudioClip fishCaughtSound;
    #endregion

    #region Settings
    [Header("Water Settings")]
    [SerializeField] public float waterSurfaceY = 0f;

    [Header("Line Settings")]
    [SerializeField] public float maxLineLength = 30f;
    [SerializeField] private float lineVibrationFactor = 0.05f;

    [Header("Casting Settings")]
    [SerializeField] private float minCastForce = 15f;
    [SerializeField] private float maxCastForce = 70f;
    [SerializeField] private float castChargeRate = 40f;
    [SerializeField] private float castAngle = 45f;
    
    [Header("Reeling & Fight Settings")]
    [SerializeField] private float reelInSpeed = 20.0f;
    [SerializeField] private float reelInFinishDistance = 0.5f;
    [SerializeField] private float initialFishHealth = 100f;
    [SerializeField] private float reelPower = 25f;
    [SerializeField] private float maxTension = 100f;
    [SerializeField] private float reelTensionIncrease = 45f;
    [SerializeField] private float fishStruggleTension = 10f;
    [SerializeField] private float tensionReleaseRate = 35f;
    [SerializeField] private float fishReelSpeed = 10f;
    [SerializeField] private float cooldownDuration = 1.5f;
    [SerializeField] private float bobberFollowSpeed = 5f;
    #endregion

    #region Private Variables
    public Bobber bobberInstance { get; private set; }
    private FishAI hookedFish;
    private int fishCaughtCount = 0;
    private float cooldownTimer;
    private float currentCastCharge;
    private float currentFishHealth;
    private float currentTension;
    private Image healthFillImage;
    private Image tensionFillImage;
    private float splashTimer;
    #endregion

    #region Input
    private bool IsCastingInputStarted() => Keyboard.current.spaceKey.wasPressedThisFrame;
    private bool IsCastingInputReleased() => Keyboard.current.spaceKey.wasReleasedThisFrame;
    private bool IsReelingInputStarted() => Keyboard.current.eKey.wasPressedThisFrame;
    private bool IsReelingInputHeld() => Keyboard.current.eKey.isPressed;
    #endregion
    
    #region Unity Methods
    private void Awake()
    {
        if (mainAudioSource == null) mainAudioSource = GetComponent<AudioSource>();
        if (tensionAudioSource != null) tensionAudioSource.loop = true;
        if (fishHealthSlider != null) healthFillImage = fishHealthSlider.fillRect.GetComponentInChildren<Image>();
        if (tensionSlider != null) tensionFillImage = tensionSlider.fillRect.GetComponentInChildren<Image>();
        
        chargeAmountText?.gameObject.SetActive(false);
        notificationText?.gameObject.SetActive(false);
        fishHealthSlider?.gameObject.SetActive(false);
        tensionSlider?.gameObject.SetActive(false);
    }

    private void Start()
    {
        UpdateScoreUI();
        ChangeState(FishingState.Idle);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case FishingState.Idle: Update_Idle(); break;
            case FishingState.ChargingCast: Update_ChargingCast(); break;
            case FishingState.WaitingForBite: Update_WaitingForBite(); break;
            case FishingState.FightingFish: Update_FightingFish(); break;
            case FishingState.Cooldown: case FishingState.LineSnapped: Update_Cooldown(); break;
        }
    }

    private void LateUpdate()
    {
        if (bobberInstance != null && lineRenderer.enabled)
        {
            lineRenderer.SetPosition(0, rodTipTransform.position);
            Vector3 bobberPos = bobberInstance.transform.position;
            if(CurrentState == FishingState.FightingFish)
            {
                float tensionRatio = currentTension / maxTension;
                bobberPos += Random.insideUnitSphere * tensionRatio * lineVibrationFactor;
            }
            lineRenderer.SetPosition(1, bobberPos);
        }
    }
    #endregion
    
    #region State Machine & Gameplay
    private void ChangeState(FishingState newState)
    {
        if (CurrentState == newState) return;
        if (CurrentState == FishingState.FightingFish) { playerAnimator?.SetBool("IsFighting", false); tensionAudioSource?.Stop(); StartCoroutine(AnimateSliderOut(fishHealthSlider?.transform)); StartCoroutine(AnimateSliderOut(tensionSlider?.transform)); }
        CurrentState = newState;
        switch (CurrentState)
        {
            case FishingState.Idle: playerAnimator?.SetTrigger("GoIdle"); HideBobberAndLine(); break;
            case FishingState.ChargingCast: currentCastCharge = minCastForce; chargeAmountText?.gameObject.SetActive(true); playerAnimator?.SetBool("IsCharging", true); break;
            case FishingState.Casting: playerAnimator?.SetBool("IsCharging", false); playerAnimator?.SetTrigger("Cast"); chargeAmountText?.gameObject.SetActive(false); CastBobber(); break;
            case FishingState.FightingFish: currentFishHealth = initialFishHealth; currentTension = 20f; splashTimer = 0.5f; if (fishHealthSlider != null) { fishHealthSlider.gameObject.SetActive(true); fishHealthSlider.maxValue = initialFishHealth; StartCoroutine(AnimateSliderIn(fishHealthSlider.transform)); } if (tensionSlider != null) { tensionSlider.gameObject.SetActive(true); tensionSlider.maxValue = maxTension; StartCoroutine(AnimateSliderIn(tensionSlider.transform)); } if (tensionAudioSource != null && lineTensionSound != null) { tensionAudioSource.clip = lineTensionSound; tensionAudioSource.Play(); } UpdateFightUI(); playerAnimator?.SetBool("IsFighting", true); break;
            case FishingState.ReelingInFish: StartCoroutine(ReelFishToCatchZone()); break;
            case FishingState.ReelingInEmpty: StartCoroutine(ReelInEmptyBobberCoroutine()); break;
            case FishingState.LineSnapped: ShowNotification("Line Snapped!", 2f); hookedFish?.ResetFishState(); hookedFish = null; cooldownTimer = cooldownDuration; break;
            case FishingState.Cooldown: cooldownTimer = cooldownDuration; break;
        }
    }
    private void Update_Idle() { if (IsCastingInputStarted()) ChangeState(FishingState.ChargingCast); }
    private void Update_ChargingCast() { currentCastCharge = Mathf.Min(currentCastCharge + castChargeRate * Time.deltaTime, maxCastForce); if (chargeAmountText != null) chargeAmountText.text = $"Charge: {currentCastCharge:F0}"; if (IsCastingInputReleased()) ChangeState(FishingState.Casting); }
    private void Update_WaitingForBite() { if (IsReelingInputStarted()) ChangeState(FishingState.ReelingInEmpty); }
    private void Update_FightingFish() { if (bobberInstance != null && hookedFish != null) { bobberInstance.transform.position = Vector3.Lerp(bobberInstance.transform.position, hookedFish.transform.position, Time.deltaTime * bobberFollowSpeed); } currentTension += fishStruggleTension * Time.deltaTime; if (IsReelingInputHeld()) { currentTension += reelTensionIncrease * Time.deltaTime; currentFishHealth -= reelPower * Time.deltaTime; } else { currentTension -= tensionReleaseRate * Time.deltaTime; } currentTension = Mathf.Clamp(currentTension, 0, maxTension); currentFishHealth = Mathf.Clamp(currentFishHealth, 0, initialFishHealth); UpdateFightUI(); splashTimer -= Time.deltaTime; if (splashTimer <= 0f && currentTension > maxTension * 0.6f) { if(bobberInstance != null && bobberSplashEffect != null) Instantiate(bobberSplashEffect, bobberInstance.transform.position, Quaternion.identity); splashTimer = Random.Range(0.8f, 2.0f); } if (currentTension >= maxTension) ChangeState(FishingState.LineSnapped); else if (currentFishHealth <= 0f) ChangeState(FishingState.ReelingInFish); }
    private void Update_Cooldown() { cooldownTimer -= Time.deltaTime; if (cooldownTimer <= 0) ChangeState(FishingState.Idle); }

    public void OnBobberEnterWater() { if (CurrentState == FishingState.Casting) ChangeState(FishingState.WaitingForBite); if (mainAudioSource != null && bobberSplashSound != null) mainAudioSource.PlayOneShot(bobberSplashSound); }
    public void StartFishFight(FishAI fish) { if (CurrentState == FishingState.WaitingForBite) { hookedFish = fish; ChangeState(FishingState.FightingFish); } }
    public void CancelFishCatch() { hookedFish?.ResetFishState(); hookedFish = null; ChangeState(FishingState.Cooldown); }
    private void CastBobber() { if (bobberPrefab == null) return; if (bobberInstance != null) Destroy(bobberInstance.gameObject); var go = Instantiate(bobberPrefab, bobberSpawnPoint.position, Quaternion.identity); bobberInstance = go.GetComponent<Bobber>(); bobberInstance.Initialize(this, waterSurfaceY); if (bobberInstance.TryGetComponent<Rigidbody>(out var rb)) { rb.isKinematic = false; Vector3 dir = (bobberSpawnPoint.forward + Vector3.up * Mathf.Tan(castAngle * Mathf.Deg2Rad)).normalized; rb.AddForce(dir * currentCastCharge, ForceMode.VelocityChange); } lineRenderer.positionCount = 2; lineRenderer.enabled = true; }
    
    private IEnumerator ReelInEmptyBobberCoroutine()
    {
        if (bobberInstance == null || catchZoneTransform == null) { ChangeState(FishingState.Idle); yield break; }
        
        Transform bobberTransform = bobberInstance.transform;
        Rigidbody bobberRb = bobberInstance.GetRigidbody();
        if (bobberRb != null) bobberRb.isKinematic = true;

        while (bobberTransform != null && Vector3.Distance(bobberTransform.position, catchZoneTransform.position) > reelInFinishDistance)
        {
            bobberTransform.position = Vector3.MoveTowards(bobberTransform.position, catchZoneTransform.position, reelInSpeed * Time.deltaTime);
            yield return null;
        }
        ChangeState(FishingState.Idle);
    }
    
    private IEnumerator ReelFishToCatchZone() 
    { 
        if (catchZoneTransform == null) { ChangeState(FishingState.Cooldown); yield break; } 
        
        hookedFish?.DisableAI(true);
        if (bobberInstance != null) 
        { 
            bobberInstance.GetComponent<Collider>().enabled = false; 
            Rigidbody bobberRb = bobberInstance.GetRigidbody(); 
            if(bobberRb != null) bobberRb.isKinematic = true; 
            
            Transform bobberTransform = bobberInstance.transform;
            Transform fishTransform = hookedFish?.transform;

            while (bobberTransform != null && Vector3.Distance(bobberTransform.position, catchZoneTransform.position) > 0.1f) 
            { 
                bobberTransform.position = Vector3.MoveTowards(bobberTransform.position, catchZoneTransform.position, fishReelSpeed * Time.deltaTime); 
                if (fishTransform != null) { fishTransform.position = bobberTransform.position; } // Fish model follows bobber
                yield return null; 
            } 
        } 
        
        if (hookedFish != null) Destroy(hookedFish.gameObject); // Destroy the fish object after it's caught
        hookedFish = null;
        
        fishCaughtCount++; 
        UpdateScoreUI(); 
        ShowNotification("You Caught a Fish!", 2f, true); 
        if (mainAudioSource != null && fishCaughtSound != null) mainAudioSource.PlayOneShot(fishCaughtSound); 
        ChangeState(FishingState.Cooldown); 
    }
    
    public bool IsReelingMinigameActive() => CurrentState == FishingState.FightingFish;
    #endregion

    #region UI and Effects
    private void UpdateFightUI() { float tensionRatio = currentTension / maxTension; if (fishHealthSlider != null) { fishHealthSlider.value = currentFishHealth; if (healthFillImage != null) healthFillImage.color = healthColorGradient.Evaluate(currentFishHealth / initialFishHealth); } if (tensionSlider != null && tensionFillImage != null) { tensionSlider.value = currentTension; if (tensionRatio < 0.5f) tensionFillImage.color = safeColor; else if (tensionRatio < 0.85f) tensionFillImage.color = warningColor; else tensionFillImage.color = criticalColor; } if (tensionAudioSource != null) { tensionAudioSource.pitch = Mathf.Lerp(1.0f, 2.5f, tensionRatio); tensionAudioSource.volume = Mathf.Clamp01(tensionRatio * 1.2f); } }
    private void UpdateScoreUI() { if (scoreText != null) scoreText.text = $"Score: {fishCaughtCount}"; }
    private void ShowNotification(string message, float duration, bool animate = false) { StartCoroutine(NotificationCoroutine(message, duration, animate)); }
    private IEnumerator NotificationCoroutine(string message, float duration, bool animate) { if(notificationText != null) { notificationText.text = message; notificationText.gameObject.SetActive(true); if(animate) yield return AnimateSliderIn(notificationText.transform); } yield return new WaitForSeconds(duration); if(notificationText != null) notificationText.gameObject.SetActive(false); }
    public void HideBobberAndLine() { if (bobberInstance != null) Destroy(bobberInstance.gameObject); if (lineRenderer != null) lineRenderer.enabled = false; }
    private IEnumerator AnimateSliderIn(Transform uiElement) { if (uiElement == null) yield break; uiElement.gameObject.SetActive(true); uiElement.localScale = Vector3.zero; float timer = 0; float duration = 0.2f; while (timer < duration) { uiElement.localScale = Vector3.one * (timer / duration); timer += Time.deltaTime; yield return null; } uiElement.localScale = Vector3.one; }
    private IEnumerator AnimateSliderOut(Transform uiElement) { if (uiElement == null) yield break; Vector3 originalScale = uiElement.localScale; float timer = 0; float duration = 0.2f; while (timer < duration) { uiElement.localScale = originalScale * (1 - (timer / duration)); timer += Time.deltaTime; yield return null; } uiElement.gameObject.SetActive(false); }
    #endregion
}