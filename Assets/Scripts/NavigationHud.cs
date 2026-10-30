using System;
using System.Collections.Generic;
using SafeSight.Navigation;
using UnityEngine;
/// <summary>
/// HUD de navegacao (RF01, RF05, RF06, RF07): desenhado em codigo (IMGUI), fixo na tela sobre a
/// camera, sem ancoragem espacial. Le o estado da NavigationController/NavigationLogic.
/// Estados: aguardando QR (moldura), rota ativa (seta, instrucao, progresso, proximos passos),
/// novo QR necessario ("?"), chegada (check) e aviso de QR invalido. Elevador e extintor como alertas.
/// Os botoes Reiniciar e Debug sao icones no canto do cartao superior. Com o Debug ligado, so os
/// paineis de teste aparecem (o HUD some), um de cada vez.
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
    Texture2D arrowTex, restartTex, bugTex;
    GUIStyle sty;
    float u;          // unidade de escala (relativa ao tamanho da tela)
    bool debugOn;
    void Awake()
    {
        arrowTex = Bake(256, (x, y) => InArrow(x, y, 0.035f) ? 1f : 0f, (x, y) => InArrow(x, y, 0f));
        restartTex = Bake(128, RestartIcon);
        bugTex = Bake(128, BugIcon);
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
    /// <summary>Menor tamanho de fonte (em u) que faz o texto caber em maxW; ate minSize.</summary>
    float FitSize(string s, float size, float maxW, bool bold, float minSize)
    {
        float w = TextWidth(s, size, bold);
        if (w <= maxW) return size;
        return Mathf.Max(minSize, size * maxW / w);
    }
    /// <summary>Maior tamanho de fonte (em u, ate size) em que o texto, quebrando linha, cabe em w x h.</summary>
    float FitWrapSize(string s, float size, float w, float h, bool bold, float minSize)
    {
        sty.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        sty.wordWrap = true;
        for (float sz = size; sz > minSize; sz -= 2f)
        {
            sty.fontSize = Mathf.RoundToInt(sz * u);
            if (sty.CalcHeight(new GUIContent(s), w) <= h) return sz;
        }
        return minSize;
    }
    // Pilula de alerta com icone "!"; o texto encolhe para caber na largura; devolve a altura usada.
    float Pill(float x, float y, float maxW, string s, Color bg, Color fg)
    {
        float h = 56f * u;
        float size = FitSize(s, 28f, maxW - 96f * u, true, 16f);
        float w = TextWidth(s, size, true) + 96f * u;
        Round(new Rect(x, y, w, h), bg, h / 2f);
        var ic = new Rect(x + 12f * u, y + 10f * u, 36f * u, 36f * u);
        Round(ic, Color.white, 18f * u);
        Text(ic, "!", 26, bg, TextAnchor.MiddleCenter, true);
        Text(new Rect(x + 58f * u, y, w - 62f * u, h), s, size, fg, TextAnchor.MiddleLeft, true);
        return h;
    }
    /// <summary>Botao redondo com icone. active = destaque verde.</summary>
    bool IconButton(Rect r, Texture2D icon, bool active)
    {
        Round(r, active ? new Color(Green.r, Green.g, Green.b, 0.95f) : new Color(1f, 1f, 1f, 0.22f), r.height / 2f);
        float pad = r.width * 0.2f;
        var ir = new Rect(r.x + pad, r.y + pad, r.width - 2f * pad, r.height - 2f * pad);
        GUI.DrawTexture(ir, icon, ScaleMode.ScaleToFit, true, 0f, active ? Color.black : Color.white, 0f, 0f);
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
        // Cartao superior (no modo debug nao e desenhado, mas os botoes ficam no mesmo lugar).
        var tc = new Rect(left, top, colW, 130f * u);
        float bs = 56f * u;
        var bugR = new Rect(tc.xMax - 14f * u - bs, tc.y + 8f * u, bs, bs);
        var restartR = new Rect(bugR.x - 8f * u - bs, bugR.y, bs, bs);
        if (!debugOn) DrawHud(logic, est, state, leg, sa, H, left, colW, top, bottom, cx, tc, restartR);
        // Botoes (sempre visiveis, por cima de tudo)
        if (IconButton(restartR, restartTex, false)) controller.ResetNavigation();
        if (IconButton(bugR, bugTex, debugOn))
        {
            debugOn = !debugOn;
            controller.SetAllDebugPanels(debugOn);
        }
    }
    void DrawHud(NavigationLogic logic, DisplacementEstimator est, NavState state, TrajLeg leg,
                 Rect sa, float H, float left, float colW, float top, float bottom, float cx, Rect tc, Rect restartR)
    {
        Color accent = state == NavState.NeedNewQr ? Red : state == NavState.TurnWarning ? Amber : Green;
        bool active = leg != null && est != null && est.Started &&
                      state != NavState.Idle && state != NavState.Arrived;
        // ===== Cartao superior: onde estou / destino (RF05) =====
        Round(tc, Panel, 30f * u);
        Text(new Rect(tc.x + 26f * u, tc.y + 12f * u, colW * 0.5f, 26f * u), "VOCÊ ESTÁ EM", 19, Muted, bold: true);
        string id = string.IsNullOrEmpty(logic.CurrentId) ? "-" : logic.CurrentId;
        Text(new Rect(tc.x + 26f * u, tc.y + 32f * u, colW * 0.45f, 62f * u), id, 50, Color.white, bold: true);
        float idW = TextWidth(id, 50, true) + 40f * u;
        // DESTINO: rotulo ao lado dos botoes; nome logo abaixo, encolhendo para caber
        Text(new Rect(tc.x + 26f * u, tc.y + 12f * u, restartR.x - tc.x - 26f * u - 12f * u, 26f * u), "DESTINO", 19, Muted,
             TextAnchor.UpperRight, true);
        float nameAvail = colW - 26f * u - idW - 26f * u;
        string exitName = logic.ExitName;
        float nameSize = FitSize(exitName, 34f, nameAvail, true, 18f);
        Text(new Rect(tc.xMax - 26f * u - nameAvail, tc.y + 56f * u, nameAvail, 44f * u), exitName, nameSize, Green,
             TextAnchor.UpperRight, true);
        var cp = logic.CurrentPoint;
        if (cp != null && !string.IsNullOrEmpty(cp.description))
            Text(new Rect(tc.x + 26f * u, tc.y + 98f * u, colW - 52f * u, 28f * u), cp.description, 21, Muted);
        // ===== Alertas (RF07) =====
        float ay = tc.yMax + 10f * u;
        if (cp != null)
        {
            if (logic.ShowElevatorAlert) ay += Pill(left, ay, colW, "Não utilize o elevador", Red, Color.white) + 8f * u;
            if (!string.IsNullOrEmpty(logic.ExtinguisherText))
                ay += Pill(left, ay, colW, "Extintor: " + logic.ExtinguisherText, Amber, new Color(0.15f, 0.1f, 0f)) + 8f * u;
        }
        // ===== Centro: seta / estado =====
        float cy = H * 0.46f;
        float pulse = 1f + 0.035f * Mathf.Sin(Time.realtimeSinceStartup * 3f);
        float D = 240f * u * pulse;
        var circle = new Rect(cx - D / 2f, cy - D / 2f, D, D);
        if (state == NavState.Following || state == NavState.TurnWarning)
        {
            Round(circle, new Color(0.05f, 0.07f, 0.11f, 0.45f), D / 2f);
            RingShape(circle, accent, 6f * u);
            float angle = 0f;
            if (state == NavState.TurnWarning && leg != null && !logic.IsLastLeg)
                angle = leg.turn == "right" ? 90f : leg.turn == "left" ? -90f : 0f;
            float S = 165f * u * pulse;
            var ar = new Rect(cx - S / 2f, cy - S / 2f, S, S);
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, ar.center);
            if (arrowTex != null) GUI.DrawTexture(ar, arrowTex);
            GUI.matrix = old;
        }
        else if (state == NavState.Arrived)
        {
            Round(circle, new Color(0.12f, 0.78f, 0.35f, 0.95f), D / 2f);
            var v = new Vector2(cx - 14f * u, cy + 40f * u); // vertice do "check"
            RotatedBar(new Vector2(v.x - 35f * u, v.y - 20f * u), 66f * u, 22f * u, 45f, Color.white);
            RotatedBar(new Vector2(v.x + 40f * u, v.y - 47f * u), 138f * u, 22f * u, -50f, Color.white);
        }
        else if (state == NavState.NeedNewQr)
        {
            Round(circle, new Color(0.90f, 0.22f, 0.22f, 0.90f), D / 2f);
            Text(circle, "?", 125, Color.white, TextAnchor.MiddleCenter, true);
        }
        else // Idle: moldura de leitura de QR
        {
            float s = 250f * u, L = 54f * u, th = 8f * u;
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
        float ch = active ? (190f + nSteps * 36f + 14f) * u : 170f * u;
        var bc = new Rect(left, bottom - ch, colW, ch);
        Round(bc, state == NavState.Arrived ? new Color(0.06f, 0.42f, 0.2f, 0.94f) : Panel, 30f * u);
        float instH = active ? 104f * u : ch - 28f * u;
        var instR = new Rect(bc.x + 44f * u, bc.y + 14f * u, colW - 70f * u, instH);
        float instSize = FitWrapSize(logic.Instruction, 40f, instR.width, instR.height, true, 22f);
        Round(new Rect(bc.x + 16f * u, bc.y + 20f * u, 10f * u, instH - 12f * u), accent, 5f * u);
        Text(instR, logic.Instruction, instSize, Color.white, TextAnchor.MiddleLeft, true, true);
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
            float tsize = FitSize(toast, 28f, colW - 70f * u, true, 16f);
            float tw = TextWidth(toast, tsize, true) + 70f * u;
            var tr = new Rect(cx - tw / 2f, bc.y - 74f * u, tw, 58f * u);
            Round(tr, new Color(0.18f, 0.18f, 0.2f, 0.95f), 29f * u);
            Text(tr, toast, tsize, Color.white, TextAnchor.MiddleCenter, true);
        }
    }
    // ---------- Texturas geradas em codigo (sem assets) ----------
    /// <summary>Gera uma textura branca com alfa vindo de cov (x,y em 0..1, y para cima), com suavizacao 3x3.</summary>
    static Texture2D Bake(int n, Func<float, float, float> cov)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        const int SS = 3;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float a = 0f;
                for (int sy = 0; sy < SS; sy++)
                    for (int sx = 0; sx < SS; sx++)
                        a += cov((x + (sx + 0.5f) / SS) / n, (y + (sy + 0.5f) / SS) / n);
                a /= SS * SS;
                px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * a));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
    // Seta: contorno branco + preenchimento verde (cores proprias, nao tingida).
    static Texture2D Bake(int n, Func<float, float, float> outline, Func<float, float, bool> fill)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float fx = (x + 0.5f) / n, fy = (y + 0.5f) / n;
                Color32 c = new Color32(0, 0, 0, 0);
                if (outline(fx, fy) > 0f) c = new Color32(255, 255, 255, 255);
                if (fill(fx, fy)) c = new Color32(30, 200, 90, 255);
                px[y * n + x] = c;
            }
        tex.SetPixels32(px);
        tex.Apply();
        return tex;
    }
    static bool InArrow(float x, float y, float m)
    {
        // cabeca (triangulo) + haste (retangulo); y cresce para cima
        bool head = y >= 0.5f - m && y <= 0.95f + m &&
                    Mathf.Abs(x - 0.5f) <= (0.38f + m) * ((0.95f + m) - y) / (0.45f + 2f * m);
        bool shaft = Mathf.Abs(x - 0.5f) <= 0.13f + m && y >= 0.05f - m && y <= 0.55f;
        return head || shaft;
    }
    static float DistSeg(float px, float py, float ax, float ay, float bx, float by)
    {
        float vx = bx - ax, vy = by - ay;
        float t = Mathf.Clamp01(((px - ax) * vx + (py - ay) * vy) / (vx * vx + vy * vy));
        float dx = px - (ax + t * vx), dy = py - (ay + t * vy);
        return Mathf.Sqrt(dx * dx + dy * dy);
    }
    static bool InTri(float px, float py, float ax, float ay, float bx, float by, float cx, float cy)
    {
        float d1 = (px - bx) * (ay - by) - (ax - bx) * (py - by);
        float d2 = (px - cx) * (by - cy) - (bx - cx) * (py - cy);
        float d3 = (px - ax) * (cy - ay) - (cx - ax) * (py - ay);
        bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
        return !(neg && pos);
    }
    // Icone "reiniciar": seta circular no sentido horario.
    static float RestartIcon(float x, float y)
    {
        const float R = 0.27f, T = 0.09f;
        float dx = x - 0.5f, dy = y - 0.5f;
        float r = Mathf.Sqrt(dx * dx + dy * dy);
        float a = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;      // graus, sentido horario a partir do topo
        if (a < 0f) a += 360f;
        bool arc = Mathf.Abs(r - R) <= T / 2f && a >= 55f;   // abertura entre 0 e 55 graus
        bool head = InTri(x, y, 0.5f + 0.13f, 0.5f + R, 0.5f, 0.5f + R + 0.11f, 0.5f, 0.5f + R - 0.11f);
        return arc || head ? 1f : 0f;
    }
    // Icone "debug": inseto.
    static float BugIcon(float x, float y)
    {
        float ex = (x - 0.5f) / 0.16f, ey = (y - 0.42f) / 0.24f;
        bool body = ex * ex + ey * ey <= 1f;
        float hx = x - 0.5f, hy = y - 0.72f;
        bool head = hx * hx + hy * hy <= 0.09f * 0.09f;
        const float L = 0.0225f;                              // meia espessura das pernas
        float mx = 1f - x;                                    // espelho para o lado direito
        bool legs =
            DistSeg(x, y, 0.36f, 0.52f, 0.18f, 0.60f) <= L || DistSeg(mx, y, 0.36f, 0.52f, 0.18f, 0.60f) <= L ||
            DistSeg(x, y, 0.34f, 0.42f, 0.15f, 0.42f) <= L || DistSeg(mx, y, 0.34f, 0.42f, 0.15f, 0.42f) <= L ||
            DistSeg(x, y, 0.36f, 0.32f, 0.18f, 0.24f) <= L || DistSeg(mx, y, 0.36f, 0.32f, 0.18f, 0.24f) <= L ||
            DistSeg(x, y, 0.46f, 0.79f, 0.38f, 0.92f) <= L * 0.8f || DistSeg(mx, y, 0.46f, 0.79f, 0.38f, 0.92f) <= L * 0.8f;
        return body || head || legs ? 1f : 0f;
    }
}
