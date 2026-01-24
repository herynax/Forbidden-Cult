using UnityEngine;

[CreateAssetMenu(fileName = "NewMergeObject", menuName = "Clicker/Minigame/MergeObject")]
public class MergeObjectSO : ScriptableObject
{
    public int level;
    public Sprite sprite;
    public Color itemColor = Color.white;
    public float scale = 1f;
    public double reward;
    public MergeObjectSO nextLevel;
}