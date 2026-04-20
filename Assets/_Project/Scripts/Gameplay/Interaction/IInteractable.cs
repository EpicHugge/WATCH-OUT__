public interface IInteractable
{
    bool CanInteract(PlayerInteractionController interactor);
    string GetInteractionPrompt(PlayerInteractionController interactor);
    void Interact(PlayerInteractionController interactor);
    void EndInteract(PlayerInteractionController interactor);
    void OnHoverEnter(PlayerInteractionController interactor);
    void OnHoverExit(PlayerInteractionController interactor);
}
