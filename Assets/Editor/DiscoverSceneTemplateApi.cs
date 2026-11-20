using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class DiscoverSceneTemplateApi
{
    private static readonly List<string> Report = new();

    public static void Run()
    {
        Report.Clear();
        try
        {
            var module = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(assembly => assembly.GetName().Name == "UnityEditor.SceneTemplateModule");

            Report.Add("=== UniversalAdditionalCameraData attributes ===");
            var cameraDataType = typeof(UnityEngine.Rendering.Universal.UniversalAdditionalCameraData);
            foreach (var attribute in cameraDataType.GetCustomAttributes(false))
            {
                Report.Add($"  {attribute}");
            }

            Report.Add("=== SceneTemplateInfo fields ===");
            var infoType = module?.GetType("UnityEditor.SceneTemplate.SceneTemplateInfo");
            if (infoType != null)
            {
                foreach (var field in infoType.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                      BindingFlags.Instance | BindingFlags.Static))
                {
                    Report.Add($"  {field.FieldType.Name} {field.Name} public={field.IsPublic}");
                }
            }

            Report.Add("=== SceneTemplateUtils default template infos ===");
            var utilsType = module?.GetType("UnityEditor.SceneTemplate.SceneTemplateUtils");
            if (utilsType != null)
            {
                foreach (var field in utilsType.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                          BindingFlags.Static))
                {
                    if (!field.Name.Contains("default") && !field.Name.Contains("builtin"))
                    {
                        continue;
                    }

                    object value = null;
                    try
                    {
                        value = field.GetValue(null);
                    }
                    catch (Exception exception)
                    {
                        Report.Add($"  {field.Name} -> ERROR {exception.GetType().Name}");
                        continue;
                    }

                    Report.Add($"  {field.Name} -> {Describe(value)}");
                }
            }

            Report.Add("=== SceneTemplatePreferences ===");
            var preferencesType = module?.GetType("UnityEditor.SceneTemplate.SceneTemplatePreferences");
            if (preferencesType != null)
            {
                foreach (var member in preferencesType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic |
                                                                BindingFlags.Instance | BindingFlags.Static |
                                                                BindingFlags.DeclaredOnly))
                {
                    Report.Add($"  {member.MemberType} {member}");
                }
            }

            Report.Add("=== GraphicsSettings.renderPipelineAsset type ===");
            var pipelineProperty = typeof(UnityEngine.Rendering.GraphicsSettings)
                .GetProperty("renderPipelineAsset");
            Report.Add($"  {pipelineProperty?.PropertyType.FullName} canWrite={pipelineProperty?.CanWrite}");
        }
        catch (Exception exception)
        {
            Report.Add("EXCEPTION: " + exception);
        }

        File.WriteAllLines(Path.Combine(Application.dataPath, "..", "Logs", "template_api_report.txt"), Report);
        Debug.Log("DISCOVERY: report written.");
    }

    private static string Describe(object value)
    {
        if (value == null)
        {
            return "<null>";
        }

        var type = value.GetType();
        var parts = new List<string>();
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            object fieldValue = null;
            try
            {
                fieldValue = field.GetValue(value);
            }
            catch
            {
                continue;
            }

            if (fieldValue is string text || fieldValue is int || fieldValue is bool)
            {
                parts.Add($"{field.Name}={fieldValue}");
            }
        }

        return $"{type.Name}({string.Join(", ", parts)})";
    }
}
