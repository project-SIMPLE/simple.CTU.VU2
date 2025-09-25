using System.Collections.Generic;

[System.Serializable]
public class Lang
{
    public Labels labels;
    public Gameplay gameplay;
    public InterpretationData interpretation; // Mô tả của cây 
    public List<Plant> plants;
    public List<Animal> livestock;
    public List<Fish> fish;  

    // Linh's code
    public List<NPCDialogue> npcDialogues;
}