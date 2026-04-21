using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassetteShelfItem : InteractableBase
{
    [Header("Cassette")]
    [SerializeField] private CassetteData cassetteData;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private GameObject visualToHide;
    [SerializeField] private Collider interactionCollider;
    [SerializeField] private string prompt = "Take Cassette";
    [SerializeField] private string alreadyCarryingPrompt = "Already Carrying Cassette";

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
    private CassettePlayerReceiver subscribedCassettePlayerReceiver;

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
        RefreshSubscriptions();
        hoverVisualStartLocalPosition = hoverVisual != null ? hoverVisual.localPosition : Vector3.zero;
        EnsureLabel();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RefreshSubscriptions();
    }

    private void OnDisable()
    {
        RefreshSubscriptions(clearOnly: true);
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
        return base.CanInteract(interactor) && !hasBeenPickedUp && cassetteData != null;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        ResolveReferences();

        if (cassettePlayerReceiver != null && !cassettePlayerReceiver.CanSelectCassette(cassetteData))
        {
            return alreadyCarryingPrompt;
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
            return;
        }

        if (!cassettePlayerReceiver.TrySelectCassette(cassetteData))
        {
            return;
        }

        hasBeenPickedUp = true;
        SetInteractionEnabled(false);

        if (interactionCollider != null)
        {
            interactionCollider.enabled = false;
        }

        SetVisualVisible(false);
    }

    private void ResolveReferences()
    {
        if (cassettePlayerReceiver == null)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }

        if (visualToHide == null)
        {
            visualToHide = hoverVisual != null && hoverVisual.gameObject != gameObject
                ? hoverVisual.gameObject
                : (transform.childCount > 0 ? transform.GetChild(0).gameObject : gameObject);
        }

        if (interactionCollider == null)
        {
            interactionCollider = GetComponent<Collider>();
        }

        if (hoverVisual == null)
        {
            hoverVisual = transform;
        }

        RefreshSubscriptions();
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

    private void HandleCassetteReleased(CassetteData cassette)
    {
        if (!hasBeenPickedUp || cassetteData == null || cassette != cassetteData)
        {
            return;
        }

        hasBeenPickedUp = false;
        SetInteractionEnabled(true);

        if (interactionCollider != null)
        {
            interactionCollider.enabled = true;
        }

        SetVisualVisible(true);
    }

    private void RefreshSubscriptions(bool clearOnly = false)
    {
        if (subscribedCassettePlayerReceiver != null)
        {
            subscribedCassettePlayerReceiver.CassetteReleased -= HandleCassetteReleased;
            subscribedCassettePlayerReceiver = null;
        }

        if (clearOnly || !isActiveAndEnabled || cassettePlayerReceiver == null)
        {
            return;
        }

        cassettePlayerReceiver.CassetteReleased += HandleCassetteReleased;
        subscribedCassettePlayerReceiver = cassettePlayerReceiver;
    }

    private void SetVisualVisible(bool isVisible)
    {
        GameObject targetVisual = visualToHide != null ? visualToHide : gameObject;
        if (targetVisual != gameObject)
        {
            targetVisual.SetActive(isVisible);
            return;
        }

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].enabled = isVisible;
            }
        }
    }
}
