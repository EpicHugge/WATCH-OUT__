using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassettePlayerInteractable : InteractableBase
{
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private DialogueConversation noCassetteLoadedConversation;
    [SerializeField] private string playPromptPrefix = "Play";
    [SerializeField] private string noCassettePrompt = "No Cassette Loaded";

    protected override void Awake()
    {
        base.Awake();
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (progressionManager != null)
        {
            progressionManager.StateChanged += RefreshLockState;
        }

        RefreshLockState();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.StateChanged -= RefreshLockState;
        }
    }

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        if (!base.CanInteract(interactor))
        {
            return false;
        }

        ResolveReferences();
        return dialogueRunner == null || !dialogueRunner.IsRunning;
    }

    public override string GetInteractionPrompt(PlayerInteractionController interactor)
    {
        if (IsLocked)
        {
            return base.GetInteractionPrompt(interactor);
        }

        ResolveReferences();
        if (cassettePlayerReceiver != null && cassettePlayerReceiver.HasLoadedCassette)
        {
            return $"{playPromptPrefix} {cassettePlayerReceiver.LoadedCassette.CassetteName}";
        }

        return noCassettePrompt;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        ResolveReferences();
        if (cassettePlayerReceiver != null && cassettePlayerReceiver.TryPlayLoadedCassette())
        {
            return;
        }

        if (dialogueRunner != null && noCassetteLoadedConversation != null)
        {
            dialogueRunner.StartConversation(noCassetteLoadedConversation);
        }
    }

    private void ResolveReferences()
    {
        if (cassettePlayerReceiver == null)
        {
            cassettePlayerReceiver = GetComponent<CassettePlayerReceiver>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }
    }

    private void RefreshLockState()
    {
        if (progressionManager == null)
        {
            return;
        }

        if (progressionManager.UnlockAllInteractionsForDebug)
        {
            SetLocked(false);
            return;
        }

        CassetteData loadedCassette = cassettePlayerReceiver != null ? cassettePlayerReceiver.LoadedCassette : null;
        SetLocked(!progressionManager.CanPlayCassette(loadedCassette));
    }
}
