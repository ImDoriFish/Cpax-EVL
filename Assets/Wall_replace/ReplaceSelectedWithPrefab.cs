using UnityEngine;
using UnityEditor;

public class ReplaceSelectedWithPrefab : EditorWindow
{
    private GameObject prefab;
    private bool copyOldScale = false;

    [MenuItem("Tools/Replace Selected With Prefab")]
    public static void ShowWindow()
    {
        GetWindow<ReplaceSelectedWithPrefab>("Replace With Prefab");
    }

    private void OnGUI()
    {
        prefab = (GameObject)EditorGUILayout.ObjectField(
            "New Wall Prefab",
            prefab,
            typeof(GameObject),
            false
        );

        copyOldScale = EditorGUILayout.Toggle("Copy Old Scale", copyOldScale);

        if (GUILayout.Button("Replace Selected Objects"))
        {
            ReplaceObjects();
        }
    }

    private void ReplaceObjects()
    {
        if (prefab == null)
        {
            Debug.LogError("No prefab assigned.");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        foreach (GameObject oldObject in selectedObjects)
        {
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Undo.RegisterCreatedObjectUndo(newObject, "Replace Selected With Prefab");

            newObject.transform.SetParent(oldObject.transform.parent);

            newObject.transform.position = oldObject.transform.position;
            newObject.transform.rotation = oldObject.transform.rotation;

            if (copyOldScale)
            {
                newObject.transform.localScale = oldObject.transform.localScale;
            }
            else
            {
                newObject.transform.localScale = prefab.transform.localScale;
            }

            newObject.name = oldObject.name + "_Replaced";

            Undo.DestroyObjectImmediate(oldObject);
        }

        Debug.Log("Finished replacing selected objects.");
    }
}