using UnityEngine;
using TMPro;

[DisallowMultipleComponent]
public sealed class CassetteShelfItem : InteractableBase
{
    [Header("Cassette")]
    [SerializeField] private CassetteData cassetteData;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private GameObject visualToHide;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private string prompt = "Take Cassette";

    [Header("Hover")]
    [SerializeField] private Transform hoverVisual;
    [SerializeField] private Vector3 hoverOffset = new Vector3(0f, 0.01f, 0f);
    [SerializeField] private float hoverMoveSpeed = 10f;

    [Header("Label")]
    [SerializeField] private bool showLabel = true;
    [SerializeField] private string labelTextOverride = string.Empty;
    [SerializeField] private Color labelColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Vector3 labelLocalPosition = new Vector3(0f, 0.85f, 0f);
    [SerializeField] private Vector3 labelLocalEulerAngles;
    [SerializeField] private float labelFontSize = 2.4f;
    [SerializeField] private float labelScale = 0.08f;

    private TextMeshPro labelText;

    private Vector3 hoverVisualStartLocalPosition;
    private bool isHovered;
    private bool hasBeenPickedUp;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
        hoverVisualStartLocalPosition = hoverVisual != null ? hoverVisual.localPosition : Vector3.zero;
        EnsureLabel();
    }

    private void Update()
    {
        if (hoverVisual == null)
        {
            return;
        }

        Vector3 targetLocalPosition = isHovered
            ? hoverVisualStartLocalPosition + hoverOffset
            : hoverVisualStartLocalPosition;
        hoverVisual.localPosition = Vector3.Lerp(
            hoverVisual.localPosition,
            targetLocalPosition,
            Time.deltaTime * hoverMoveSpeed);
    }

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        return !hasBeenPickedUp && base.CanInteract(interactor);
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        if (cassetteData != null)
        {
            return $"Take {cassetteData.CassetteName}";
        }

        return prompt;
    }

    protected override void OnHoverEnterInternal(PlayerInteractionController interactor)
    {
        isHovered = true;
    }

    protected override void OnHoverExitInternal(PlayerInteractionController interactor)
    {
        isHovered = false;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        ResolveReferences();

        if (cassetteData == null || cassettePlayerReceiver == null)
        {
            Debug.LogWarning("CassetteShelfItem is missing a CassetteData or CassettePlayerReceiver reference.", this);
            return;
        }

        if (!cassettePlayerReceiver.TryLoadCassette(cassetteData))
        {
            return;
        }

        hasBeenPickedUp = true;
        SetInteractionEnabled(false);

        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }

        GameObject targetVisual = visualToHide != null ? visualToHide : gameObject;
        targetVisual.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (cassettePlayerReceiver == null)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }

        if (visualToHide == null)
        {
            visualToHide = hoverVisual != null ? hoverVisual.gameObject : gameObject;
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider>();
        }

        if (hoverVisual == null)
        {
            hoverVisual = transform;
        }
    }

    private void EnsureLabel()
    {
        if (!showLabel)
        {
            return;
        }

        if (labelText == null)
        {
            labelText = GetComponentInChildren<TextMeshPro>(true);
        }

        if (labelText == null)
        {
            GameObject labelObject = new GameObject("CassetteLabel");
            labelObject.transform.SetParent(transform, false);
            labelText = labelObject.AddComponent<TextMeshPro>();
        }

        labelText.transform.localPosition = labelLocalPosition;
        labelText.transform.localRotation = Quaternion.Euler(labelLocalEulerAngles);
        labelText.transform.localScale = Vector3.one * labelScale;
        labelText.text = ResolveLabelText();
        labelText.color = labelColor;
        labelText.fontSize = labelFontSize;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private string ResolveLabelText()
    {
        if (!string.IsNullOrWhiteSpace(labelTextOverride))
        {
            return labelTextOverride;
        }

        if (cassetteData != null)
        {
            return cassetteData.CassetteName.ToUpperInvariant();
        }

        return "CASSETTE";
    }
}
