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

    public CassetteData LoadedCassette => loadedCassette;
    public bool HasLoadedCassette => loadedCassette != null;

    private void Awake()
    {
        ResolveReferences();
        RefreshVisualState();
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

        if (loadedCassette.BroadcastConversation != null)
        {
            if (dialogueRunner == null)
            {
                Debug.LogWarning("CassettePlayerReceiver could not find a DialogueRunner to play the loaded cassette.", this);
                return false;
            }

            if (!dialogueRunner.StartConversation(loadedCassette.BroadcastConversation))
            {
                return false;
            }
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

    private void RefreshVisualState()
    {
        if (loadedCassetteVisual != null)
        {
            loadedCassetteVisual.SetActive(loadedCassette != null);
        }
    }
}
