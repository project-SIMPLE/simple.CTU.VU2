using System.Collections.Generic;

[System.Serializable]
public class Lang
{
    public Labels labels; 
    public Gameplay gameplay;  
    public InterpretationData interpretation; // Mô tả của cây 
    public List<Plant> plants;
}