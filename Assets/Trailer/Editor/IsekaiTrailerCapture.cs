using System;
using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class IsekaiTrailerCapture
{
    #region Fields
    const string ActiveKey = "Trailer.Capture.Active";
    const string FailedKey = "Trailer.Capture.Failed";
    const string ScenePath = "Assets/Scenes/GameScene.unity";
    const int Width = 1920;
    const int Height = 1080;
    const float FrameRate = 60f;
    const int StillFrames = 3;
    const float RunTimeout = 600f;
    const float BootSeconds = 2f;
    static IsekaiTrailerPilot _pilot;
    static RecorderController _movie;
    static int _imageStart = -1;
    static float _started;
    static bool _finishing;
    static string OutputFolder => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Marketing", "trailer", "raw"));
    static bool Active => SessionState.GetBool(ActiveKey, false);
    #endregion

    #region Public Methods
    static IsekaiTrailerCapture()
    {
        if (Active) Hook();
    }
    [MenuItem("Tools/Trailer/Capture All")]
    public static void CaptureAll()
    {
        SessionState.SetBool(ActiveKey, true);
        SessionState.SetInt(FailedKey, 0);
        _started = Time.realtimeSinceStartup;
        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(Path.Combine(OutputFolder, "capture-log.txt"), string.Empty);
        _pilot = null;
        _finishing = false;
        _imageStart = -1;
        Directory.CreateDirectory(OutputFolder);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Hook();
        EditorApplication.isPlaying = true;
    }
    #endregion

    #region Private Methods
    static void Hook()
    {
        Application.logMessageReceived -= Log;
        Application.logMessageReceived += Log;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }
    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredPlayMode && Active)
        {
            _pilot = null;
            _started = Time.realtimeSinceStartup;
        }
        if (change == PlayModeStateChange.EnteredEditMode && Active) Finish();
    }
    static void Tick()
    {
        if (!Active || _finishing || !EditorApplication.isPlaying) return;
        try { Drive(); }
        catch (Exception e)
        {
            Debug.LogException(e);
            Abort("Capture threw " + e.GetType().Name + ".");
        }
    }
    static void Drive()
    {
        if (_pilot == null)
        {
            if (Time.realtimeSinceStartup - _started < BootSeconds || GameManager.Instance == null || GameManager.Instance.player == null) return;
            _pilot = new GameObject("TrailerPilot").AddComponent<IsekaiTrailerPilot>();
            _pilot.Begin();
            Debug.Log("[Trailer] Pilot started; output " + OutputFolder);
            return;
        }
        if (Time.realtimeSinceStartup - _started > RunTimeout)
        {
            Abort($"Run took longer than {RunTimeout} s.");
            return;
        }
        switch (_pilot.Phase)
        {
            case TrailerPhase.AwaitingCamera:
                StartMovie(_pilot.Current.Id);
                _pilot.CameraRolling = true;
                break;
            case TrailerPhase.Still:
                TickStill(_pilot.Current.Id);
                break;
            case TrailerPhase.Cut:
                StopMovie();
                _pilot.CameraRolling = false;
                break;
            case TrailerPhase.Done:
                Debug.Log("[Trailer] All clips captured.");
                EditorApplication.isPlaying = false;
                break;
            case TrailerPhase.Failed:
                Abort(_pilot.Failure);
                break;
        }
    }
    static void StartMovie(string id)
    {
        if (_movie != null) return;
        string path = Path.Combine(OutputFolder, id);
        if (File.Exists(path + ".mp4")) File.Delete(path + ".mp4");
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = "Trailer " + id;
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High
        };
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = Width, OutputHeight = Height };
        movie.CaptureAudio = true;
        movie.OutputFile = path;
        _movie = Controller(movie);
        Debug.Log("[Trailer] Recording " + id);
    }
    static void StopMovie()
    {
        if (_movie == null) return;
        _movie.StopRecording();
        _movie = null;
    }
    static void TickStill(string id)
    {
        if (_pilot.StillTaken) return;
        if (_imageStart < 0)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(OutputFolder, id + "_still.png"));
            _imageStart = Time.frameCount;
            Debug.Log("[Trailer] Still requested for " + id);
            return;
        }
        if (Time.frameCount < _imageStart + StillFrames) return;
        _imageStart = -1;
        _pilot.StillTaken = true;
    }
    static RecorderController Controller(RecorderSettings recorder)
    {
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        settings.AddRecorderSettings(recorder);
        settings.SetRecordModeToManual();
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.FrameRate = FrameRate;
        settings.CapFrameRate = true;
        var controller = new RecorderController(settings);
        controller.PrepareRecording();
        if (!controller.StartRecording()) Debug.LogError("[Trailer] Recorder did not start for " + recorder.name);
        return controller;
    }
    static void Log(string message, string stack, LogType type)
    {
        if (!message.StartsWith("[Trailer]") && type != LogType.Error && type != LogType.Exception) return;
        File.AppendAllText(Path.Combine(OutputFolder, "capture-log.txt"), $"{DateTime.Now:HH:mm:ss} {type}: {message}\n{(type == LogType.Exception ? stack + "\n" : string.Empty)}");
    }
    static void Abort(string reason)
    {
        Debug.LogError("[Trailer] " + reason);
        SessionState.SetInt(FailedKey, SessionState.GetInt(FailedKey, 0) + 1);
        StopMovie();
        EditorApplication.isPlaying = false;
    }
    static void Finish()
    {
        _finishing = true;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        Application.logMessageReceived -= Log;
        int failed = SessionState.GetInt(FailedKey, 0);
        SessionState.EraseBool(ActiveKey);
        SessionState.EraseInt(FailedKey);
        File.WriteAllText(Path.Combine(OutputFolder, "capture-result.txt"), failed > 0 ? "FAILED, see the Console [Trailer] lines" : "OK");
        EditorApplication.Exit(failed > 0 ? 1 : 0);
    }
    #endregion
}
