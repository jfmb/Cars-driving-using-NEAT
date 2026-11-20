using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

/// Temporary diagnostic script: reports the current render-pipeline / scene-template
/// configuration so the before/after state can be compared.
public static class Urp3DProbe
{
    private static readonly List<string> Report = new();

    public static void Run()
    {
        Report.Clear();

        try
        {
            Report.Add("=== Graphics / Quality pipeline assignment ===");
            Report.Add($"  GraphicsSettings.defaultRenderPipeline = " +
                       $"{GraphicsSettings.defaultRenderPipeline?.name ?? "<null>"}");
            Report.Add($"  GraphicsSettings.renderPipelineAsset = " +
                       $"{GraphicsSettings.defaultRenderPipeline?.name ?? "<null>"}");
            for (var i = 0; i < QualitySettings.names.Length; i++)
            {
                Report.Add($"  Quality '{QualitySettings.names[i]}' renderPipeline = " +
                           $"{QualitySettings.renderPipeline?.name ?? "<null>"}");
            }

            Report.Add("=== URP asset renderers ===");
            var urpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                "Assets/Settings/UniversalRP.asset");
            if (urpAsset != null)
            {
                var serialized = new SerializedObject(urpAsset);
                var list = serialized.FindProperty("m_RendererDataList");
                var defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");
                Report.Add($"  renderer count = {list.arraySize}, default index = {defaultIndex.intValue}");
                for (var i = 0; i < list.arraySize; i++)
                {
                    var element = list.GetArrayElementAtIndex(i).objectReferenceValue;
                    Report.Add($"  [{i}] {element?.name ?? "<null>"} ({element?.GetType().Name})");
                }

                Report.Add($"  defaultShader = {urpAsset.defaultShader?.name ?? "<null>"}");
                Report.Add($"  defaultMaterial = {urpAsset.defaultMaterial?.name ?? "<null>"}");
            }

            Report.Add("=== Editor behavior mode ===");
            Report.Add($"  EditorSettings.defaultBehaviorMode = {EditorSettings.defaultBehaviorMode}");

            Report.Add("=== Scene templates ===");
            foreach (var guid in AssetDatabase.FindAssets("t:SceneTemplateAsset"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var template = AssetDatabase.LoadAssetAtPath<UnityEditor.SceneTemplate.SceneTemplateAsset>(path);
                Report.Add($"  {path} name='{template.name}' addToDefaults={template.addToDefaults} " +
                           $"title='{template.templateName}'");
            }

            Report.Add("=== Scene template infos (internal) ===");
            var module = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "UnityEditor.SceneTemplateModule");
            var utilsType = module?.GetType("UnityEditor.SceneTemplate.SceneTemplateUtils");
            var infosMethod = utilsType?.GetMethod("GetSceneTemplateInfos",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (infosMethod?.Invoke(null, null) is System.Collections.IEnumerable infos)
            {
                foreach (var info in infos)
                {
                    var infoType = info.GetType();
                    var name = infoType.GetField("name")?.GetValue(info);
                    var assetPath = infoType.GetField("assetPath")?.GetValue(info);
                    var isPinned = infoType.GetField("isPinned")?.GetValue(info);
                    var isDeprecated = infoType.GetField("isDeprecated")?.GetValue(info);
                    Report.Add($"  '{name}' path={assetPath} pinned={isPinned} deprecated={isDeprecated}");
                }
            }

            Report.Add("=== New scene defaults (NewSceneSetup.DefaultGameObjects) ===");
            Report.Add(DescribeNewScene());

            Report.Add("=== Existing scene cameras ===");
            foreach (var scenePath in new[]
                     {
                         "Assets/Scenes/Scene00.unity",
                         "Assets/Scenes/Scene01.unity",
                         "Assets/Scenes/Scene01.1.unity",
                         "Assets/Scenes/Scene02.unity",
                     })
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                var cameras = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Camera>(true)).ToArray();
                foreach (var camera in cameras)
                {
                    var cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
                    var lights = scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
                    Report.Add($"  {scenePath}: camera '{camera.name}' ortho={camera.orthographic} " +
                               $"clearFlags={camera.clearFlags} " +
                               $"rendererIndex={ReadRendererIndex(cameraData)} " +
                               $"lights={lights.Length} " +
                               $"lightTypes={string.Join(",", lights.Select(l => l.type.ToString()))}");
                }
            }
        }
        catch (Exception exception)
        {
            Report.Add("EXCEPTION: " + exception);
        }

        File.WriteAllLines(Path.Combine(Application.dataPath, "..", "Logs", "urp3d_probe.txt"), Report);
        Debug.Log("PROBE: report written.");
    }

    private static string ReadRendererIndex(UniversalAdditionalCameraData cameraData)
    {
        if (cameraData == null)
        {
            return "<no camera data>";
        }

        var field = typeof(UniversalAdditionalCameraData)
            .GetField("m_RendererIndex", BindingFlags.Instance | BindingFlags.NonPublic);

        return field == null ? "<field not found>" : field.GetValue(cameraData).ToString();
    }

    private static string DescribeNewScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var parts = new List<string>();
        foreach (var root in scene.GetRootGameObjects())
        {
            var camera = root.GetComponent<Camera>();
            var light = root.GetComponent<Light>();
            if (camera != null)
            {
                var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
                parts.Add($"camera '{camera.name}' ortho={camera.orthographic} " +
                          $"clearFlags={camera.clearFlags} " +
                          $"rendererIndex={ReadRendererIndex(cameraData)}");
            }
            else if (light != null)
            {
                parts.Add($"light '{root.name}' type={light.type}");
            }
            else
            {
                parts.Add($"object '{root.name}'");
            }
        }

        return string.Join(" | ", parts);
    }
}
