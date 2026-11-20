using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// Temporary discovery: locates the editor entry point that the "Assets &gt; Create &gt; Material"
/// menu uses, so the material-default validation can exercise the real workflow.
public static class DiscoverMaterialCreation
{
    private static readonly List<string> Report = new();

    public static void Run()
    {
        Report.Clear();

        try
        {
            Report.Add("=== types exposing a default-material factory ===");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!assembly.GetName().Name.StartsWith("UnityEditor"))
                {
                    continue;
                }

                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    types = exception.Types.Where(type => type != null).ToArray();
                }

                foreach (var type in types)
                {
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                          BindingFlags.Static | BindingFlags.DeclaredOnly))
                    {
                        if (method.ReturnType != typeof(Material) || method.GetParameters().Length > 1)
                        {
                            continue;
                        }

                        if (!method.Name.Contains("Material") && !method.Name.Contains("Default"))
                        {
                            continue;
                        }

                        Report.Add($"  {type.FullName}.{method.Name}(" +
                                   $"{string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name))})");
                    }
                }
            }

            Report.Add("=== RenderPipelineAsset.defaultShader (base) ===");
            var property = typeof(RenderPipelineAsset).GetProperty("defaultShader");
            Report.Add($"  declared on {property?.DeclaringType?.FullName}, canRead={property?.CanRead}");

            Report.Add("=== current default shader resolution ===");
            Report.Add($"  GraphicsSettings.currentRenderPipeline = " +
                       $"{GraphicsSettings.currentRenderPipeline?.name ?? "<null>"}");
            Report.Add($"  defaultShader = " +
                       $"{GraphicsSettings.currentRenderPipeline?.defaultShader?.name ?? "<null>"}");
        }
        catch (Exception exception)
        {
            Report.Add("EXCEPTION: " + exception);
        }

        File.WriteAllLines(Path.Combine(Application.dataPath, "..", "Logs", "material_creation_report.txt"), Report);
        Debug.Log("DISCOVERY: report written.");
    }
}
