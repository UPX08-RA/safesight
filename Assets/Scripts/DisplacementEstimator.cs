using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Estima o deslocamento desde o ultimo QR lido usando o rastreamento de movimento
/// do ARCore (pose da camera).
/// - Reinicia a estimativa a cada leitura de QR (apos um intervalo sem ver o QR).
/// - Calcula distancia em linha reta (plano horizontal), caminho acumulado e giro.
/// - Avisa a curva quando faltar cerca de warnMeters para o fim do trecho.
/// - Pede novo QR se o rastreamento for perdido ou o trecho for excedido (RF10).
/// O comprimento do trecho vem de SetLeg (a navegacao chama com o valor do JSON);
/// enquanto isso, usa legMeters do Inspector (valor de teste com trena).
/// </summary>
public class DisplacementEstimator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] QrScanner qrScanner;

    [Header("Trecho")]
    [Tooltip("Comprimento do trecho atual (m). Valor de teste; a navegacao usa SetLeg().")]
    [SerializeField, Min(0.5f)] float legMeters = 5f;

    [Tooltip("Avisa a curva quando faltar esta distancia (m) para o fim do trecho.")]
    [SerializeField, Min(0.1f)] float warnMeters = 1f;

    [Tooltip("Se passar do fim do trecho por mais que isso (m), pede novo QR.")]
    [SerializeField, Min(0.1f)] float overshootMeters = 1.5f;

    [Header("Deteccao")]
    [Tooltip("Ignora leituras repetidas do mesmo QR dentro deste intervalo (s).")]
    [SerializeField, Min(0.5f)] float rescanGapSeconds = 1.5f;

    [Tooltip("Giro (graus) a partir do qual consideramos que o usuario virou.")]
    [SerializeField, Min(10f)] float turnThresholdDegrees = 60f;

    [Header("Teste")]
    [Tooltip("Mostra o painel de teste. Desligar quando a UI definitiva existir.")]
    [SerializeField] bool showDebugPanel = true;

    Transform cam;
    Vector3 startPos;
    Vector3 lastPos;
    float startYaw;
    float pathMeters;
    bool hasStart;
    bool trackingLost;
    float lastQrEventTime = -999f;
    string startQr = "";

    public float StraightMeters { get; private set; }
    public float PathMeters => pathMeters;
    public float YawDelta { get; private set; }
    public bool TrackingLost => trackingLost;
    public bool Started => hasStart;

    /// <summary>
    /// Se verdadeiro (padrao), reinicia sozinho a cada QR lido. A navegacao desliga isso e chama
    /// Restart() so em QR valido, para um QR estranho nao zerar a distancia no meio do trecho.
    /// </summary>
    public bool AutoRestartOnQr { get; set; } = true;

    public float LegMeters => legMeters;
    public float RemainingMeters => Mathf.Max(0f, legMeters - StraightMeters);
    public bool LegExceeded => hasStart && StraightMeters > legMeters + overshootMeters;
    public bool TurnWarning => hasStart && !trackingLost && !LegExceeded && RemainingMeters <= warnMeters;
    public bool TurnConfirmed => hasStart && Mathf.Abs(YawDelta) >= turnThresholdDegrees;
    public bool NeedNewQr => hasStart && (trackingLost || LegExceeded);

    void OnEnable()
    {
        if (qrScanner == null) qrScanner = FindFirstObjectByType<QrScanner>();
        if (qrScanner != null) qrScanner.OnQrRead += HandleQr;
    }

    void OnDisable()
    {
        if (qrScanner != null) qrScanner.OnQrRead -= HandleQr;
    }

    void HandleQr(string text)
    {
        if (!AutoRestartOnQr) return;
        bool gap = Time.realtimeSinceStartup - lastQrEventTime > rescanGapSeconds;
        lastQrEventTime = Time.realtimeSinceStartup;
        if (gap || !hasStart) Restart(text);
    }

    /// <summary>Define o comprimento do trecho atual (m), vindo da rota cadastrada.</summary>
    public void SetLeg(float meters)
    {
        legMeters = Mathf.Max(0.5f, meters);
    }

    public void Restart(string qrText = "")
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null) return;
        startQr = qrText;
        startPos = lastPos = Flat(cam.position);
        startYaw = cam.eulerAngles.y;
        pathMeters = 0f;
        StraightMeters = 0f;
        YawDelta = 0f;
        trackingLost = false;
        hasStart = true;
    }

    void Update()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (!hasStart || cam == null) return;

        if (ARSession.state != ARSessionState.SessionTracking)
        {
            trackingLost = true; // exige novo QR (RF10)
            return;
        }
        if (trackingLost) return; // so volta apos novo QR

        Vector3 p = Flat(cam.position);
        pathMeters += Vector3.Distance(lastPos, p);
        lastPos = p;
        StraightMeters = Vector3.Distance(startPos, p);
        YawDelta = Mathf.DeltaAngle(startYaw, cam.eulerAngles.y);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // Painel de teste simples (sem Canvas).
    void OnGUI()
    {
        if (!showDebugPanel) return;
        int size = Mathf.RoundToInt(Screen.height * 0.035f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true };
        style.normal.textColor = Color.white;

        float pad = size;
        var rect = new Rect(pad, Screen.height * 0.5f, Screen.width - 2 * pad, Screen.height * 0.3f);
        GUI.Box(rect, GUIContent.none);

        string state = !hasStart ? "Leia um QR para iniciar"
                     : trackingLost ? "RASTREAMENTO PERDIDO - leia um novo QR"
                     : LegExceeded ? "TRECHO EXCEDIDO - leia um novo QR"
                     : TurnWarning ? "CURVA PROXIMA"
                     : TurnConfirmed ? "Giro detectado"
                     : "Rastreando";

        GUI.Label(rect,
            $"{state}\n" +
            $"QR de partida: {(string.IsNullOrEmpty(startQr) ? "-" : startQr)}\n" +
            $"Linha reta: {StraightMeters:0.00} m  |  Caminho: {pathMeters:0.00} m\n" +
            $"Trecho: {legMeters:0.0} m  |  Faltam: {RemainingMeters:0.00} m  |  Giro: {YawDelta:0}°", style);

        float bw = Screen.width * 0.4f, bh = Screen.height * 0.07f;
        if (GUI.Button(new Rect(pad, Screen.height * 0.82f, bw, bh), "Zerar agora"))
            Restart(startQr);
    }
}
