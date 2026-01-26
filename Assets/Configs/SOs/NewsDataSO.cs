using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewsData", menuName = "Clicker/News Data")]
public class NewsDataSO : ScriptableObject
{
    public List<NewsEntry> allNews;
}