using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// Temporary configuration script: moves the project-wide defaults from the 2D template to
/// 3D + URP, while keeping the 2D renderer available for the scene that depends on it.
public static class Urp3DConfigure
{
    private const string PipelineAssetPath = "Assets/Settings/UniversalRP.asset";
    private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
    private const string Renderer3DPath = "Assets/Settings/Renderer3D.asset";
    private const string TemplateScenePath = "Assets/Settings/Scenes/URP3DSceneTemplate.unity";
    private const string TemplatePath = "Assets/Settings/URP3DSceneTemplate.scenetemplate";
    private const string Template2DPath = "Assets/Settings/Lit2DSceneTemplate.scenetemplate";

    private const int Renderer2DIndex = 0;
    private const int Renderer3DIndex = 1;

    private static readonly List<string> Report = new();

    public static void Run()
    {
        Report.Clear();

        try
        {
            var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelineAssetPath);
            if (pipelineAsset == null)
            {
                throw new Exception($"Pipeline asset not found at {PipelineAssetPath}");
            }

            var renderer2D = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(Renderer2DPath);
            if (renderer2D == null)
            {
                throw new Exception($"2D renderer not found at {Renderer2DPath}");
            }

            var renderer3D = CreateOrLoadRenderer3D();
            ConfigureRendererList(pipelineAsset, renderer2D, renderer3D);

            // Project-wide Graphics setting: keep URP as the fallback so a quality level without
            // an override cannot fall back to the built-in pipeline.
            GraphicsSettings.defaultRenderPipeline = pipelineAsset;

            // Project-wide 2D/3D behaviour mode: 3D defaults for newly created content.
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode3D;

            CreateTemplateScene();
            ConfigureTemplates();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        catch (Exception exception)
        {
            Report.Add("EXCEPTION: " + exception);
        }

        File.WriteAllLines(Path.Combine(Application.dataPath, "..", "Logs", "urp3d_configure.txt"), Report);
        Debug.Log("CONFIGURE: report written.");
    }

    private static void ConfigureRendererList(
        UniversalRenderPipelineAsset pipelineAsset,
        ScriptableRendererData renderer2D,
        ScriptableRendererData renderer3D)
    {
        var serialized = new SerializedObject(pipelineAsset);
        var list = serialized.FindProperty("m_RendererDataList");
        var defaultIndex = serialized.FindProperty("m_DefaultRendererIndex");

        while (list.arraySize > 2)
        {
            list.DeleteArrayElementAtIndex(list.arraySize - 1);
        }

        if (list.arraySize < 2)
        {
            list.arraySize = 2;
        }

        list.GetArrayElementAtIndex(Renderer2DIndex).objectReferenceValue = renderer2D;
        list.GetArrayElementAtIndex(Renderer3DIndex).objectReferenceValue = renderer3D;
        defaultIndex.intValue = Renderer3DIndex;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipelineAsset);

        Report.Add($"  renderer count = {list.arraySize}, default index = {defaultIndex.intValue}");
        Report.Add($"  [0] {renderer2D.name}, [1] {renderer3D.name}");
        Report.Add($"  defaultShader = {pipelineAsset.defaultShader?.name ?? "<null>"}");
        Report.Add($"  defaultMaterial = {pipelineAsset.defaultMaterial?.name ?? "<null>"}");
    }

    private static ScriptableRendererData CreateOrLoadRenderer3D()
    {
        var existing = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(Renderer3DPath);
        if (existing != null)
        {
            return existing;
        }

        // Same factory used by "Assets/Create/Rendering/URP Universal Renderer".
        var createRendererAsset = typeof(UniversalRenderPipelineAsset)
            .GetMethod("CreateRendererAsset", BindingFlags.NonPublic | BindingFlags.Static);
        if (createRendererAsset == null)
        {
            throw new Exception("UniversalRenderPipelineAsset.CreateRendererAsset not found");
        }

        return (ScriptableRendererData)createRendererAsset.Invoke(
            null,
            new object[] { Renderer3DPath, RendererType.UniversalRenderer, false, "Renderer" });
    }

    private static void CreateTemplateScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath) != null)
        {
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraObject.transform.position = new Vector3(0f, 1f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = false;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 0f);
        camera.nearClipPlane = 0.3f;
        camera.farClipPlane = 1000f;
        camera.fieldOfView = 60f;
        cameraObject.AddComponent<UniversalAdditionalCameraData>();

        var lightObject = new GameObject("Directional Light");
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        light.color = Color.white;
        light.shadows = LightShadows.Soft;

        EditorSceneManager.SaveScene(scene, TemplateScenePath);
    }

    private static void ConfigureTemplates()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(TemplateScenePath);
        if (sceneAsset == null)
        {
            throw new Exception($"Template scene not found at {TemplateScenePath}");
        }

        var template = AssetDatabase.LoadAssetAtPath<UnityEditor.SceneTemplate.SceneTemplateAsset>(TemplatePath);
        if (template == null)
        {
            template = UnityEditor.SceneTemplate.SceneTemplateService
                .CreateTemplateFromScene(sceneAsset, TemplatePath);
        }

        template.templateName = "Basic 3D (URP)";
        template.description = "Contains a perspective camera and a directional light. " +
                               "Uses the Universal 3D Renderer.";
        template.addToDefaults = true;
        EditorUtility.SetDirty(template);

        var template2D = AssetDatabase
            .LoadAssetAtPath<UnityEditor.SceneTemplate.SceneTemplateAsset>(Template2DPath);
        if (template2D != null)
        {
            template2D.addToDefaults = false;
            EditorUtility.SetDirty(template2D);
        }
    }
}
