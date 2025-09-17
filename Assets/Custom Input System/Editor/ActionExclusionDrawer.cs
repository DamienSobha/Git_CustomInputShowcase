#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.InputSystem;

[CustomPropertyDrawer(typeof(ActionExclusion))]
public class ActionExclusionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty mapProp = property.FindPropertyRelative("actionMapName");
        SerializedProperty actionProp = property.FindPropertyRelative("actionName");

        EditorGUI.BeginProperty(position, label, property);

        Rect mapRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        Rect actionRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + 2, position.width, EditorGUIUtility.singleLineHeight);

        // Get the InputActionAsset from the target object
        var target = property.serializedObject.targetObject as ActionMapRebindManager;
        if (target == null || target.inputAsset == null)
        {
            EditorGUI.HelpBox(position, "InputActionAsset not assigned in manager.", UnityEditor.MessageType.Warning);
            return;
        }

        string[] mapNames = target.inputAsset.actionMaps.Select(m => m.name).ToArray();
        int selectedMapIndex = Mathf.Max(0, System.Array.IndexOf(mapNames, mapProp.stringValue));
        selectedMapIndex = EditorGUI.Popup(mapRect, "Action Map", selectedMapIndex, mapNames);
        mapProp.stringValue = mapNames[selectedMapIndex];

        var selectedMap = target.inputAsset.actionMaps.FirstOrDefault(m => m.name == mapProp.stringValue);
        string[] actionNames = selectedMap?.actions.Select(a => a.name).ToArray() ?? new string[0];
        int selectedActionIndex = Mathf.Max(0, System.Array.IndexOf(actionNames, actionProp.stringValue));
        selectedActionIndex = EditorGUI.Popup(actionRect, "Action", selectedActionIndex, actionNames);
        if (actionNames.Length > 0)
            actionProp.stringValue = actionNames[selectedActionIndex];

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + 2) * 2;
    }
}
#endif
