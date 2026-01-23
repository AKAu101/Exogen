#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemData))]  // This tells Unity to use this editor for ItemData objects
public class ItemDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Get the ItemData we're editing
        ItemData itemData = (ItemData)target;
        
        // First, draw the default inspector (shows all the normal fields)
        DrawDefaultInspector();
        
        // Add some space
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        // Add our custom section
        EditorGUILayout.LabelField("Animation Helper", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Use this to automatically set animation type based on item name", MessageType.Info);
        
        // Create a button
        if (GUILayout.Button("Auto-detect Animation Type", GUILayout.Height(30)))
        {
            AutoDetectAnimationType(itemData);
        }
        
        // Show what the current animation type is
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Animation Type:", EditorStyles.miniBoldLabel);
        EditorGUILayout.SelectableLabel(itemData.animationType.ToString(), EditorStyles.textField, GUILayout.Height(20));
        
        // Add another button for specific types
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Set:", EditorStyles.miniBoldLabel);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set as Lantern"))
        {
            itemData.animationType = ItemData.HandItemAnimation.Lantern;
            itemData.itemCategory = ItemData.ItemCategory.Lantern;
            EditorUtility.SetDirty(itemData);
        }
        
        if (GUILayout.Button("Set as Scanner"))
        {
            itemData.animationType = ItemData.HandItemAnimation.Radar;
            itemData.itemCategory = ItemData.ItemCategory.Scanner;
            EditorUtility.SetDirty(itemData);
        }
        
        if (GUILayout.Button("Set as Other"))
        {
            itemData.animationType = ItemData.HandItemAnimation.Other;
            itemData.itemCategory = ItemData.ItemCategory.Tool;
            EditorUtility.SetDirty(itemData);
        }
        EditorGUILayout.EndHorizontal();
    }
    
    private void AutoDetectAnimationType(ItemData itemData)
    {
        if (itemData.itemName == null)
        {
            Debug.LogWarning("ItemData has no name!");
            return;
        }
        
        string name = itemData.itemName.ToLower();
        
        if (name.Contains("lantern"))
        {
            itemData.animationType = ItemData.HandItemAnimation.Lantern;
            itemData.itemCategory = ItemData.ItemCategory.Lantern;
            Debug.Log($"✓ Set '{itemData.itemName}' as Lantern");
        }
        else if (name.Contains("scanner") || name.Contains("radar"))
        {
            itemData.animationType = ItemData.HandItemAnimation.Radar;
            itemData.itemCategory = ItemData.ItemCategory.Scanner;
            Debug.Log($"✓ Set '{itemData.itemName}' as Scanner");
        }
        else if (name.Contains("tool") || name.Contains("weapon") || name.Contains("pickaxe") || name.Contains("hammer"))
        {
            itemData.animationType = ItemData.HandItemAnimation.Other;
            itemData.itemCategory = ItemData.ItemCategory.Tool;
            Debug.Log($"✓ Set '{itemData.itemName}' as Tool (Other)");
        }
        else
        {
            itemData.animationType = ItemData.HandItemAnimation.Nothing;
            itemData.itemCategory = ItemData.ItemCategory.General;
            Debug.Log($"✓ Set '{itemData.itemName}' as General (Nothing)");
        }
        
        // Mark the item as changed so Unity saves it
        EditorUtility.SetDirty(itemData);
        
        // Also force a save if in prefab mode
        PrefabUtility.RecordPrefabInstancePropertyModifications(itemData);
    }
}
#endif