[System.Serializable]
public class Labels
{
    public string info, name, level, score, playagain, language, setting, salinity;
    
    // [David] Thêm cho David_SeasonHUD
    public string water_level;      // "Mực nước sông" / "River Level"
    public string full;             // "Đầy" / "Full"
    public string low;              // "Thấp" / "Low"
    public string season_rainy;     // "Mùa mưa" / "Rainy Season"
    public string season_dry;       // "Mùa khô" / "Dry Season"
}