using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassettePlayerReceiver : MonoBehaviour
{
    [Header("References")]
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
    public event Action<CassetteData> CassetteLoaded;
    public event Action<CassetteData> CassetteReleased;

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

    public bool CanSelectCassette(CassetteData cassette)
    {
        return cassette != null && loadedCassette == null;
    }

    public bool TrySelectCassette(CassetteData cassette)
    {
        if (!CanSelectCassette(cassette))
        {
            return false;
        }

        ResolveReferences();

        loadedCassette = cassette;
        pendingPlaybackCassette = null;
        RefreshVisualState();
        CassetteLoaded?.Invoke(loadedCassette);
        return true;
    }

    public bool TryPlayLoadedCassette()
    {
        ResolveReferences();
        if (loadedCassette == null)
        {
            return false;
        }

        if (loadedCassette.BroadcastConversation == null)
        {
            ReleaseLoadedCassette();
            return true;
        }

        if (dialogueRunner == null)
        {
            return false;
        }

        pendingPlaybackCassette = loadedCassette;
        if (dialogueRunner.StartConversation(loadedCassette.BroadcastConversation))
        {
            return true;
        }

        pendingPlaybackCassette = null;
        return false;
    }

    private void ResolveReferences()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        RefreshSubscriptions();
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

        ReleaseLoadedCassette();
    }

    private void RefreshVisualState()
    {
        if (loadedCassetteVisual != null)
        {
            loadedCassetteVisual.SetActive(loadedCassette != null);
        }
    }

    private void ReleaseLoadedCassette()
    {
        CassetteData releasedCassette = loadedCassette;
        loadedCassette = null;
        pendingPlaybackCassette = null;
        RefreshVisualState();

        if (releasedCassette != null)
        {
            CassetteReleased?.Invoke(releasedCassette);
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
