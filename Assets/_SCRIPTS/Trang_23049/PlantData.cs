using System;
using System.Collections.Generic;

[Serializable]
public class PlantData
{
    public int id;
    public string tag_name;
    public int growth_time;
    public List<string> status;
    public int economic_benefits;
    public string information;
}
public class FishData
{

}
[Serializable]
public class LanguageData
{
    public List<PlantData> plants;
    public List<FishData> fish; 
}

[Serializable]
public class RootData
{
    public LanguageData vi;
    public LanguageData en;
}
