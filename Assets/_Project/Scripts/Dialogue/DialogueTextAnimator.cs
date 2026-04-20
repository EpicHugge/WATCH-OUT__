using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DialogueTextAnimator : MonoBehaviour
{
    private TMP_Text targetLabel;
    private DialogueTextEffect activeEffect = DialogueTextEffect.None;
    private DialogueTextEffectSpan[] inlineEffectSpans = System.Array.Empty<DialogueTextEffectSpan>();
    private TMP_MeshInfo[] cachedMeshInfo;
    private bool restoreMesh;

    public void SetTarget(TMP_Text textLabel)
    {
        if (targetLabel == textLabel)
        {
            return;
        }

        RestoreBaseMesh();
        targetLabel = textLabel;
        RefreshCachedMesh();
    }

    public void SetEffect(DialogueTextEffect textEffect)
    {
        if (activeEffect == textEffect)
        {
            return;
        }

        RestoreBaseMesh();
        activeEffect = textEffect;
        RefreshCachedMesh();
    }

    public void SetInlineEffects(DialogueTextEffectSpan[] effectSpans)
    {
        DialogueTextEffectSpan[] nextSpans = effectSpans ?? System.Array.Empty<DialogueTextEffectSpan>();
        if (ReferenceEquals(inlineEffectSpans, nextSpans))
        {
            return;
        }

        RestoreBaseMesh();
        inlineEffectSpans = nextSpans;
        RefreshCachedMesh();
    }

    public void RefreshCachedMesh()
    {
        if (targetLabel == null)
        {
            cachedMeshInfo = null;
            return;
        }

        targetLabel.ForceMeshUpdate();
        cachedMeshInfo = targetLabel.textInfo.CopyMeshInfoVertexData();
        restoreMesh = false;
    }

    private void OnDisable()
    {
        RestoreBaseMesh();
    }

    private void RestoreVisibleVertices(TMP_TextInfo textInfo, int visibleCharacterCount)
    {
        for (int i = 0; i < visibleCharacterCount; i++)
        {
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible)
            {
                continue;
            }

            int materialIndex = characterInfo.materialReferenceIndex;
            int vertexIndex = characterInfo.vertexIndex;
            Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;
            Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;
            destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0];
            destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1];
            destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2];
            destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3];
        }
    }

    private void RestoreBaseMesh()
    {
        if (!restoreMesh || targetLabel == null || cachedMeshInfo == null)
        {
            return;
        }

        TMP_TextInfo textInfo = targetLabel.textInfo;
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            Vector3[] destinationVertices = textInfo.meshInfo[i].vertices;
            Vector3[] sourceVertices = cachedMeshInfo[i].vertices;
            int vertexCount = Mathf.Min(destinationVertices.Length, sourceVertices.Length);

            for (int vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
            {
                destinationVertices[vertexIndex] = sourceVertices[vertexIndex];
            }

            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = destinationVertices;
            targetLabel.UpdateGeometry(meshInfo.mesh, i);
        }

        restoreMesh = false;
    }

    private static bool HasAnimatedEffect(DialogueTextEffect textEffect)
    {
        return textEffect == DialogueTextEffect.Shake ||
               textEffect == DialogueTextEffect.Wave ||
               textEffect == DialogueTextEffect.Whisper ||
               textEffect == DialogueTextEffect.Shout ||
               textEffect == DialogueTextEffect.Glitch;
    }

    private void LateUpdate()
    {
        if (targetLabel == null || !targetLabel.isActiveAndEnabled)
        {
            return;
        }

        if (!HasAnimatedEffect(activeEffect) && !HasAnimatedInlineEffect())
        {
            RestoreBaseMesh();
            return;
        }

        if (targetLabel.havePropertiesChanged || cachedMeshInfo == null || cachedMeshInfo.Length != targetLabel.textInfo.meshInfo.Length)
        {
            RefreshCachedMesh();
            targetLabel.havePropertiesChanged = false;
        }

        TMP_TextInfo textInfo = targetLabel.textInfo;
        int visibleCharacterCount = Mathf.Min(targetLabel.maxVisibleCharacters, textInfo.characterCount);
        if (visibleCharacterCount <= 0)
        {
            RestoreBaseMesh();
            return;
        }

        RestoreVisibleVertices(textInfo, visibleCharacterCount);

        float time = Time.unscaledTime;
        for (int i = 0; i < visibleCharacterCount; i++)
        {
            TMP_CharacterInfo characterInfo = textInfo.characterInfo[i];
            if (!characterInfo.isVisible)
            {
                continue;
            }

            DialogueTextEffect characterEffect = ResolveEffectForCharacter(i);
            GetCharacterTransform(characterEffect, i, time, out Vector3 offset, out float scale);
            if (offset == Vector3.zero && Mathf.Approximately(scale, 1f))
            {
                continue;
            }

            int materialIndex = characterInfo.materialReferenceIndex;
            int vertexIndex = characterInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            if (!Mathf.Approximately(scale, 1f))
            {
                Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) * 0.5f;
                vertices[vertexIndex + 0] = center + ((vertices[vertexIndex + 0] - center) * scale);
                vertices[vertexIndex + 1] = center + ((vertices[vertexIndex + 1] - center) * scale);
                vertices[vertexIndex + 2] = center + ((vertices[vertexIndex + 2] - center) * scale);
                vertices[vertexIndex + 3] = center + ((vertices[vertexIndex + 3] - center) * scale);
            }

            vertices[vertexIndex + 0] += offset;
            vertices[vertexIndex + 1] += offset;
            vertices[vertexIndex + 2] += offset;
            vertices[vertexIndex + 3] += offset;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
            meshInfo.mesh.vertices = meshInfo.vertices;
            targetLabel.UpdateGeometry(meshInfo.mesh, i);
        }

        restoreMesh = true;
    }

    private bool HasAnimatedInlineEffect()
    {
        for (int i = 0; i < inlineEffectSpans.Length; i++)
        {
            if (HasAnimatedEffect(inlineEffectSpans[i].Effect))
            {
                return true;
            }
        }

        return false;
    }

    private DialogueTextEffect ResolveEffectForCharacter(int characterIndex)
    {
        for (int i = 0; i < inlineEffectSpans.Length; i++)
        {
            if (inlineEffectSpans[i].Contains(characterIndex))
            {
                return inlineEffectSpans[i].Effect;
            }
        }

        return activeEffect;
    }

    private static void GetCharacterTransform(DialogueTextEffect textEffect, int characterIndex, float time, out Vector3 offset, out float scale)
    {
        offset = Vector3.zero;
        scale = 1f;

        switch (textEffect)
        {
            case DialogueTextEffect.Shake:
            {
                float step = Mathf.Floor(time * 24f);
                float randomX = (Hash01((characterIndex * 17.13f) + (step * 3.71f)) - 0.5f) * 1.4f;
                float randomY = (Hash01((characterIndex * 23.81f) + (step * 4.19f)) - 0.5f) * 1.4f;
                offset = new Vector3(randomX, randomY, 0f);
                break;
            }
            case DialogueTextEffect.Wave:
            {
                float phase = (time * 3.6f) + (characterIndex * 0.5f);
                offset = new Vector3(0f, Mathf.Sin(phase) * 3.1f, 0f);
                break;
            }
            case DialogueTextEffect.Whisper:
            {
                float driftX = Mathf.Sin((time * 1.2f) + (characterIndex * 0.33f)) * 0.22f;
                float driftY = Mathf.Cos((time * 1.55f) + (characterIndex * 0.41f)) * 0.28f;
                float step = Mathf.Floor(time * 5f);
                float unevenX = (Hash01((characterIndex * 9.27f) + (step * 1.93f)) - 0.5f) * 0.12f;
                float unevenY = (Hash01((characterIndex * 5.61f) + (step * 2.17f)) - 0.5f) * 0.1f;
                offset = new Vector3(driftX + unevenX, driftY + unevenY, 0f);
                break;
            }
            case DialogueTextEffect.Shout:
            {
                float step = Mathf.Floor(time * 30f);
                float randomX = (Hash01((characterIndex * 19.33f) + (step * 5.27f)) - 0.5f) * 2.3f;
                float randomY = (Hash01((characterIndex * 27.41f) + (step * 4.63f)) - 0.5f) * 2.5f;
                float pulse = 1f + (Mathf.Max(0f, Mathf.Sin((time * 11.5f) + (characterIndex * 0.55f))) * 0.05f);
                offset = new Vector3(randomX, randomY, 0f);
                scale = pulse;
                break;
            }
            case DialogueTextEffect.Glitch:
            {
                float step = Mathf.Floor(time * 14f);
                float activation = Hash01((characterIndex * 31.17f) + (step * 7.13f));
                if (activation > 0.84f)
                {
                    float horizontal = (Hash01((characterIndex * 13.73f) + (step * 9.91f)) - 0.5f) * 4.2f;
                    float vertical = (Hash01((characterIndex * 21.19f) + (step * 6.41f)) - 0.5f) * 1.6f;
                    offset = new Vector3(horizontal, vertical, 0f);
                }

                if (activation > 0.95f)
                {
                    scale = 0.97f + (Hash01((characterIndex * 11.71f) + (step * 3.17f)) * 0.06f);
                }

                break;
            }
        }
    }

    private static float Hash01(float value)
    {
        return Mathf.Repeat(Mathf.Sin(value) * 43758.5453f, 1f);
    }
}
