using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassettePlayerReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private GameObject loadedCassetteVisual;

    [Header("Selection")]
    [SerializeField] private bool replaceLoadedCassette = true;

    private CassetteData loadedCassette;
    private CassetteData pendingPlaybackCassette;
    private DialogueRunner subscribedDialogueRunner;

    public CassetteData LoadedCassette => loadedCassette;
    public bool HasLoadedCassette => loadedCassette != null;
    public CassetteData PendingPlaybackCassette => pendingPlaybackCassette;

    private void Awake()
    {
        ResolveReferences();
        RefreshVisualState();
    }

    private void OnEnable()
    {
        ResolveReferences();
    }

    private void OnDisable()
    {
        RefreshSubscriptions(clearOnly: true);
    }

    public bool TrySelectCassette(CassetteData cassette)
    {
        if (cassette == null)
        {
            Debug.LogWarning("CassettePlayerReceiver rejected a null cassette selection.", this);
            return false;
        }

        ResolveReferences();

        if (loadedCassette != null && loadedCassette != cassette && !replaceLoadedCassette)
        {
            Debug.LogWarning(
                $"CassettePlayerReceiver blocked selecting '{cassette.CassetteName}' because '{loadedCassette.CassetteName}' is already loaded.",
                this);
            return false;
        }

        CassetteData previousCassette = loadedCassette;
        loadedCassette = cassette;
        RefreshVisualState();

        if (progressionManager != null && !progressionManager.SelectCassette(cassette))
        {
            loadedCassette = previousCassette;
            RefreshVisualState();
            Debug.LogWarning(
                $"CassettePlayerReceiver could not select '{cassette.CassetteName}' during step {progressionManager.CurrentObjectiveStep}.",
                this);
            return false;
        }

        RefreshVisualState();
        return true;
    }

    public bool TryPlayLoadedCassette()
    {
        ResolveReferences();
        TryRestoreLoadedCassetteFromProgression();

        if (loadedCassette == null)
        {
            Debug.LogWarning("CassettePlayerReceiver could not play because no cassette is loaded.", this);
            return false;
        }

        if (progressionManager != null && !progressionManager.CanPlayCassette(loadedCassette))
        {
            Debug.LogWarning(
                $"CassettePlayerReceiver blocked playback for '{loadedCassette.CassetteName}' during step {progressionManager.CurrentObjectiveStep}.",
                this);
            return false;
        }

        if (loadedCassette.BroadcastConversation != null)
        {
            if (dialogueRunner == null)
            {
                Debug.LogWarning("CassettePlayerReceiver could not find a DialogueRunner to play the loaded cassette.", this);
                return false;
            }

            pendingPlaybackCassette = loadedCassette;
            Debug.Log($"CassettePlayerReceiver started playback for '{loadedCassette.CassetteName}'.", this);
            if (!dialogueRunner.StartConversation(loadedCassette.BroadcastConversation))
            {
                pendingPlaybackCassette = null;
                return false;
            }

            return true;
        }

        progressionManager?.MarkCassettePlaybackStarted(loadedCassette);
        return true;
    }

    private void ResolveReferences()
    {
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }

        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        RefreshSubscriptions();
    }

    private void TryRestoreLoadedCassetteFromProgression()
    {
        if (loadedCassette != null || progressionManager == null)
        {
            return;
        }

        CassetteData selectedCassette = progressionManager.SelectedCassetteToday;
        if (selectedCassette == null || progressionManager.CurrentObjectiveStep != GameJamObjectiveStep.PlayCassette)
        {
            return;
        }

        loadedCassette = selectedCassette;
        RefreshVisualState();
        Debug.Log($"CassettePlayerReceiver restored '{selectedCassette.CassetteName}' from progression state.", this);
    }

    private void HandleConversationEnded(DialogueConversation conversation)
    {
        if (pendingPlaybackCassette == null || conversation == null)
        {
            return;
        }

        if (pendingPlaybackCassette.BroadcastConversation != conversation)
        {
            return;
        }

        bool playbackMarkedComplete = progressionManager != null &&
                                      progressionManager.MarkCassettePlaybackStarted(pendingPlaybackCassette);
        Debug.Log(
            $"CassettePlayerReceiver finished '{pendingPlaybackCassette.CassetteName}'. Playback marked complete: {playbackMarkedComplete}.",
            this);
        pendingPlaybackCassette = null;
    }

    private void RefreshVisualState()
    {
        if (loadedCassetteVisual != null)
        {
            loadedCassetteVisual.SetActive(loadedCassette != null);
        }
    }

    private void RefreshSubscriptions(bool clearOnly = false)
    {
        if (subscribedDialogueRunner != null)
        {
            subscribedDialogueRunner.ConversationEnded -= HandleConversationEnded;
            subscribedDialogueRunner = null;
        }

        if (clearOnly || !isActiveAndEnabled || dialogueRunner == null)
        {
            return;
        }

        dialogueRunner.ConversationEnded += HandleConversationEnded;
        subscribedDialogueRunner = dialogueRunner;
    }
}
