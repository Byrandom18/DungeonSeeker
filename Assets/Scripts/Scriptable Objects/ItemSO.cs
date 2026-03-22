using UnityEngine;

[CreateAssetMenu(fileName = "ItemSO", menuName = "Scriptable Objects/ItemSO")]
public class ItemSO : ScriptableObject
{
    public int ID => GetInstanceID();
    [field: SerializeField] private Sprite Sprite { get; set; }
    [field: SerializeField] private bool IsStackable { get; set; }
    [field: SerializeField] private string Name { get; set; }
    [field: SerializeField, TextArea] private string Description { get; set; }
    
}
