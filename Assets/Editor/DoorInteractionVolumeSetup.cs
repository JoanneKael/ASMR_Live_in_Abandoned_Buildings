using UnityEditor;
using UnityEngine;

/// <summary>
/// BP_Door_Inside* 문에 InteractionVolume을 생성하고
/// InteractableDoor를 Volume으로 이관합니다. (A방식)
/// </summary>
public static class DoorInteractionVolumeSetup
{
    private const string VolumeName = "InteractionVolume";
    private const string MenuPath = "Tools/DDNB/Setup Door InteractionVolumes";

    private static readonly int InteractableLayer = LayerMask.NameToLayer("Interactable");
    private static readonly int ObstacleLayer = LayerMask.NameToLayer("Obstacle");

    [MenuItem(MenuPath)]
    public static void SetupAll()
    {
        if (InteractableLayer < 0 || ObstacleLayer < 0)
        {
            EditorUtility.DisplayDialog(
                "Door Setup",
                "Interactable / Obstacle 레이어를 찾을 수 없습니다.",
                "OK");
            return;
        }

        InteractableDoor[] doors = Object.FindObjectsByType<InteractableDoor>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        int setupCount = 0;
        int skipCount = 0;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup Door InteractionVolumes");

        foreach (InteractableDoor door in doors)
        {
            if (door == null) continue;

            Transform root = FindDoorRoot(door.transform);
            if (root == null || !root.name.StartsWith("BP_Door_Inside"))
            {
                skipCount++;
                continue;
            }

            if (SetupOne(root, door))
                setupCount++;
            else
                skipCount++;
        }

        Undo.CollapseUndoOperations(undoGroup);

        EditorUtility.DisplayDialog(
            "Door Setup",
            $"완료\n설정됨: {setupCount}\n스킵: {skipCount}\n\n씬을 저장하세요 (Ctrl+S).",
            "OK");

        Debug.Log($"[DoorInteractionVolumeSetup] setup={setupCount}, skip={skipCount}");
    }

    private static Transform FindDoorRoot(Transform t)
    {
        Transform cur = t;
        while (cur != null)
        {
            if (cur.name.StartsWith("BP_Door_Inside"))
                return cur;
            cur = cur.parent;
        }
        return t.parent != null ? t.parent : t;
    }

    private static bool SetupOne(Transform root, InteractableDoor existingDoor)
    {
        // 이미 Volume에 스크립트가 있으면 doorMesh만 보정
        if (existingDoor.gameObject.name == VolumeName)
        {
            EnsureDoorMeshAssigned(existingDoor, FindDoorMesh(root, existingDoor.transform));
            return true;
        }

        Transform doorMesh = FindDoorMesh(root, existingDoor.transform);
        if (doorMesh == null)
        {
            Debug.LogWarning($"[DoorSetup] SM_Door를 찾지 못함: {root.name}", root);
            return false;
        }

        // 기존 값 백업
        SerializedObject oldSo = new SerializedObject(existingDoor);
        InteractableData data = oldSo.FindProperty("data").objectReferenceValue as InteractableData;
        int inputMode = oldSo.FindProperty("inputMode").enumValueIndex;
        float closedAngle = oldSo.FindProperty("closedAngle").floatValue;
        float openAngle = oldSo.FindProperty("openAngle").floatValue;

        // InteractionVolume 확보
        Transform volumeTf = root.Find(VolumeName);
        GameObject volumeGo;
        if (volumeTf == null)
        {
            volumeGo = new GameObject(VolumeName);
            Undo.RegisterCreatedObjectUndo(volumeGo, "Create InteractionVolume");
            Undo.SetTransformParent(volumeGo.transform, root, "Parent InteractionVolume");
        }
        else
        {
            volumeGo = volumeTf.gameObject;
        }

        // 닫힌 문 기준으로 Volume 위치/콜라이더 맞춤
        PlaceVolumeLikeClosedDoor(volumeGo.transform, doorMesh, closedAngle);

        BoxCollider srcCol = doorMesh.GetComponent<BoxCollider>();
        BoxCollider volumeCol = volumeGo.GetComponent<BoxCollider>();
        if (volumeCol == null)
            volumeCol = Undo.AddComponent<BoxCollider>(volumeGo);

        Undo.RecordObject(volumeCol, "Configure InteractionVolume Collider");
        if (srcCol != null)
        {
            volumeCol.center = srcCol.center;
            volumeCol.size = srcCol.size;
        }
        else
        {
            volumeCol.center = new Vector3(0.475f, -0.675f, 0f);
            volumeCol.size = new Vector3(0.97f, 2.11f, 0.14f);
        }
        volumeCol.isTrigger = true;

        // 레이어
        Undo.RecordObject(volumeGo, "Set InteractionVolume Layer");
        volumeGo.layer = InteractableLayer;

        Undo.RecordObject(doorMesh.gameObject, "Set SM_Door Layer");
        doorMesh.gameObject.layer = ObstacleLayer;

        // 스크립트를 Volume으로 이관
        InteractableDoor onVolume = volumeGo.GetComponent<InteractableDoor>();
        if (onVolume == null)
            onVolume = Undo.AddComponent<InteractableDoor>(volumeGo);

        SerializedObject newSo = new SerializedObject(onVolume);
        newSo.FindProperty("data").objectReferenceValue = data;
        newSo.FindProperty("inputMode").enumValueIndex = inputMode;
        newSo.FindProperty("closedAngle").floatValue = closedAngle;
        newSo.FindProperty("openAngle").floatValue = openAngle;
        newSo.FindProperty("doorMesh").objectReferenceValue = doorMesh;
        newSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(onVolume);

        // SM_Door에 남아 있던 스크립트 제거
        if (existingDoor != null && existingDoor.gameObject != volumeGo)
            Undo.DestroyObjectImmediate(existingDoor);

        EditorUtility.SetDirty(volumeGo);
        EditorUtility.SetDirty(doorMesh.gameObject);
        EditorUtility.SetDirty(root.gameObject);

        Debug.Log($"[DoorSetup] OK: {root.name}", root);
        return true;
    }

    private static Transform FindDoorMesh(Transform root, Transform prefer)
    {
        if (prefer != null && prefer != root && prefer.name != VolumeName)
        {
            if (prefer.GetComponent<MeshFilter>() != null || prefer.name.StartsWith("SM_Door"))
                return prefer;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == VolumeName) continue;
            if (child.name.StartsWith("SM_Door") || child.GetComponent<MeshFilter>() != null)
                return child;
        }

        return null;
    }

    private static void PlaceVolumeLikeClosedDoor(Transform volume, Transform doorMesh, float closedAngle)
    {
        Undo.RecordObject(volume, "Place InteractionVolume");
        volume.localPosition = doorMesh.localPosition;
        volume.localRotation = Quaternion.Euler(0f, closedAngle, 0f);
        volume.localScale = Vector3.one;
    }

    private static void EnsureDoorMeshAssigned(InteractableDoor door, Transform mesh)
    {
        if (mesh == null) return;
        SerializedObject so = new SerializedObject(door);
        SerializedProperty prop = so.FindProperty("doorMesh");
        if (prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = mesh;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(door);
        }
    }
}
