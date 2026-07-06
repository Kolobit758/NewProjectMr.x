using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CustomFormationData))]
public class CustomFormationEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CustomFormationData data = (CustomFormationData)target;

        // ส่วนตั้งค่าทั่วไป
        data.gridWidth = EditorGUILayout.IntField("Grid Width", data.gridWidth);
        data.gridHeight = EditorGUILayout.IntField("Grid Height", data.gridHeight);
        data.spacing = EditorGUILayout.FloatField("Spacing", data.spacing);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--- ดีไซน์ Formation (ติ๊กช่องที่ให้ยูนิตยืน) ---", EditorStyles.boldLabel);

        // ตรวจสอบและปรับขนาดขอบเขต Array ให้ตรงกับ Width/Height
        if (data.rows == null || data.rows.Length != data.gridHeight)
        {
            data.rows = new FormationRow[data.gridHeight];
        }
        for (int i = 0; i < data.gridHeight; i++)
        {
            if (data.rows[i] == null || data.rows[i].cols == null || data.rows[i].cols.Length != data.gridWidth)
            {
                data.rows[i] = new FormationRow();
                data.rows[i].cols = new bool[data.gridWidth];
            }
        }

        // วาดตาราง Grid 2D ของ Bool
        for (int y = 0; y < data.gridHeight; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < data.gridWidth; x++)
            {
                // วาดกล่องติ๊กถูก (Toggle) เรียงกันเป็นแถวแนวนอน
                data.rows[y].cols[x] = EditorGUILayout.Toggle(data.rows[y].cols[x], GUILayout.Width(25));
            }
            EditorGUILayout.EndHorizontal();
        }

        // เซฟค่าเมื่อมีการเปลี่ยนแปลงใน Inspector
        if (GUI.changed)
        {
            EditorUtility.SetDirty(data);
        }
    }
}