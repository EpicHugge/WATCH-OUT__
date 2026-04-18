using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ProgressionInteractablePhaseGate : MonoBehaviour
{
    [SerializeField] private ProgressionManager progressionManager;
    [SerializeField] private InteractableBase targetInteractable;
    [SerializeField] private bool unlockWhenManagerMissing = true;
    [SerializeField] private List<DayPhase> allowedPhases = new List<DayPhase> { DayPhase.PowerOut, DayPhase.CanSleep };

    private void Awake()
    {
        ResolveReferences();
        ApplyLockState();
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (progressionManager != null)
        {
            progressionManager.StateChanged += ApplyLockState;
        }

        ApplyLockState();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.StateChanged -= ApplyLockState;
        }
    }

    private void ResolveReferences()
    {
        if (progressionManager == null)
        {
            progressionManager = FindAnyObjectByType<ProgressionManager>();
        }

        if (targetInteractable == null)
        {
            targetInteractable = GetComponent<InteractableBase>();
        }
    }

    private void ApplyLockState()
    {
        if (targetInteractable == null)
        {
            return;
        }

        if (progressionManager == null)
        {
            targetInteractable.SetLocked(!unlockWhenManagerMissing);
            return;
        }

        bool isAllowed = allowedPhases != null && allowedPhases.Contains(progressionManager.CurrentPhase);
        targetInteractable.SetLocked(!isAllowed);
    }

    private void OnValidate()
    {
        if (targetInteractable == null)
        {
            targetInteractable = GetComponent<InteractableBase>();
        }

        if (allowedPhases == null)
        {
            allowedPhases = new List<DayPhase>();
        }

        for (int i = allowedPhases.Count - 1; i >= 0; i--)
        {
            DayPhase candidate = allowedPhases[i];
            if (allowedPhases.IndexOf(candidate) != i)
            {
                allowedPhases.RemoveAt(i);
            }
        }
    }
}
