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

    public CassetteData LoadedCassette => loadedCassette;
    public bool HasLoadedCassette => loadedCassette != null;

    private void Awake()
    {
        ResolveReferences();
        RefreshVisualState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (dialogueRunner != null)
        {
            dialogueRunner.ConversationEnded += HandleConversationEnded;
        }
    }

    private void OnDisable()
    {
        if (dialogueRunner != null)
        {
            dialogueRunner.ConversationEnded -= HandleConversationEnded;
        }
    }

    public bool TrySelectCassette(CassetteData cassette)
    {
        if (cassette == null)
        {
            return false;
        }

        ResolveReferences();

        if (loadedCassette != null && loadedCassette != cassette && !replaceLoadedCassette)
        {
            return false;
        }

        if (progressionManager != null && !progressionManager.SelectCassette(cassette))
        {
            return false;
        }

        loadedCassette = cassette;
        RefreshVisualState();
        return true;
    }

    public bool TryPlayLoadedCassette()
    {
        if (loadedCassette == null)
        {
            return false;
        }

        ResolveReferences();

        if (progressionManager != null && !progressionManager.CanPlayCassette(loadedCassette))
        {
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

        progressionManager?.MarkCassettePlaybackStarted(pendingPlaybackCassette);
        pendingPlaybackCassette = null;
    }

    private void RefreshVisualState()
    {
        if (loadedCassetteVisual != null)
        {
            loadedCassetteVisual.SetActive(loadedCassette != null);
        }
    }
}
