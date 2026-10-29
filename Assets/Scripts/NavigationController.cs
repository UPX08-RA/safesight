using SafeSight.Navigation;
using UnityEngine;

/// <summary>
/// Liga o QrScanner e o DisplacementEstimator a NavigationLogic (regras de navegacao por rota
/// cadastrada) e expoe o estado para o HUD (NavigationHud). O painel de teste so aparece se ligado.
/// </summary>
public class NavigationController : MonoBehaviour
{
    [Header("Referencias (se vazias, procura na cena)")]
    [SerializeField] TextAsset trajetoJson;   // se vazio, carrega Resources/trajeto
    [SerializeField] QrScanner qrScanner;
    [SerializeField] DisplacementEstimator estimator;

    [Header("Limiares")]
    [SerializeField, Min(0.3f)] float warnMeters = 1.0f;
    [SerializeField, Min(0.3f)] float overshootMeters = 1.5f;
    [SerializeField, Min(0.5f)] float rescanGapSeconds = 1.5f;

    [Header("Teste")]
    [SerializeField] bool showDebugPanel = false;

    NavigationLogic logic;
    TextAsset jsonAsset;
    string toast = "";
    float toastUntil;

    public NavigationLogic Logic => logic;
    public DisplacementEstimator Estimator => estimator;
    public NavState State => logic != null ? logic.State : NavState.Idle;

    /// <summary>Aviso temporario (QR invalido, ponto sem rota).</summary>
    public string ToastText => toast;
    public bool ToastActive => Time.realtimeSinceStartup < toastUntil;

    public bool ShowDebugPanel { get => showDebugPanel; set => showDebugPanel = value; }

    /// <summary>Liga/desliga os paineis de teste (navegacao, leitor de QR e estimador).</summary>
    public void SetAllDebugPanels(bool on)
    {
        showDebugPanel = on;
        if (qrScanner != null) qrScanner.ShowDebugPanel = on;
        if (estimator != null) estimator.ShowDebugPanel = on;
    }

    /// <summary>Volta ao estado inicial (aguardando QR).</summary>
    public void ResetNavigation()
    {
        if (jsonAsset == null) return;
        logic = NavigationLogic.FromJson(jsonAsset.text);
        ApplyThresholds();
        toast = "";
        toastUntil = 0f;
    }

    void Awake()
    {
        jsonAsset = trajetoJson != null ? trajetoJson : Resources.Load<TextAsset>("trajeto");
        if (jsonAsset == null)
        {
            Debug.LogError("trajeto.json nao encontrado em Resources");
            return;
        }
        logic = NavigationLogic.FromJson(jsonAsset.text);
        ApplyThresholds();
    }

    void OnValidate()
    {
        if (logic != null) ApplyThresholds();
    }

    void ApplyThresholds()
    {
        logic.WarnMeters = warnMeters;
        logic.OvershootMeters = overshootMeters;
        logic.RescanGapSeconds = rescanGapSeconds;
    }

    void OnEnable()
    {
        if (qrScanner == null) qrScanner = FindFirstObjectByType<QrScanner>();
        if (estimator == null) estimator = FindFirstObjectByType<DisplacementEstimator>();
        // Quem reinicia a estimativa e a navegacao (so em QR valido), nao o proprio estimador.
        if (estimator != null) estimator.AutoRestartOnQr = false;
        if (qrScanner != null) qrScanner.OnQrRead += HandleQr;
    }

    void OnDisable()
    {
        if (qrScanner != null) qrScanner.OnQrRead -= HandleQr;
        if (estimator != null) estimator.AutoRestartOnQr = true;
    }

    void HandleQr(string text)
    {
        if (logic == null) return;
        switch (logic.OnQrRead(text, Time.realtimeSinceStartup))
        {
            case QrResult.RouteLoaded:
                if (estimator != null)
                {
                    estimator.Restart(text);
                    estimator.SetLeg(logic.CurrentLeg.meters);
                }
                break;
            case QrResult.Unknown:
            case QrResult.NoRoute:
                toast = logic.Message;
                toastUntil = Time.realtimeSinceStartup + 3f;
                break;
        }
    }

    void Update()
    {
        if (logic == null || estimator == null || !estimator.Started) return;
        logic.Tick(estimator.StraightMeters, estimator.TrackingLost);
    }

    // Painel de teste simples (sem Canvas). A UI do usuario e o NavigationHud.
    void OnGUI()
    {
        if (logic == null || !showDebugPanel) return;
        int size = Mathf.RoundToInt(Screen.height * 0.024f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true };
        style.normal.textColor = Color.white;

        float pad = Screen.height * 0.02f;
        var rect = new Rect(pad, Screen.height * 0.28f, Screen.width - 2 * pad, Screen.height * 0.22f);
        GUI.Box(rect, GUIContent.none);

        string ponto = logic.CurrentPoint != null ? logic.CurrentId : "-";
        string alertas = (logic.ShowElevatorAlert ? "NAO USE O ELEVADOR  " : "") +
                         (string.IsNullOrEmpty(logic.ExtinguisherText) ? "" : $"Extintor: {logic.ExtinguisherText}");
        string msg = ToastActive ? $"\n{toast}" : "";

        GUI.Label(rect,
            $"Estado: {logic.State}  |  Ponto: {ponto}  |  Destino: {logic.ExitName}\n" +
            $"{logic.Instruction}\n" +
            $"Restam no trecho: {logic.RemainingLegMeters:0.0} m  |  Ate a saida: {logic.RemainingTotalMeters:0} m\n" +
            $"{alertas}{msg}", style);
    }
}
