// Assets/Editor/RuneDatabaseEditor.cs
using UnityEngine;
using UnityEditor;
using System; // for Type scanning
using System.Linq; // for LINQ

[CustomEditor(typeof(UnityEngine.Object), true)]
public class RuneDatabaseEditor : Editor
{
    private Type runeDbType;

    private void OnEnable()
    {
        // 自動找名稱為 RuneDatabase 的類別（跨 Assembly）
        runeDbType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "RuneDatabase");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (runeDbType == null) return;
        if (!runeDbType.IsInstanceOfType(target)) return;

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("自動建立24筆預設符文資料", MessageType.Info);

        if (GUILayout.Button("➕ Generate 24 Default Runes"))
        {
            GenerateDefaults(target);
        }
    }

    private void GenerateDefaults(UnityEngine.Object dbObj)
    {
        SerializedObject so = new SerializedObject(dbObj);
        var runesProp = so.FindProperty("runes");

        runesProp.ClearArray();

        void Add(string id, string name, int color)
        {
            int index = runesProp.arraySize;
            runesProp.InsertArrayElementAtIndex(index);
            var elem = runesProp.GetArrayElementAtIndex(index);
            elem.FindPropertyRelative("AbilityID").stringValue = id;
            elem.FindPropertyRelative("DisplayName").stringValue = name;
            elem.FindPropertyRelative("Description").stringValue = "";
            elem.FindPropertyRelative("Icon").objectReferenceValue = null;
            elem.FindPropertyRelative("Color").enumValueIndex = color;
        }

        // Blue
        Add("blue00", "預設", 0);
        Add("blue01", "同學", 0);
        Add("blue02", "講師", 0);
        Add("blue03", "艾妮（學院）", 0);
        Add("blue04", "血系女巫", 0);
        Add("blue05", "艾妮（血系）", 0);

        // Red
        Add("red00", "預設", 1);
        Add("red01", "魔書", 1);
        Add("red02", "象牙塔之眼", 1);
        Add("red03", "伊莎貝拉", 1);
        Add("red04", "血系女巫", 1);
        Add("red05", "赫菲", 1);

        // Green
        Add("green00", "預設", 2);
        Add("green01", "狼", 2);
        Add("green02", "樹女", 2);
        Add("green03", "潼", 2);
        Add("green04", "吉兒", 2);
        Add("green05", "樹母", 2);

        // Yellow
        Add("yellow00", "預設", 3);
        Add("yellow01", "妖精打架", 3);
        Add("yellow02", "吃史萊姆", 3);
        Add("yellow03", "黑肉精靈", 3);
        Add("yellow04", "吸血精靈", 3);
        Add("yellow05", "獻祭女巫", 3);

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(dbObj);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ RuneDatabase 已自動產生 24 筆資料。");
    }
}
