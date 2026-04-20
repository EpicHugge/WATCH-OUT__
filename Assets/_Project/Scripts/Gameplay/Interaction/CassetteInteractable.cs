using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class CassetteInteractable : InteractableBase
{
    [Header("Cassette")]
    [SerializeField] private CassetteData cassetteData;
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private string defaultPrompt = "Insert Cassette";
    [SerializeField] private bool disableOnSelect = true;
    [SerializeField] private GameObject objectToDisable;

    [Header("Events")]
    [SerializeField] private UnityEvent onCassetteSelected;

    protected override void Awake()
    {
        base.Awake();
        
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        if (cassetteData != null)
        {
            return $"Insert {cassetteData.CassetteName}";
        }

        return defaultPrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        if (progressionManager == null || cassetteData == null)
        {
            Debug.LogWarning("Missing ProgressionManager or CassetteData on CassetteInteractable.", this);
            return;
        }

        if (progressionManager.SelectCassette(cassetteData))
        {
            onCassetteSelected?.Invoke();

            if (disableOnSelect)
            {
                GameObject target = objectToDisable != null ? objectToDisable : gameObject;
                target.SetActive(false);
            }
        }
    }
}