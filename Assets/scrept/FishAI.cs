// ## REALISM UPDATE ##
// FishAI.cs

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class FishAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float movementSmoothing = 0.5f;

    [Header("Wander Settings")]
    [SerializeField] private Vector3 wanderAreaCenter = Vector3.zero;
    [SerializeField] private Vector3 wanderAreaSize = new Vector3(30, 8, 30);
    [SerializeField] private float waterSurfaceY = 0f;

    [Header("Bait Interaction")]
    [SerializeField] private float baitDetectionRadius = 8f;
    [SerializeField] private float minDistanceToRod = 2.0f;
    [SerializeField] private float biteDistance = 1.0f;
    [SerializeField] private float timeBeforeBite = 1.0f;
    [SerializeField] private GameObject biteIndicatorPrefab;

    [Header("Fight Settings")]
    [SerializeField] private float fishFightPullForce = 20f;
    
    private Vector3 targetPosition;
    private Rigidbody rb;
    private FishingRodController fishingRodController;
    private enum FishState { Wandering, FollowingBait, Biting, Fighting, Cooldown, Disabled }
    private FishState currentState;
    private Vector3 velocityRef = Vector3.zero;
    private Vector3 wanderTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        wanderAreaSize = new Vector3(Mathf.Abs(wanderAreaSize.x), Mathf.Abs(wanderAreaSize.y), Mathf.Abs(wanderAreaSize.z));
    }

    private void Start()
    {
        fishingRodController = FindObjectOfType<FishingRodController>();
        if (fishingRodController == null) { enabled = false; return; }
        SetState(FishState.Wandering);
    }
    
    private void FixedUpdate()
    {
        if (currentState == FishState.Fighting)
        {
            FightBehavior();
        }
        else if (currentState != FishState.Disabled && currentState != FishState.Biting)
        {
            UpdateTargetPosition();
            ApplyStableMovement();
        }
        
        ClampPositionToWater();
    }
    
    private void SetState(FishState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        switch (currentState)
        {
            case FishState.Wandering: 
                DisableAI(false); // Make sure AI is enabled when wandering
                SetNewWanderTarget(); 
                break;
            case FishState.Biting: 
                rb.linearVelocity = Vector3.zero; 
                StartCoroutine(BiteCoroutine()); 
                break;
            case FishState.Cooldown: 
                StartCoroutine(CooldownCoroutine()); 
                break;
            case FishState.Disabled:
                rb.isKinematic = true;
                break;
        }
    }

    private void UpdateTargetPosition()
    {
        bool isBobberReady = fishingRodController.CurrentState == FishingRodController.FishingState.WaitingForBite;

        if (currentState == FishState.Wandering && isBobberReady && fishingRodController.bobberInstance != null)
        {
            Transform bobberTransform = fishingRodController.bobberInstance.transform;
            float distanceToBobber = Vector3.Distance(transform.position, bobberTransform.position);
            float distanceOfBobberFromRod = Vector3.Distance(bobberTransform.position, fishingRodController.rodTipTransform.position);
            if (distanceToBobber <= baitDetectionRadius && distanceOfBobberFromRod > minDistanceToRod)
            {
                SetState(FishState.FollowingBait);
            }
        }
        
        if (currentState == FishState.FollowingBait)
        {
             if (!isBobberReady || fishingRodController.bobberInstance == null) { SetState(FishState.Wandering); return; }
             targetPosition = fishingRodController.bobberInstance.transform.position;
             if (Vector3.Distance(transform.position, targetPosition) < biteDistance) SetState(FishState.Biting);
        }
        else { targetPosition = wanderTarget; if (Vector3.Distance(transform.position, wanderTarget) < 2.5f) SetNewWanderTarget(); }
    }

    private void ApplyStableMovement()
    {
        Vector3 clampedTarget = new Vector3(targetPosition.x, Mathf.Min(targetPosition.y, waterSurfaceY - 0.5f), targetPosition.z);
        rb.position = Vector3.SmoothDamp(rb.position, clampedTarget, ref velocityRef, movementSmoothing, moveSpeed);
        if (velocityRef.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocityRef);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }
    }

    private void ClampPositionToWater() { if (transform.position.y > waterSurfaceY) { transform.position = new Vector3(transform.position.x, waterSurfaceY, transform.position.z); } }
    private void SetNewWanderTarget() { float x = Random.Range(-wanderAreaSize.x / 2, wanderAreaSize.x / 2) + wanderAreaCenter.x; float y = Random.Range(waterSurfaceY - wanderAreaSize.y, waterSurfaceY - 1f); float z = Random.Range(-wanderAreaSize.z / 2, wanderAreaSize.z / 2) + wanderAreaCenter.z; wanderTarget = new Vector3(x, y, z); }
    private IEnumerator BiteCoroutine() { GameObject indicator = null; if (biteIndicatorPrefab != null) indicator = Instantiate(biteIndicatorPrefab, transform.position + Vector3.up * 1.5f, Quaternion.identity); yield return new WaitForSeconds(timeBeforeBite); if (indicator != null) Destroy(indicator); if(fishingRodController.CurrentState == FishingRodController.FishingState.WaitingForBite) { fishingRodController.StartFishFight(this); SetState(FishState.Fighting); } else { SetState(FishState.Wandering); } }
    
    private void FightBehavior() 
    {
        if (fishingRodController == null) return;
        Transform rodTip = fishingRodController.rodTipTransform;
        float maxDistance = fishingRodController.maxLineLength;
        float distanceToRod = Vector3.Distance(transform.position, rodTip.position);

        if (distanceToRod < maxDistance)
        {
            Vector3 fromRodToFish = transform.position - rodTip.position;
            fromRodToFish.y = 0;
            Vector3 resistanceDirection = (fromRodToFish.normalized + (Vector3.down * 0.2f)).normalized;
            rb.AddForce(resistanceDirection * fishFightPullForce, ForceMode.Acceleration);
        }
        
        if (distanceToRod > maxDistance)
        {
            Vector3 direction = (transform.position - rodTip.position).normalized;
            transform.position = rodTip.position + direction * maxDistance;
            rb.linearVelocity *= 0.5f;
        }

        if (rb.linearVelocity.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(rb.linearVelocity.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private IEnumerator CooldownCoroutine() { yield return new WaitForSeconds(3f); SetState(FishState.Wandering); }
    public void ResetFishState() { StopAllCoroutines(); rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; SetState(FishState.Wandering); }
    
    public void DisableAI(bool isDisabled)
    {
        if(isDisabled)
        {
            SetState(FishState.Disabled);
        }
        else
        {
            rb.isKinematic = false;
            SetState(FishState.Wandering);
        }
    }
}