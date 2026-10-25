using SafeSight.Navigation;
using UnityEngine;

/// <summary>
/// Liga o QrScanner e o DisplacementEstimator a NavigationLogic (regras de navegacao por rota
/// cadastrada) e expoe o estado para o HUD. Tem so um painel de teste simples; as telas
/// definitivas ficam com o HUD (Canvas).
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
    [SerializeField] bool showDebugPanel = true;

    NavigationLogic logic;
    string toast = "";
    float toastUntil;

    public NavigationLogic Logic => logic;
    public NavState State => logic != null ? logic.State : NavState.Idle;

    void Awake()
    {
        var ta = trajetoJson != null ? trajetoJson : Resources.Load<TextAsset>("trajeto");
        if (ta == null)
        {
            Debug.LogError("trajeto.json nao encontrado em Resources");
            return;
        }
        logic = NavigationLogic.FromJson(ta.text);
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

    // Painel de teste simples (sem Canvas). A UI definitiva e do HUD.
    void OnGUI()
    {
        if (logic == null || !showDebugPanel) return;
        int size = Mathf.RoundToInt(Screen.height * 0.03f);
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, wordWrap = true };
        style.normal.textColor = Color.white;

        float pad = size;
        var rect = new Rect(pad, Screen.height * 0.62f, Screen.width - 2 * pad, Screen.height * 0.34f);
        GUI.Box(rect, GUIContent.none);

        string ponto = logic.CurrentPoint != null ? logic.CurrentId : "-";
        string alertas = (logic.ShowElevatorAlert ? "NAO USE O ELEVADOR  " : "") +
                         (string.IsNullOrEmpty(logic.ExtinguisherText) ? "" : $"Extintor: {logic.ExtinguisherText}");
        string msg = Time.realtimeSinceStartup < toastUntil ? $"\n{toast}" : "";

        GUI.Label(rect,
            $"Estado: {logic.State}  |  Ponto: {ponto}  |  Destino: {logic.ExitName}\n" +
            $"{logic.Instruction}\n" +
            $"Restam no trecho: {logic.RemainingLegMeters:0.0} m  |  Ate a saida: {logic.RemainingTotalMeters:0} m\n" +
            $"{alertas}{msg}", style);
    }
}
