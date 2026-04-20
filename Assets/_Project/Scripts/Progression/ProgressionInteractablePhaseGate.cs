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

        if (progressionManager.UnlockAllInteractionsForDebug)
        {
            targetInteractable.SetLocked(false);
            return;
        }

        if (progressionManager.IsGameComplete && targetInteractable is SleepBedInteractable)
        {
            targetInteractable.SetLocked(true);
            return;
        }

        bool isAllowed = allowedPhases != null && allowedPhases.Contains(progressionManager.CurrentPhase);

        // Keep the sleep bed available once the nightly power-out has happened,
        // even if debug flow timing lands the manager in a transient state.
        if (!isAllowed &&
            targetInteractable is SleepBedInteractable &&
            progressionManager.PowerOutTriggeredToday &&
            !progressionManager.IsGameComplete)
        {
            isAllowed = true;
        }

        targetInteractable.SetLocked(!isAllowed);
    }

    public void RefreshLockState()
    {
        ApplyLockState();
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
