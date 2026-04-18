using UnityEngine;

[DisallowMultipleComponent]
public sealed class CassettePlayerReceiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private GameObject loadedCassetteVisual;

    [Header("Playback")]
    [SerializeField] private bool replaceLoadedCassette = true;
    [SerializeField] private bool playBroadcastConversationOnLoad = true;

    private CassetteData loadedCassette;

    public CassetteData LoadedCassette => loadedCassette;
    public bool HasLoadedCassette => loadedCassette != null;

    private void Awake()
    {
        ResolveReferences();
        RefreshVisualState();
    }

    public bool TryLoadCassette(CassetteData cassette)
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
        progressionManager?.MarkCassettePlaybackStarted(cassette);
        RefreshVisualState();

        if (playBroadcastConversationOnLoad && cassette.BroadcastConversation != null && dialogueRunner != null)
        {
            dialogueRunner.StartConversation(cassette.BroadcastConversation);
        }

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

    private void RefreshVisualState()
    {
        if (loadedCassetteVisual != null)
        {
            loadedCassetteVisual.SetActive(loadedCassette != null);
        }
    }
}
