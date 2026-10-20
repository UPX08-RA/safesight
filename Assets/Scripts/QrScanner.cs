using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ZXing;

/// <summary>
/// Le QR Codes direto do frame da camera do ARCore (XRCpuImage), sem segunda camera.
/// Usa so o plano Y (luminancia) do frame, que e tudo que o ZXing precisa.
/// Teste inicial: mostra na tela o texto lido, o tempo de decodificacao e o FPS.
/// </summary>
public class QrScanner : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Se vazio, procura o ARCameraManager da cena.")]
    [SerializeField] ARCameraManager cameraManager;

    [Header("Teste")]
    [Tooltip("Mostra o painel de teste (texto lido, ms, FPS). Desligar quando a UI definitiva existir.")]
    [SerializeField] bool showDebugPanel = true;

    [Header("Desempenho")]
    [Tooltip("Tenta decodificar a cada N frames (maior = mais leve).")]
    [SerializeField, Min(1)] int scanEveryNFrames = 5;

    /// <summary>Disparado a cada leitura valida (texto do QR).</summary>
    public event Action<string> OnQrRead;

    public string LastText { get; private set; } = "";

    BarcodeReaderGeneric reader;
    byte[] buffer;
    int frameCount;
    float decodeMs;
    float fps;
    float lastReadTime = -999f;
    string status = "Aponte a camera para um QR Code";

    void Awake()
    {
        if (cameraManager == null)
            cameraManager = FindFirstObjectByType<ARCameraManager>();

        reader = new BarcodeReaderGeneric
        {
            AutoRotate = true,
            Options = new ZXing.Common.DecodingOptions
            {
                TryHarder = true,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE }
            }
        };
    }

    void Update()
    {
        fps = Mathf.Lerp(fps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.1f);

        if (cameraManager == null)
        {
            status = "ARCameraManager nao encontrado na cena";
            return;
        }

        if (++frameCount % scanEveryNFrames != 0) return;
        if (!cameraManager.TryAcquireLatestCpuImage(out var image)) return;

        using (image)
        {
            int w = image.width;
            int h = image.height;
            var plane = image.GetPlane(0); // plano Y (luminancia)

            if (buffer == null || buffer.Length != w * h)
                buffer = new byte[w * h];

            NativeArray<byte> data = plane.data;
            for (int row = 0; row < h; row++)
                NativeArray<byte>.Copy(data, row * plane.rowStride, buffer, row * w, w);

            float t0 = Time.realtimeSinceStartup;
            var source = new PlanarYUVLuminanceSource(buffer, w, h, 0, 0, w, h, false);
            Result result = reader.Decode(source);
            decodeMs = (Time.realtimeSinceStartup - t0) * 1000f;

            if (result != null)
            {
                LastText = result.Text;
                lastReadTime = Time.realtimeSinceStartup;
                status = "QR lido";
                OnQrRead?.Invoke(result.Text);
            }
            else if (Time.realtimeSinceStartup - lastReadTime > 2f)
            {
                status = "Aponte a camera para um QR Code";
            }
        }
    }

    // Tela de teste simples (sem Canvas). A UI definitiva vem depois.
    void OnGUI()
    {
        if (!showDebugPanel) return;
        int size = Mathf.RoundToInt(Screen.height * 0.035f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true };
        style.normal.textColor = Color.white;

        float pad = size;
        var rect = new Rect(pad, pad, Screen.width - 2 * pad, Screen.height * 0.4f);
        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect,
            $"{status}\n" +
            $"Ultimo QR: {(string.IsNullOrEmpty(LastText) ? "-" : LastText)}\n" +
            $"Decodificacao: {decodeMs:0.0} ms  |  FPS: {fps:0}\n" +
            $"Escaneia a cada {scanEveryNFrames} frames", style);
    }
}