using UnityEditor;
using UnityEngine;

// IDπ‹¿Ì∆˜
[CreateAssetMenu(menuName = "Data/ID Manager")]
public class IdManager : ScriptableObject {

    
    [SerializeField] private int _nextId = 1;

    public int GetNextID() {
        int current = _nextId;
        _nextId++;

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif

        return current;
    }
}