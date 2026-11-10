using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a screen-space uGUI canvas on the game's pixel grid: one canvas unit is one game pixel, scaled by the same
/// whole number the PixelCamera uses, so UI sprites never land between screen pixels.
/// </summary>
[RequireComponent(typeof(CanvasScaler))]
public class PixelCanvasScaler : MonoBehaviour
{
    private CanvasScaler _scaler;
    private Vector2Int _screenSize;

    private void OnEnable()
    {
        _scaler = GetComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        GetComponent<Canvas>().pixelPerfect = true;
        _screenSize = Vector2Int.zero;
        Apply();
    }

    private void Update() => Apply();

    private void Apply()
    {
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (screen == _screenSize) return;
        _screenSize = screen;

        // Match the PixelCamera when there is one; otherwise use the same maths with the project's reference size.
        PixelCamera pixelCamera = PixelCamera.Main;
        int scale = pixelCamera != null && pixelCamera.IntegerScale > 0
            ? pixelCamera.IntegerScale
            : PixelViewport.Compute(PixelPerfectSettings.Current.ResolvedReferenceResolution, screen, UpscaleMode.Integer).IntegerScale;
        _scaler.scaleFactor = Mathf.Max(1, scale);
    }
}
