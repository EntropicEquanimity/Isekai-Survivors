using Blondie.PixelPerfect;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps a screen-space uGUI canvas on the game's pixel grid: one canvas unit is one game pixel, scaled by the same
/// whole number the PixelCamera uses, so UI sprites never land between screen pixels.
/// Runs before other scripts so the canvas has its reference pixels per unit before anything under it calls
/// <see cref="Image.SetNativeSize"/> during scene load (Unity's default of 100 would make those sprites ~3x too big).
/// </summary>
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(CanvasScaler))]
public class PixelCanvasScaler : MonoBehaviour
{
    private CanvasScaler _scaler;
    private Vector2Int _screenSize;

    private void OnEnable()
    {
        _scaler = GetComponent<CanvasScaler>();
        _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        var canvas = GetComponent<Canvas>();
        canvas.pixelPerfect = true;
        canvas.referencePixelsPerUnit = _scaler.referencePixelsPerUnit;
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
