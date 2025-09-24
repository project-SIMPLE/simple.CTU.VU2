using UnityEngine;
using System.IO;
using System.Reflection;

public class NPC : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private ConversationUIController _conversationController;

    private NPCDialogue _npcDialogues = null;
    private string _fileName = "data";
    private Root _root;

    private string _currentLang = "vi";


    public void Talk()
    {
        GetDialoguesFromData();

        if (_npcDialogues != null)
            _conversationController.StartConversation(_npcDialogues.npcName, _npcDialogues.dialogues);
        else
            Debug.LogError("Can't get npc dialogue from data file.");
    }


    private void GetDialoguesFromData()
    {
        // get data file from Resources
        string resourceName = Path.GetFileNameWithoutExtension(_fileName);
        TextAsset jsonFile = Resources.Load<TextAsset>(resourceName);
        if (jsonFile == null)
        {
            Debug.LogError($"Không tìm thấy file JSON trong Resources: {resourceName}");
            return;
        }

        string jsonString = jsonFile.text;
        _root = JsonUtility.FromJson<Root>(jsonString);

        // get current langue (?)
        var lang = GetCurrentLangData();

        // get npc dialogues via npc id
        var dialogues = lang?.npcDialogues;
        _npcDialogues = dialogues.Find(npc => npc.npcId == _npcId);
    }
    

    // copy từ script Thuan_23127_JsonReader.cs sang
    public Lang GetCurrentLangData()
    {
        if (_root == null) return null;

        var fi = typeof(Root).GetField(_currentLang, BindingFlags.Public | BindingFlags.Instance);
        if (fi != null)
        {
            if (fi.GetValue(_root) is Lang langObj) return langObj;
        }

        if (_root.en != null) return _root.en;

        if (_root.vi != null) return _root.vi;

        // if (Root.fr != null) return Root.fr;
        // if (Root.th != null) return Root.th;

        // Debug.Log("Không tìm thấy ngôn ngữ phù hợp ");
        return null;
    }
}