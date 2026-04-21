using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueInteractable : InteractableBase
{
    [SerializeField] private DialogueConversation conversation;
    [SerializeField] private DialogueRunner dialogueRunner;

    public override bool CanInteract(PlayerInteractionController interactor)
    {
        if (!base.CanInteract(interactor) || conversation == null)
        {
            return false;
        }

        DialogueRunner runner = ResolveRunner(interactor);
        return runner == null || !runner.IsRunning;
    }

    protected override void InteractInternal(PlayerInteractionController interactor)
    {
        DialogueRunner runner = ResolveRunner(interactor);
        if (runner == null)
        {
            return;
        }

        runner.StartConversation(conversation);
    }

    private DialogueRunner ResolveRunner(PlayerInteractionController interactor)
    {
        if (dialogueRunner != null)
        {
            return dialogueRunner;
        }

        return interactor != null ? interactor.GetComponent<DialogueRunner>() : null;
    }
}
