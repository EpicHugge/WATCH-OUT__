using UnityEngine;

[CreateAssetMenu(fileName = "CAS_", menuName = "Progression/Cassette Data")]
public sealed class CassetteData : ScriptableObject
{
    [Header("Cassette")]
    [SerializeField] private string cassetteName = "New Cassette";
    [SerializeField] [TextArea(2, 4)] private string description = string.Empty;
    [SerializeField] private DialogueConversation broadcastConversation;
    [SerializeField] private string category = string.Empty;

    [Header("Notes")]
    [SerializeField] [TextArea(2, 4)] private string debugNotes = string.Empty;

    public string CassetteName => string.IsNullOrWhiteSpace(cassetteName) ? name : cassetteName.Trim();
    public string Description => description;
    public DialogueConversation BroadcastConversation => broadcastConversation;
    public string Category => category;
    public string DebugNotes => debugNotes;
}
