#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Protocol0317.Editor
{
    public static class ProtocolSceneBuilder
    {
        [MenuItem("Protocol 03:17/Create or Rebuild Main Scene")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("ProtocolGame", typeof(Protocol0317.ProtocolGame));
            root.GetComponent<Protocol0317.ProtocolGame>().BakeEnvironmentForEditor();
            // Persist generated materials as assets so their colors survive the EXE build.
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (!Directory.Exists("Assets/GeneratedMaterials")) Directory.CreateDirectory("Assets/GeneratedMaterials");
            int materialIndex = 0;
            foreach (var renderer in renderers)
            {
                if (!(renderer is MeshRenderer)) continue;
                var material = renderer.sharedMaterial;
                if (material == null || AssetDatabase.Contains(material)) continue;
                string materialPath = "Assets/GeneratedMaterials/Room_" + materialIndex++.ToString("D3") + ".mat";
                AssetDatabase.CreateAsset(material, materialPath);
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            }
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Protocol 03:17: saved room, camera and lights in Main.unity. Ready to build Windows EXE.");
        }
    }
}
#endif
