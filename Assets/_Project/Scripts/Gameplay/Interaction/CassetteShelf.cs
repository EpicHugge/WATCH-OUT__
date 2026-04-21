using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CassetteShelf : MonoBehaviour
{
    [Serializable]
    public sealed class SlotDefinition
    {
        [SerializeField] private CassetteData cassetteData;
        [SerializeField] private CassetteShelfSlot.SlotMode state = CassetteShelfSlot.SlotMode.Normal;
        [SerializeField] private string overrideDisplayName = string.Empty;
        [SerializeField] private string overrideInteractionText = string.Empty;

        public CassetteData CassetteData
        {
            get => cassetteData;
            set => cassetteData = value;
        }

        public CassetteShelfSlot.SlotMode State
        {
            get => state;
            set => state = value;
        }

        public string OverrideDisplayName
        {
            get => overrideDisplayName;
            set => overrideDisplayName = value;
        }

        public string OverrideInteractionText
        {
            get => overrideInteractionText;
            set => overrideInteractionText = value;
        }

        public bool ShouldGenerate()
        {
            if (state == CassetteShelfSlot.SlotMode.Hidden)
            {
                return false;
            }

            return cassetteData != null ||
                   state == CassetteShelfSlot.SlotMode.WorkInProgress ||
                   state == CassetteShelfSlot.SlotMode.Locked;
        }

        public string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(overrideDisplayName))
            {
                return overrideDisplayName.Trim();
            }

            return cassetteData != null ? cassetteData.CassetteName : string.Empty;
        }
    }

    private const string DefaultCassettePrefabPath = "Assets/_Project/Prefabs/Interaction/Cassette/CasetteTape.prefab";
    private const string CassetteRootName = "CassetteRoot";

    [Header("References")]
    [SerializeField] private GameObject cassettePrefab;
    [SerializeField] private Transform cassetteRoot;
    [SerializeField] private CassettePlayerReceiver cassettePlayerReceiver;
    [SerializeField] private Transform placementAnchor;

    [Header("Layout")]
    [SerializeField] [Min(1)] private int columns = 4;
    [SerializeField] [Min(1)] private int rows = 2;
    [SerializeField] private Vector3 localStartPoint = Vector3.zero;
    [SerializeField] [Min(0f)] private float horizontalSpacing = 0.11f;
    [SerializeField] [Min(0f)] private float verticalSpacing = 0.14f;
    [SerializeField] private float depthOffset;
    [SerializeField] private Vector3 cassetteRotationEuler;

    [Header("Slots")]
    [SerializeField] private List<SlotDefinition> slots = new List<SlotDefinition>();

    public GameObject CassettePrefab => cassettePrefab;
    public Transform CassetteRoot => cassetteRoot;
    public int Columns => columns;
    public int Rows => rows;
    public int Capacity => Mathf.Max(1, columns) * Mathf.Max(1, rows);
    public IReadOnlyList<SlotDefinition> Slots => slots;

    private void Reset()
    {
#if UNITY_EDITOR
        if (cassettePrefab == null)
        {
            cassettePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultCassettePrefabPath);
        }
#endif

        EnsureCassetteRoot();
        SyncSlotsToGrid();
    }

    private void OnValidate()
    {
        columns = Mathf.Max(1, columns);
        rows = Mathf.Max(1, rows);
        horizontalSpacing = Mathf.Max(0f, horizontalSpacing);
        verticalSpacing = Mathf.Max(0f, verticalSpacing);
        EnsureCassetteRoot();
    }

    public void SyncSlotsToGrid()
    {
        int capacity = Capacity;
        while (slots.Count < capacity)
        {
            slots.Add(new SlotDefinition());
        }

        while (slots.Count > capacity)
        {
            slots.RemoveAt(slots.Count - 1);
        }
    }

    public void RebuildShelf()
    {
#if UNITY_EDITOR
        EnsureCassetteRoot();
        if (cassetteRoot == null)
        {
            return;
        }

        ClearGeneratedChildren();
        if (cassettePrefab == null)
        {
            return;
        }

        int maxIndex = Mathf.Min(Capacity, slots.Count);
        for (int index = 0; index < maxIndex; index++)
        {
            SlotDefinition slot = slots[index];
            if (slot == null || !slot.ShouldGenerate())
            {
                continue;
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(cassettePrefab, cassetteRoot) as GameObject;
            if (instance == null)
            {
                continue;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Rebuild Cassette Shelf");
            instance.transform.SetLocalPositionAndRotation(GetLocalPosition(index), GetLocalRotation());
            instance.transform.localScale = Vector3.one;
            instance.name = BuildCassetteName(index, slot);

            CassetteShelfSlot shelfSlot = instance.GetComponent<CassetteShelfSlot>();
            if (shelfSlot != null)
            {
                shelfSlot.ApplyShelfDefinition(
                    slot.CassetteData,
                    slot.State,
                    slot.OverrideDisplayName,
                    slot.OverrideInteractionText,
                    cassettePlayerReceiver);
                EditorUtility.SetDirty(shelfSlot);
            }

            HoverMoveInteractable hoverInteractable = instance.GetComponent<HoverMoveInteractable>();
            if (hoverInteractable != null)
            {
                EditorUtility.SetDirty(hoverInteractable);
            }

            EditorUtility.SetDirty(instance);
        }

        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(cassetteRoot.gameObject);
#endif
    }

    public void ClearGeneratedChildren()
    {
#if UNITY_EDITOR
        if (cassetteRoot == null)
        {
            return;
        }

        for (int childIndex = cassetteRoot.childCount - 1; childIndex >= 0; childIndex--)
        {
            Undo.DestroyObjectImmediate(cassetteRoot.GetChild(childIndex).gameObject);
        }
#endif
    }

    private void EnsureCassetteRoot()
    {
        if (cassetteRoot != null)
        {
            return;
        }

        Transform existingRoot = transform.Find(CassetteRootName);
        if (existingRoot != null)
        {
            cassetteRoot = existingRoot;
            return;
        }

        GameObject rootObject = new GameObject(CassetteRootName);
        rootObject.transform.SetParent(transform, false);
        cassetteRoot = rootObject.transform;
    }

    private Vector3 GetLocalPosition(int index)
    {
        int column = columns <= 0 ? 0 : index % columns;
        int row = columns <= 0 ? 0 : index / columns;

        Vector3 startPoint = placementAnchor != null ? placementAnchor.localPosition : localStartPoint;
        startPoint.z += depthOffset;

        return startPoint + new Vector3(column * horizontalSpacing, -row * verticalSpacing, 0f);
    }

    private Quaternion GetLocalRotation()
    {
        Quaternion baseRotation = placementAnchor != null ? placementAnchor.localRotation : Quaternion.identity;
        return baseRotation * Quaternion.Euler(cassetteRotationEuler);
    }

    private static string BuildCassetteName(int index, SlotDefinition slot)
    {
        string displayName = slot.ResolveDisplayName();
        string suffix = string.IsNullOrWhiteSpace(displayName) ? slot.State.ToString() : displayName;
        return $"Slot_{index + 1:00}_{suffix.Replace(' ', '_')}";
    }
}
