
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class TrackCollisionGenerator
{
    [MenuItem("Tools/Track/Generate Collision Mesh")]
    private static void GenerateCollisionMesh()
    {
        GameObject track = Selection.activeGameObject;

        if (!track)
        {
            Debug.LogError("Select the track root GameObject first.");
            return;
        }

        MeshFilter[] meshFilters =
            track.GetComponentsInChildren<MeshFilter>();

        List<CombineInstance> combineInstances = new();

        GameObject collisionObject = new GameObject("TrackCollision");
        collisionObject.transform.SetParent(track.transform, false);

        foreach (MeshFilter filter in meshFilters)
        {
            if (!filter.sharedMesh ||
                filter.transform == collisionObject.transform ||
                filter.transform.IsChildOf(collisionObject.transform))
                continue;

            CombineInstance instance = new CombineInstance
            {
                mesh = filter.sharedMesh,
                transform = collisionObject.transform.worldToLocalMatrix *
                            filter.transform.localToWorldMatrix
            };

            combineInstances.Add(instance);
        }

        if (combineInstances.Count == 0)
        {
            Object.DestroyImmediate(collisionObject);
            Debug.LogError("No meshes found in the selected track.");
            return;
        }

        Mesh combinedMesh = new Mesh
        {
            name = "TrackCollisionMesh",
            indexFormat = IndexFormat.UInt32
        };

        combinedMesh.CombineMeshes(
            combineInstances.ToArray(),
            true,
            true
        );

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Track Collision Mesh",
            "TrackCollisionMesh",
            "asset",
            "Choose where to save the collision mesh."
        );

        if (string.IsNullOrEmpty(path))
        {
            Object.DestroyImmediate(collisionObject);
            Object.DestroyImmediate(combinedMesh);
            return;
        }

        AssetDatabase.CreateAsset(combinedMesh, path);

        MeshCollider meshCollider =
            collisionObject.AddComponent<MeshCollider>();

        meshCollider.sharedMesh = combinedMesh;

        Undo.RegisterCreatedObjectUndo(
            collisionObject,
            "Generate Track Collision"
        );

        EditorUtility.SetDirty(track);

        Debug.Log($"Collision mesh generated: {path}");
    }
}
