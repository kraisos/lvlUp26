using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        if (GUILayout.Button("Force Update Inventory UI"))
        {
            var inventory = (Inventory)target;
            inventory.ForceNotifyChanged();
            EditorUtility.SetDirty(inventory);
        }
    }
}