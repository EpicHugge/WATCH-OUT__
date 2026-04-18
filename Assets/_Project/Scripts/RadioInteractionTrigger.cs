using UnityEngine;
using WatchOut;

/// <summary>
/// Attach this to physical 3D button objects to bridge interaction events to the RadioSystem.
/// </summary>
public class RadioInteractionTrigger : MonoBehaviour
{
    [SerializeField] private RadioSystem radioSystem;
    [SerializeField] public bool isIncreaseButton;

    /// <summary>
    /// Call this method from your Interaction System when the button is pressed/clicked.
    /// </summary>
    public void OnPress()
    {
        if (isIncreaseButton)
            radioSystem.SetIncreasing(true);
        else
            radioSystem.SetDecreasing(true);
    }

    /// <summary>
    /// Call this method from your Interaction System when the button is released.
    /// </summary>
    public void OnRelease()
    {
        if (isIncreaseButton)
            radioSystem.SetIncreasing(false);
        else
            radioSystem.SetDecreasing(false);
    }
}