using System;
using System.Collections.Generic;

[Serializable]
public class LevelData
{
    public int Version => 1;
    public ItemData[] SaveData;

    public LevelData() { }

    public LevelData(IEnumerable<ItemData> data)
    {
        SaveData = new List<ItemData>(data).ToArray();
    }

    public override string ToString() => $"LevelData (Version: {Version}, SaveData Count: {SaveData?.Length ?? 0})";
}
