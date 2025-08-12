using UnityEngine;

public class KeyMappingPanel : MonoBehaviour
{
    public Transform listParent;
    public GameObject itemPrefab;

    void Start()
    {
        AddKey("up", "Up");
        AddKey("down", "Down");
        AddKey("left", "Left");
        AddKey("right", "Right");
        AddKey("a", "A");
        AddKey("b", "B");
        AddKey("start", "Start");
        AddKey("select", "Select");
    }

    void AddKey(string keyFieldName, string displayName)
    {
        var go = Instantiate(itemPrefab, listParent);
        // go.GetComponent<KeyMappingItem>().Init(keyFieldName, displayName);
    }
}
