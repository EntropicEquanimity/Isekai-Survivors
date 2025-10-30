using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.UI;

public class SwitchToRandomColor : MonoBehaviour
{
    [BoxGroup("Settings")] public bool triggerAtStart = true;
    [BoxGroup("Settings")] public bool randomizeColor = true;
    [BoxGroup("Settings")] [ShowIf("randomizeColor")] public bool randomizeToSimilarColor = false;
    [BoxGroup("Settings")] [ShowIf("randomizeToSimilarColor")] public float randomizeSimilarVariation = 0.3f;
    [BoxGroup("Settings")] public Color newColor = Color.white;
    [BoxGroup("Settings")] public SpriteRenderer sr;
    [BoxGroup("Settings")] public Image image;
    void OnEnable()
    {
        ColorChange();
    }

    // Update is called once per frame
    public void ColorChange()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            if (triggerAtStart)
            {
                if (randomizeColor) { if (randomizeToSimilarColor) { cam.backgroundColor = RandomColorSimilarTo(newColor, randomizeSimilarVariation); } else { cam.backgroundColor = RandomColor(); } }
                else { cam.backgroundColor = newColor; }
            }
        }
        else if (sr != null)
        {
            if (triggerAtStart)
            {
                if (randomizeColor) { if (randomizeToSimilarColor) { sr.color = RandomColorSimilarTo(newColor, randomizeSimilarVariation); } else { sr.color = RandomColor(); } }
                else { sr.color = newColor; }
            }
        }
        else if(image != null)
        {
            if (triggerAtStart)
            {
                if (randomizeColor) { if (randomizeToSimilarColor) { image.color = RandomColorSimilarTo(newColor, randomizeSimilarVariation); } else { image.color = RandomColor(); } }
                else { image.color = newColor; }
            }
        }
    }

    // 1. Any random color
    public static Color RandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }

    // 2. Random color within min/max range
    public static Color RandomColorInRange(Color min, Color max)
    {
        return new Color(
            Random.Range(min.r, max.r),
            Random.Range(min.g, max.g),
            Random.Range(min.b, max.b)
        );
    }

    // 3. Random color with similar hue (good for color themes)
    public static Color RandomColorSimilarTo(Color baseColor, float variation = 0.3f)
    {
        return new Color(
            Mathf.Clamp01(baseColor.r + Random.Range(-variation, variation)),
            Mathf.Clamp01(baseColor.g + Random.Range(-variation, variation)),
            Mathf.Clamp01(baseColor.b + Random.Range(-variation, variation))
        );
    }

    // 4. Random grayscale
    public static Color RandomGrayscale(float min = 0f, float max = 1f)
    {
        float value = Random.Range(min, max);
        return new Color(value, value, value);
    }
}
