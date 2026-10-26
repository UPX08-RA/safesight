using System.Collections.Generic;
using SafeSight.Navigation;
using UnityEngine;

/// <summary>
/// HUD de navegacao (RF01, RF05, RF06, RF07): desenhado em codigo (IMGUI), fixo na tela sobre a
/// camera, sem ancoragem espacial. Le o estado da NavigationController/NavigationLogic.
/// Estados: aguardando QR (moldura), rota ativa (seta, instrucao, progresso, proximos passos),
/// novo QR necessario ("?"), chegada (check) e aviso de QR invalido. Elevador e extintor como alertas.
/// </summary>
public class NavigationHud : MonoBehaviour
{
    [SerializeField] NavigationController controller;
    [Tooltip("Giro (graus) a partir do qual a curva e considerada confirmada.")]
    [SerializeField, Min(10f)] float turnConfirmDegrees = 60f;
    [Tooltip("Mostra os paineis de teste ao iniciar (o botao Debug alterna).")]
    [SerializeField] bool debugAtStart = false;

    static readonly Color Panel = new Color(0.05f, 0.07f, 0.11f, 0.80f);
    static readonly Color Green = new Color(0.12f, 0.78f, 0.35f);
    static readonly Color Amber = new Color(1f, 0.72f, 0.10f);
    static readonly Color Red = new Color(0.90f, 0.22f, 0.22f);
    static readonly Color Muted = new Color(0.66f, 0.73f, 0.84f);

    Texture2D arrowTex;
    GUIStyle sty;
    float u;          // unidade de escala (relativa ao tamanho da tela)
    bool debugOn;

    void Awake()
    {
        arrowTex = MakeArrow();
    }

    void Start()
    {
        if (controller == null) controller = FindFirstObjectByType<NavigationController>();
        debugOn = debugAtStart;
        if (controller != null) controller.SetAllDebugPanels(debugOn);
    }

    // ---------- Desenho ----------
    static void Round(Rect r, Color c, float radius)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f,
            Mathf.Min(radius, Mathf.Min(r.width, r.height) / 2f));
    }

    static void RingShape(Rect r, Color c, float width)
    {
        GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, width, r.width / 2f);
    }

    void Text(Rect r, string s, float size, Color c, TextAnchor a = TextAnchor.UpperLeft, bool bold = false, bool wrap = false)
    {
        sty.fontSize = Mathf.RoundToInt(size * u);
        sty.alignment = a;
        sty.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        sty.wordWrap = wrap;
        sty.normal.textColor = new Color(0, 0, 0, 0.5f);
        GUI.Label(new Rect(r.x + 1.5f * u, r.y + 1.5f * u, r.width, r.height), s, sty);
        sty.normal.textColor = c;
        GUI.Label(r, s, sty);
    }

    float TextWidth(string s, float size, bool bold)
    {
        sty.fontSize = Mathf.RoundToInt(size * u);
        sty.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        sty.wordWrap = false;
        return sty.CalcSize(new GUIContent(s)).x;
    }

    // Pilula de alerta com icone "!"; devolve a altura usada.
    float Pill(float x, float y, string s, Color bg, Color fg)
    {
        float h = 56f * u;
        float w = TextWidth(s, 28, true) + 96f * u;
        Round(new Rect(x, y, w, h), bg, h / 2f);
        var ic = new Rect(x + 12f * u, y + 10f * u, 36f * u, 36f * u);
        Round(ic, Color.white, 18f * u);
        Text(ic, "!", 26, bg, TextAnchor.MiddleCenter, true);
        Text(new Rect(x + 58f * u, y, w - 62f * u, h), s, 28, fg, TextAnchor.MiddleLeft, true);
        return h;
    }

    bool GhostButton(Rect r, string s)
    {
        Round(r, new Color(1f, 1f, 1f, 0.20f), r.height / 2f);
        Text(r, s, 24, Color.white, TextAnchor.MiddleCenter, true);
        return GUI.Button(r, GUIContent.none, GUIStyle.none);
    }

    void RotatedBar(Vector2 center, float len, float th, float angle, Color c)
    {
        var old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, center);
        Round(new Rect(center.x - len / 2f, center.y - th / 2f, len, th), c, th / 2f);
        GUI.matrix = old;
    }

    static string StepText(TrajLeg l, string exitId)
    {
        if (l.IsStairs) return $"Use a escada ({l.meters:0} m)";
        string s = $"Siga {l.meters:0} m";
        if (l.toId == exitId) return s + " até a saída";
        return l.turn == "none" ? s : s + " e " + NavigationLogic.TurnText(l.turn);
    }

    bool TurnConfirmed(TrajLeg leg, DisplacementEstimator est)
    {
        if (leg == null || est == null) return false;
        return (leg.turn == "right" && est.YawDelta >= turnConfirmDegrees) ||
               (leg.turn == "left" && est.YawDelta <= -turnConfirmDegrees);
    }

    void OnGUI()
    {
        if (controller == null || controller.Logic == null) return;
        var logic = controller.Logic;
        var est = controller.Estimator;
        var state = logic.State;
        var leg = logic.CurrentLeg;

        var sa = Screen.safeArea;
        float H = Screen.height;
        u = Mathf.Min(H, sa.width * 1.6f) / 1000f;
        if (sty == null) sty = new GUIStyle(GUI.skin.label);

        float m = 16f * u;
        float colW = Mathf.Min(sa.width - 2f * m, 1150f * u);
        float left = sa.x + (sa.width - colW) / 2f;
        float top = (Screen.height - sa.yMax) + m;
        float bottom = (Screen.height - sa.yMin) - m;
        float cx = sa.x + sa.width / 2f;

        Color accent = state == NavState.NeedNewQr ? Red : state == NavState.TurnWarning ? Amber : Green;
        bool active = leg != null && est != null && est.Started &&
                      state != NavState.Idle && state != NavState.Arrived;

        // ===== Cartao superior: onde estou / destino (RF05) =====
        var tc = new Rect(left, top, colW, 124f * u);
        Round(tc, Panel, 30f * u);
        Text(new Rect(tc.x + 26f * u, tc.y + 12f * u, colW * 0.5f, 26f * u), "VOCÊ ESTÁ EM", 19, Muted, bold: true);
        Text(new Rect(tc.x + 26f * u, tc.y + 34f * u, colW * 0.5f, 60f * u),
             string.IsNullOrEmpty(logic.CurrentId) ? "-" : logic.CurrentId, 50, Color.white, bold: true);
        var cp = logic.CurrentPoint;
        if (cp != null && !string.IsNullOrEmpty(cp.description))
            Text(new Rect(tc.x + 26f * u, tc.y + 90f * u, colW * 0.62f, 28f * u), cp.description, 21, Muted);
        Text(new Rect(tc.x + colW * 0.45f, tc.y + 12f * u, colW * 0.55f - 26f * u, 26f * u), "DESTINO", 19, Muted,
             TextAnchor.UpperRight, true);
        Text(new Rect(tc.x + colW * 0.45f, tc.y + 40f * u, colW * 0.55f - 26f * u, 50f * u), logic.ExitName, 36, Green,
             TextAnchor.UpperRight, true);

        // ===== Alertas (RF07) + botoes =====
        float ay = tc.yMax + 10f * u;
        if (cp != null)
        {
            if (logic.ShowElevatorAlert) ay += Pill(left, ay, "Não utilize o elevador", Red, Color.white) + 8f * u;
            if (!string.IsNullOrEmpty(logic.ExtinguisherText))
                ay += Pill(left, ay, "Extintor: " + logic.ExtinguisherText, Amber, new Color(0.15f, 0.1f, 0f)) + 8f * u;
        }
        float bh = 50f * u, bw = 170f * u;
        if (GhostButton(new Rect(left + colW - bw, tc.yMax + 10f * u, bw, bh), debugOn ? "Ocultar debug" : "Debug"))
        {
            debugOn = !debugOn;
            controller.SetAllDebugPanels(debugOn);
        }
        if (GhostButton(new Rect(left + colW - 2 * bw - 10f * u, tc.yMax + 10f * u, bw, bh), "Reiniciar"))
            controller.ResetNavigation();

        // ===== Centro: seta / estado =====
        float cy = H * 0.46f;
        float pulse = 1f + 0.035f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
        float D = 330f * u * pulse;
        var circle = new Rect(cx - D / 2f, cy - D / 2f, D, D);

        if (state == NavState.Following || state == NavState.TurnWarning)
        {
            Round(circle, new Color(0.05f, 0.07f, 0.11f, 0.55f), D / 2f);
            RingShape(circle, accent, 7f * u);
            float angle = 0f;
            if (state == NavState.TurnWarning && leg != null && !logic.IsLastLeg)
                angle = leg.turn == "right" ? 90f : leg.turn == "left" ? -90f : 0f;
            float S = 230f * u * pulse;
            var ar = new Rect(cx - S / 2f, cy - S / 2f, S, S);
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, ar.center);
            if (arrowTex != null) GUI.DrawTexture(ar, arrowTex);
            GUI.matrix = old;
        }
        else if (state == NavState.Arrived)
        {
            Round(circle, new Color(0.12f, 0.78f, 0.35f, 0.95f), D / 2f);
            var v = new Vector2(cx - 20f * u, cy + 55f * u); // vertice do "check"
            RotatedBar(new Vector2(v.x - 48f * u, v.y - 28f * u), 90f * u, 30f * u, 45f, Color.white);
            RotatedBar(new Vector2(v.x + 55f * u, v.y - 65f * u), 190f * u, 30f * u, -50f, Color.white);
        }
        else if (state == NavState.NeedNewQr)
        {
            Round(circle, new Color(0.90f, 0.22f, 0.22f, 0.90f), D / 2f);
            Text(circle, "?", 170, Color.white, TextAnchor.MiddleCenter, true);
        }
        else // Idle: moldura de leitura de QR
        {
            float s = 330f * u, L = 70f * u, th = 10f * u;
            var c = new Color(1f, 1f, 1f, 0.9f);
            float x0 = cx - s / 2f, y0 = cy - s / 2f, x1 = cx + s / 2f, y1 = cy + s / 2f;
            Round(new Rect(x0, y0, L, th), c, th / 2f); Round(new Rect(x0, y0, th, L), c, th / 2f);
            Round(new Rect(x1 - L, y0, L, th), c, th / 2f); Round(new Rect(x1 - th, y0, th, L), c, th / 2f);
            Round(new Rect(x0, y1 - th, L, th), c, th / 2f); Round(new Rect(x0, y1 - L, th, L), c, th / 2f);
            Round(new Rect(x1 - L, y1 - th, L, th), c, th / 2f); Round(new Rect(x1 - th, y1 - L, th, L), c, th / 2f);
        }

        // ===== Cartao inferior: instrucao, progresso e proximos passos (RF06) =====
        var steps = new List<TrajLeg>(3);
        if (active)
        {
            steps.Add(leg);
            foreach (var l in logic.NextLegs)
            {
                if (steps.Count >= 3) break;
                steps.Add(l);
            }
        }
        int nSteps = steps.Count;
        float ch = active ? (190f + nSteps * 36f + 14f) * u : 150f * u;
        var bc = new Rect(left, bottom - ch, colW, ch);
        Round(bc, state == NavState.Arrived ? new Color(0.06f, 0.42f, 0.2f, 0.94f) : Panel, 30f * u);
        Round(new Rect(bc.x + 16f * u, bc.y + 20f * u, 10f * u, 92f * u), accent, 5f * u);
        Text(new Rect(bc.x + 44f * u, bc.y + 14f * u, colW - 70f * u, 104f * u), logic.Instruction, 40, Color.white,
             TextAnchor.MiddleLeft, true, true);

        if (active)
        {
            var track = new Rect(bc.x + 44f * u, bc.y + 128f * u, colW - 88f * u, 14f * u);
            if (!logic.IsStairs)
            {
                float frac = Mathf.Clamp01(est.StraightMeters / Mathf.Max(0.1f, leg.meters));
                Round(track, new Color(1f, 1f, 1f, 0.22f), 7f * u);
                Round(new Rect(track.x, track.y, Mathf.Max(14f * u, track.width * frac), track.height), accent, 7f * u);
                Text(new Rect(track.x, track.yMax + 6f * u, track.width * 0.5f, 30f * u),
                     $"Trecho: {Mathf.Min(est.StraightMeters, leg.meters):0.0} / {leg.meters:0} m", 22, Muted);
                Text(new Rect(track.x + track.width * 0.4f, track.yMax + 4f * u, track.width * 0.6f, 34f * u),
                     $"{logic.RemainingTotalMeters:0} m até a saída", 28, Color.white, TextAnchor.UpperRight, true);
            }

            float sy = track.yMax + 52f * u;
            for (int i = 0; i < nSteps; i++)
            {
                var dot = new Rect(track.x, sy + i * 36f * u, 28f * u, 28f * u);
                Round(dot, i == 0 ? accent : new Color(1f, 1f, 1f, 0.25f), 14f * u);
                Text(dot, (i + 1).ToString(), 18, i == 0 ? Color.black : Color.white, TextAnchor.MiddleCenter, true);
                Text(new Rect(track.x + 42f * u, sy + i * 36f * u - 1f * u, track.width - 42f * u, 32f * u),
                     StepText(steps[i], logic.ExitId), 24, i == 0 ? Color.white : Muted);
            }

            if (state == NavState.TurnWarning && !logic.IsLastLeg && TurnConfirmed(leg, est))
                Text(new Rect(track.x + track.width * 0.4f, sy, track.width * 0.6f, 34f * u), "Curva confirmada", 24,
                     Green, TextAnchor.UpperRight, true);
        }

        // ===== Aviso temporario (QR invalido etc.) =====
        if (controller.ToastActive)
        {
            string toast = controller.ToastText;
            float tw = TextWidth(toast, 28, true) + 70f * u;
            var tr = new Rect(cx - tw / 2f, bc.y - 74f * u, tw, 58f * u);
            Round(tr, new Color(0.18f, 0.18f, 0.2f, 0.95f), 29f * u);
            Text(tr, toast, 28, Color.white, TextAnchor.MiddleCenter, true);
        }
    }

    // Seta gerada em codigo (sem asset): contorno branco + preenchimento verde.
    static Texture2D MakeArrow()
    {
        const int N = 256;
        var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float px01 = (x + 0.5f) / N, py01 = (y + 0.5f) / N;
                Color32 c = new Color32(0, 0, 0, 0);
                if (InArrow(px01, py01, 0.035f)) c = new Color32(255, 255, 255, 255);
                if (InArrow(px01, py01, 0f)) c = new Color32(30, 200, 90, 255);
                px[y * N + x] = c;
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }

    static bool InArrow(float u, float v, float m)
    {
        // cabeca (triangulo) + haste (retangulo); v cresce para cima
        bool head = v >= 0.5f - m && v <= 0.95f + m &&
                    Mathf.Abs(u - 0.5f) <= (0.38f + m) * ((0.95f + m) - v) / (0.45f + 2f * m);
        bool shaft = Mathf.Abs(u - 0.5f) <= 0.13f + m && v >= 0.05f - m && v <= 0.55f;
        return head || shaft;
    }
}
