using TMPro;
using UnityEngine;

/// <summary>
/// Moves every glyph of a screen-space TMP text onto whole screen pixels. With a pixel font at a whole multiple of
/// its native size, this keeps each font pixel a crisp block even when alignment (e.g. centred) lands half a pixel off.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class PixelTextSnap : MonoBehaviour
{
    private TMP_Text _text;

    private void OnEnable()
    {
        _text = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        _text.ForceMeshUpdate();
    }

    private void OnDisable() => TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);

    private void OnTextChanged(Object changed)
    {
        if (changed == _text) Snap();
    }

    // In a Screen Space - Overlay canvas, world units are screen pixels.
    private void Snap()
    {
        TMP_TextInfo info = _text.textInfo;
        Transform t = _text.transform;
        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo character = info.characterInfo[i];
            if (!character.isVisible) continue;
            Vector3[] vertices = info.meshInfo[character.materialReferenceIndex].vertices;
            int v = character.vertexIndex;
            Vector3 bottomLeft = t.TransformPoint(vertices[v]);
            var snapped = new Vector3(Mathf.Round(bottomLeft.x), Mathf.Round(bottomLeft.y), bottomLeft.z);
            Vector3 offset = t.InverseTransformVector(snapped - bottomLeft);
            for (int k = 0; k < 4; k++) vertices[v + k] += offset;
        }
        _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }
}
