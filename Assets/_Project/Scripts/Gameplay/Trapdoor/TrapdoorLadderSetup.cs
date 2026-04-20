using UnityEngine;

[DisallowMultipleComponent]
public sealed class TrapdoorLadderSetup : MonoBehaviour
{
    [Header("Teleport Points")]
    [SerializeField] private Transform topDestination;
    [SerializeField] private Transform bottomDestination;

    [Header("Trapdoor State")]
    [SerializeField] private Transform trapdoorPivot;
    [SerializeField] private bool startsAvailable;
    [SerializeField] private Vector3 closedLocalEulerAngles = Vector3.zero;
    [SerializeField] [Min(0f)] private float openRequiredAngle = 45f;

    [Header("Interaction")]
    [SerializeField] private Collider[] ladderInteractionColliders;

    private bool ladderAvailable;
    private bool hasAppliedInteractionState;
    private bool lastAppliedAvailability;

    public Transform TopDestination => topDestination;
    public Transform BottomDestination => bottomDestination;
    public bool LadderAvailable => EvaluateAvailability();

    private void Awake()
    {
        ladderAvailable = startsAvailable;
        ApplyInteractionState(force: true);
    }

    private void OnEnable()
    {
        ApplyInteractionState(force: true);
    }

    private void Update()
    {
        ApplyInteractionState();
    }

    public void SetLadderAvailable(bool isAvailable)
    {
        ladderAvailable = isAvailable;
        ApplyInteractionState(force: true);
    }

    public void HandleTrapdoorOpened()
    {
        ladderAvailable = true;
        ApplyInteractionState(force: true);
    }

    public void HandleTrapdoorClosed()
    {
        ladderAvailable = false;
        ApplyInteractionState(force: true);
    }

    public Transform GetDestination(TrapdoorLadderDirection direction)
    {
        return direction == TrapdoorLadderDirection.Down ? bottomDestination : topDestination;
    }

    private bool EvaluateAvailability()
    {
        return trapdoorPivot != null
            ? Quaternion.Angle(Quaternion.Euler(closedLocalEulerAngles), trapdoorPivot.localRotation) >= openRequiredAngle
            : ladderAvailable;
    }

    private void ApplyInteractionState(bool force = false)
    {
        bool isAvailable = EvaluateAvailability();
        if (!force && hasAppliedInteractionState && lastAppliedAvailability == isAvailable)
        {
            return;
        }

        hasAppliedInteractionState = true;
        lastAppliedAvailability = isAvailable;

        if (ladderInteractionColliders == null)
        {
            return;
        }

        for (int i = 0; i < ladderInteractionColliders.Length; i++)
        {
            Collider targetCollider = ladderInteractionColliders[i];
            if (targetCollider == null)
            {
                continue;
            }

            targetCollider.enabled = isAvailable;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyInteractionState(force: true);
    }
#endif
}
