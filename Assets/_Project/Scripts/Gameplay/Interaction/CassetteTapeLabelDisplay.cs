using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class CassetteTapeLabelDisplay : MonoBehaviour
{
    private const string DefaultLabelFontResourcePath = "Fonts/ChillaxsHand-Regular";
    private const string LabelObjectName = "CassetteLabelText";
    private const float DefaultTextOffsetZ = -0.001f;
    private static readonly Vector3 DefaultLabelOffset = Vector3.zero;
    private static readonly Vector3 DefaultTextScale = new Vector3(30f, 100f, 1f);

    [Serializable]
    private sealed class LabelFace
    {
        public string planeName;
        public Transform planeTransform;
        public TextMesh text;
        public Vector3 localOffset = new Vector3(0f, 0f, -0.001f);
        public Vector3 localEulerAngles = new Vector3(90f, 180f, 0f);
    }

    [Header("References")]
    [SerializeField] private CassetteShelfSlot cassetteShelfSlot;
    [SerializeField] private Font labelFont;

    [Header("Text")]
    [SerializeField] private string overrideLabelText = string.Empty;
    [SerializeField] private string editorPreviewText = "REMEMBER";
    [SerializeField] [Min(1)] private int fontSize = 72;
    [SerializeField] [Min(0.001f)] private float characterSize = 0.04f;
    [SerializeField] private float lineSpacing = 0.9f;
    [SerializeField] [Range(0.5f, 1f)] private float planeFill = 0.94f;
    [SerializeField] [Range(0f, 0.4f)] private float horizontalPaddingPercent = 0.06f;
    [SerializeField] [Range(0f, 0.4f)] private float verticalPaddingPercent = 0.1f;
    [SerializeField] [Min(1)] private int maxLines = 3;
    [SerializeField] private Color textColor = new Color32(0x07, 0x07, 0x06, 0xFF);

    [Header("Label Planes")]
    [SerializeField] private LabelFace[] labelFaces = Array.Empty<LabelFace>();

    private Material depthTestLabelMaterial;
    private bool ownsDepthTestLabelMaterial;
    private bool isDisplayVisible = true;

    private void Reset()
    {
        EnsureDefaultFaces();
        RefreshLabel();
    }

    private void Awake()
    {
        RefreshLabel();
    }

    private void Start()
    {
        RefreshLabel();
    }

    private void OnEnable()
    {
        RefreshLabel();
    }

    private void LateUpdate()
    {
        UpdateFaceVisibility();
    }

    private void OnDestroy()
    {
        if (depthTestLabelMaterial == null)
        {
            return;
        }

        if (!ownsDepthTestLabelMaterial)
        {
            depthTestLabelMaterial = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(depthTestLabelMaterial);
        }
        else
        {
            DestroyImmediate(depthTestLabelMaterial);
        }
    }

    private void OnValidate()
    {
        fontSize = Mathf.Max(1, fontSize);
        characterSize = Mathf.Max(0.001f, characterSize);
        maxLines = Mathf.Max(1, maxLines);
        planeFill = Mathf.Clamp(planeFill, 0.5f, 1f);
        horizontalPaddingPercent = Mathf.Clamp(horizontalPaddingPercent, 0f, 0.4f);
        verticalPaddingPercent = Mathf.Clamp(verticalPaddingPercent, 0f, 0.4f);
        RefreshLabel();
    }

    [ContextMenu("Refresh Cassette Labels")]
    public void RefreshLabel()
    {
        ResolveReferences();
        EnsureDefaultFaces();
        EnsureFont();

        string rawLabelText = ResolveLabelText();
        for (int i = 0; i < labelFaces.Length; i++)
        {
            LabelFace face = labelFaces[i];
            if (face == null)
            {
                continue;
            }

            EnsureFaceText(face);
            if (face.text == null)
            {
                continue;
            }

            ConfigureText(face.text, face);
            ApplyBestFittingText(face, rawLabelText);
        }

        UpdateFaceVisibility();
    }

    public void SetDisplayVisible(bool isVisible)
    {
        isDisplayVisible = isVisible;
        UpdateFaceVisibility();
    }

    private void ResolveReferences()
    {
        if (cassetteShelfSlot == null)
        {
            cassetteShelfSlot = GetComponent<CassetteShelfSlot>();
        }
    }

    private void EnsureDefaultFaces()
    {
        if (labelFaces == null || labelFaces.Length == 0)
        {
            labelFaces = new[]
            {
                CreateFace("TapeMain", DefaultLabelOffset, new Vector3(90f, 180f, 0f)),
                CreateFace("TapeMain(1)", DefaultLabelOffset, new Vector3(90f, 0f, 0f)),
                CreateFace("TapeSide", DefaultLabelOffset, new Vector3(90f, 180f, 0f)),
                CreateFace("TapeSide(1)", DefaultLabelOffset, new Vector3(90f, 180f, 0f))
            };
        }

        for (int i = 0; i < labelFaces.Length; i++)
        {
            LabelFace face = labelFaces[i];
            if (face == null)
            {
                continue;
            }

            if (face.planeTransform == null && !string.IsNullOrWhiteSpace(face.planeName))
            {
                Transform planeTransform = transform.Find(face.planeName);
                if (planeTransform != null)
                {
                    face.planeTransform = planeTransform;
                }
            }
        }
    }

    private void EnsureFont()
    {
        if (labelFont != null)
        {
            return;
        }

        labelFont = Resources.Load<Font>(DefaultLabelFontResourcePath);
    }

    private void EnsureFaceText(LabelFace face)
    {
        if (face.planeTransform == null)
        {
            return;
        }

        if (face.text == null)
        {
            Transform existingTransform = face.planeTransform.Find(LabelObjectName);
            if (existingTransform == null)
            {
                GameObject textObject = new GameObject(LabelObjectName, typeof(TextMesh));
                textObject.transform.SetParent(face.planeTransform, false);
                existingTransform = textObject.transform;
            }

            face.text = existingTransform.GetComponent<TextMesh>();
        }
    }

    private void ConfigureText(TextMesh textMesh, LabelFace face)
    {
        Transform textTransform = textMesh.transform;
        textTransform.localPosition = face.localOffset;
        textTransform.localRotation = Quaternion.Euler(face.localEulerAngles);
        textTransform.localScale = DefaultTextScale;

        if (labelFont != null)
        {
            textMesh.font = labelFont;
        }

        textMesh.offsetZ = DefaultTextOffsetZ;

        MeshRenderer meshRenderer = textMesh.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.sharedMaterial = GetDepthTestLabelMaterial();
        }

        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontSize = fontSize;
        textMesh.characterSize = characterSize;
        textMesh.lineSpacing = lineSpacing;
        textMesh.color = textColor;
        textMesh.richText = false;
        textMesh.fontStyle = FontStyle.Bold;
    }

    private string ResolveLabelText()
    {
        if (!string.IsNullOrWhiteSpace(overrideLabelText))
        {
            return overrideLabelText.Trim();
        }

        if (cassetteShelfSlot != null)
        {
            string displayName = cassetteShelfSlot.ResolveDisplayName();
            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }
        }

        return Application.isPlaying ? string.Empty : editorPreviewText;
    }

    private void ApplyBestFittingText(LabelFace face, string rawLabelText)
    {
        if (face.text == null)
        {
            return;
        }

        string sanitizedText = SanitizeLabelText(rawLabelText);
        bool hasVisibleText = !string.IsNullOrWhiteSpace(sanitizedText);
        face.text.gameObject.SetActive(hasVisibleText);
        if (!hasVisibleText)
        {
            return;
        }

        string bestText = sanitizedText;
        float bestScore = float.MinValue;
        float bestCharacterSize = characterSize;
        float bestXScale = DefaultTextScale.x;
        List<string> candidates = BuildLayoutCandidates(sanitizedText);

        face.text.transform.localScale = DefaultTextScale;
        for (int i = 0; i < candidates.Count; i++)
        {
            string candidate = candidates[i];
            face.text.text = candidate;
            face.text.characterSize = characterSize;

            if (!TryGetPlaneSurfaceSize(face, out Vector2 planeSurfaceSize) ||
                !TryGetTextSurfaceSize(face, out Vector2 textSurfaceSize))
            {
                continue;
            }

            float heightFit = planeSurfaceSize.y <= 0f
                ? 1f
                : Mathf.Min(1f, (planeSurfaceSize.y * planeFill) / textSurfaceSize.y);

            float fittedCharacterSize = characterSize * heightFit;
            float fittedWidth = textSurfaceSize.x * heightFit;
            if (fittedWidth <= 0f)
            {
                continue;
            }

            float fittedXScale = DefaultTextScale.x * ((planeSurfaceSize.x * planeFill) / fittedWidth);
            float widthFill = planeSurfaceSize.x <= 0f ? 0f : Mathf.Clamp01((fittedWidth * (fittedXScale / DefaultTextScale.x)) / planeSurfaceSize.x);
            float heightFill = planeSurfaceSize.y <= 0f ? 0f : Mathf.Clamp01((textSurfaceSize.y * heightFit) / planeSurfaceSize.y);
            float fillScore = (widthFill * 0.7f) + (heightFill * 0.3f);

            if (fillScore > bestScore)
            {
                bestScore = fillScore;
                bestText = candidate;
                bestCharacterSize = fittedCharacterSize;
                bestXScale = fittedXScale;
            }
        }

        face.text.text = bestText;
        face.text.transform.localScale = new Vector3(bestXScale, DefaultTextScale.y, DefaultTextScale.z);
        face.text.characterSize = bestCharacterSize;
        UpdateFaceVisibility();
    }

    private string SanitizeLabelText(string labelText)
    {
        return string.IsNullOrWhiteSpace(labelText) ? string.Empty : labelText.Trim();
    }

    private List<string> BuildLayoutCandidates(string labelText)
    {
        List<string> candidates = new List<string>();
        if (string.IsNullOrWhiteSpace(labelText))
        {
            return candidates;
        }

        candidates.Add(labelText);

        string[] words = labelText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1)
        {
            return candidates;
        }

        List<int> breaks = new List<int>();
        BuildLayoutCandidatesRecursive(words, 0, maxLines, breaks, candidates);
        return candidates;
    }

    private void BuildLayoutCandidatesRecursive(
        string[] words,
        int startIndex,
        int remainingLines,
        List<int> breaks,
        List<string> candidates)
    {
        if (words == null || candidates == null || remainingLines <= 0)
        {
            return;
        }

        if (startIndex >= words.Length)
        {
            string candidate = BuildCandidateText(words, breaks);
            if (!string.IsNullOrWhiteSpace(candidate) && !candidates.Contains(candidate))
            {
                candidates.Add(candidate);
            }

            return;
        }

        if (remainingLines == 1)
        {
            string candidate = BuildCandidateText(words, breaks);
            if (!string.IsNullOrWhiteSpace(candidate) && !candidates.Contains(candidate))
            {
                candidates.Add(candidate);
            }

            return;
        }

        for (int nextBreak = startIndex + 1; nextBreak < words.Length; nextBreak++)
        {
            breaks.Add(nextBreak);
            BuildLayoutCandidatesRecursive(words, nextBreak, remainingLines - 1, breaks, candidates);
            breaks.RemoveAt(breaks.Count - 1);
        }
    }

    private string BuildCandidateText(string[] words, List<int> breaks)
    {
        if (words == null || words.Length == 0)
        {
            return string.Empty;
        }

        HashSet<int> breakSet = breaks != null ? new HashSet<int>(breaks) : null;
        List<string> lines = new List<string>();
        List<string> currentLineWords = new List<string>();

        for (int i = 0; i < words.Length; i++)
        {
            if (breakSet != null && breakSet.Contains(i) && currentLineWords.Count > 0)
            {
                lines.Add(string.Join(" ", currentLineWords));
                currentLineWords.Clear();
            }

            currentLineWords.Add(words[i]);
        }

        if (currentLineWords.Count > 0)
        {
            lines.Add(string.Join(" ", currentLineWords));
        }

        return string.Join("\n", lines);
    }

    private bool TryGetPlaneSurfaceSize(LabelFace face, out Vector2 planeSurfaceSize)
    {
        planeSurfaceSize = Vector2.zero;
        if (face == null || face.planeTransform == null)
        {
            return false;
        }

        MeshFilter meshFilter = face.planeTransform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        GetSurfaceAxes(meshSize, out int axisA, out int axisB);

        float surfaceWidth = Mathf.Abs(GetAxisValue(meshSize, axisA));
        float surfaceHeight = Mathf.Abs(GetAxisValue(meshSize, axisB));

        planeSurfaceSize = new Vector2(
            surfaceWidth * Mathf.Max(0.01f, 1f - (horizontalPaddingPercent * 2f)),
            surfaceHeight * Mathf.Max(0.01f, 1f - (verticalPaddingPercent * 2f)));

        return planeSurfaceSize.x > 0f && planeSurfaceSize.y > 0f;
    }

    private bool TryGetTextSurfaceSize(LabelFace face, out Vector2 textSurfaceSize)
    {
        textSurfaceSize = Vector2.zero;
        if (face == null || face.text == null || face.planeTransform == null)
        {
            return false;
        }

        Renderer textRenderer = face.text.GetComponent<Renderer>();
        if (textRenderer == null)
        {
            return false;
        }

        Bounds worldBounds = textRenderer.bounds;
        if (worldBounds.size.sqrMagnitude <= 0f)
        {
            return false;
        }

        MeshFilter meshFilter = face.planeTransform.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return false;
        }

        Vector3 meshSize = meshFilter.sharedMesh.bounds.size;
        GetSurfaceAxes(meshSize, out int axisA, out int axisB);

        Vector3 center = worldBounds.center;
        Vector3 extents = worldBounds.extents;
        Vector3[] corners =
        {
            center + new Vector3(-extents.x, -extents.y, -extents.z),
            center + new Vector3(-extents.x, -extents.y, extents.z),
            center + new Vector3(-extents.x, extents.y, -extents.z),
            center + new Vector3(-extents.x, extents.y, extents.z),
            center + new Vector3(extents.x, -extents.y, -extents.z),
            center + new Vector3(extents.x, -extents.y, extents.z),
            center + new Vector3(extents.x, extents.y, -extents.z),
            center + new Vector3(extents.x, extents.y, extents.z)
        };

        float minA = float.MaxValue;
        float maxA = float.MinValue;
        float minB = float.MaxValue;
        float maxB = float.MinValue;

        for (int i = 0; i < corners.Length; i++)
        {
            Vector3 localPoint = face.planeTransform.InverseTransformPoint(corners[i]);
            float a = GetAxisValue(localPoint, axisA);
            float b = GetAxisValue(localPoint, axisB);

            minA = Mathf.Min(minA, a);
            maxA = Mathf.Max(maxA, a);
            minB = Mathf.Min(minB, b);
            maxB = Mathf.Max(maxB, b);
        }

        textSurfaceSize = new Vector2(maxA - minA, maxB - minB);
        return textSurfaceSize.x > 0f && textSurfaceSize.y > 0f;
    }

    private Material GetDepthTestLabelMaterial()
    {
        if (depthTestLabelMaterial != null)
        {
            return depthTestLabelMaterial;
        }

        Material fontMaterial = labelFont != null ? labelFont.material : null;
        Texture mainTexture = fontMaterial != null ? fontMaterial.mainTexture : null;

        Shader shader = Shader.Find("Unlit/Transparent");
        if (shader == null && fontMaterial != null)
        {
            depthTestLabelMaterial = fontMaterial;
            ownsDepthTestLabelMaterial = false;
            return depthTestLabelMaterial;
        }

        if (shader == null)
        {
            return fontMaterial;
        }

        depthTestLabelMaterial = new Material(shader)
        {
            name = "CassetteLabelDepthTestMaterial",
            hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild
        };
        ownsDepthTestLabelMaterial = true;

        if (mainTexture != null)
        {
            depthTestLabelMaterial.mainTexture = mainTexture;
        }

        depthTestLabelMaterial.color = Color.white;
        return depthTestLabelMaterial;
    }

    private void GetSurfaceAxes(Vector3 meshSize, out int axisA, out int axisB)
    {
        float x = Mathf.Abs(meshSize.x);
        float y = Mathf.Abs(meshSize.y);
        float z = Mathf.Abs(meshSize.z);

        if (x <= y && x <= z)
        {
            axisA = 1;
            axisB = 2;
            return;
        }

        if (y <= x && y <= z)
        {
            axisA = 0;
            axisB = 2;
            return;
        }

        axisA = 0;
        axisB = 1;
    }

    private float GetAxisValue(Vector3 vector, int axisIndex)
    {
        switch (axisIndex)
        {
            case 0:
                return vector.x;
            case 1:
                return vector.y;
            default:
                return vector.z;
        }
    }

    private void UpdateFaceVisibility()
    {
        Camera activeCamera = Camera.main;

        for (int i = 0; i < labelFaces.Length; i++)
        {
            LabelFace face = labelFaces[i];
            if (face == null || face.text == null)
            {
                continue;
            }

            Renderer textRenderer = face.text.GetComponent<Renderer>();
            if (textRenderer == null)
            {
                continue;
            }

            if (!isDisplayVisible)
            {
                textRenderer.enabled = false;
                continue;
            }

            bool hasVisibleText = face.text.gameObject.activeSelf && !string.IsNullOrWhiteSpace(face.text.text);
            if (!hasVisibleText)
            {
                textRenderer.enabled = false;
                continue;
            }

            if (activeCamera == null || face.planeTransform == null)
            {
                textRenderer.enabled = true;
                continue;
            }

            Vector3 planeNormal = face.planeTransform.up.normalized;
            Vector3 toCamera = (activeCamera.transform.position - face.planeTransform.position).normalized;
            textRenderer.enabled = Vector3.Dot(planeNormal, toCamera) > 0f;
        }
    }

    private static LabelFace CreateFace(string planeName, Vector3 localOffset, Vector3 localEulerAngles)
    {
        return new LabelFace
        {
            planeName = planeName,
            localOffset = localOffset,
            localEulerAngles = localEulerAngles
        };
    }
}
