using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CassetteInteractable : InteractableBase
{
    [Header("Cassette")]
    [SerializeField] private CassetteData cassetteData;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private string defaultPrompt = "Insert Cassette";
    [SerializeField] private string alreadyCarryingPrompt = "Already Carrying Cassette";
    [SerializeField] private bool disableOnSelect = true;
    [SerializeField] private GameObject objectToDisable;

    [Header("Events")]
    [SerializeField] private UnityEvent onCassetteSelected;

    protected override void Awake()
    {
        base.Awake();

        if (cassettePlayerReceiver == null)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }
    }

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        return base.CanInteract(interactor) && cassetteData != null;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        ResolveReceiver();

        if (cassettePlayerReceiver != null && !cassettePlayerReceiver.CanSelectCassette(cassetteData))
        {
            return alreadyCarryingPrompt;
        }

        if (cassetteData != null)
        {
            return $"Insert {cassetteData.CassetteName}";
        }

        return defaultPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        ResolveReceiver();

        if (cassetteData == null || cassettePlayerReceiver == null)
        {
            return;
        }

        if (!cassettePlayerReceiver.TrySelectCassette(cassetteData))
        {
            return;
        }

        onCassetteSelected?.Invoke();

        if (disableOnSelect)
        {
            GameObject target = objectToDisable != null ? objectToDisable : gameObject;
            target.SetActive(false);
        }
    }

    private void ResolveReceiver()
    {
        if (cassettePlayerReceiver == null)
        {
            cassettePlayerReceiver = FindAnyObjectByType<CassettePlayerReceiver>();
        }
    }
}
