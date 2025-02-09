using UnityEditor;
using UnityEngine;

namespace VarCo.GoogleSheetsDownloader
{
    [CustomEditor(typeof(GoogleSheetDownloader))]
    public class GoogleSheetDownloaderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Space();

            if (GUILayout.Button("Download"))
            {
                ((GoogleSheetDownloader)target).Download();
            }
        }
    }
}