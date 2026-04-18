using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProgressionGeneratorBridge : MonoBehaviour
{
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private GeneratorInteractable generatorInteractable;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (generatorInteractable == null)
        {
            return;
        }

        generatorInteractable.TurnedOn += HandleTurnedOn;
    }

    private void OnDisable()
    {
        if (generatorInteractable == null)
        {
            return;
        }

        generatorInteractable.TurnedOn -= HandleTurnedOn;
    }

    private void ResolveReferences()
    {
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }

        if (generatorInteractable == null)
        {
            generatorInteractable = GetComponent<GeneratorInteractable>();
        }
    }

    private void HandleTurnedOn()
    {
        progressionManager?.StartGenerator();
    }
}
