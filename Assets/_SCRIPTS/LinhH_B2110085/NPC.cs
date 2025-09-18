using UnityEngine;

public class NPC : MonoBehaviour
{
    [SerializeField] private string _npcName;
    [SerializeField] private string[] _dialogues;

    [SerializeField] private ConversationUIController _conversationController;


    public void Talk()
    {
        _conversationController.StartConversation(_npcName, _dialogues);
    }
}