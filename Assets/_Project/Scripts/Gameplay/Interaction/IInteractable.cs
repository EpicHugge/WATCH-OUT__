public interface IInteractable
{
    bool CanInteract(PlayerInteractionController interactor);
    string GetInteractionPrompt(PlayerInteractionController interactor);
    void Interact(PlayerInteractionController interactor);
    void SetHighlighted(bool isHighlighted);
}
