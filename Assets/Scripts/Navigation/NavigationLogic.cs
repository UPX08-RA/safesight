using System.Collections.Generic;
using UnityEngine;

namespace SafeSight.Navigation
{
    public enum NavState { Idle, Following, TurnWarning, NeedNewQr, Arrived }

    public enum QrResult { Ignored, Unknown, NoRoute, RouteLoaded, Arrived }

    /// <summary>
    /// Logica de navegacao por rota cadastrada (sem MonoBehaviour, testavel sem ARCore).
    /// Sem calculo de rota: cada ponto tem sua rota pre-cadastrada ate a saida-alvo.
    /// Usa so o primeiro trecho da rota; a leitura do proximo QR carrega a rota seguinte.
    /// Requisitos: RF03, RF04, RF06, RF07, RF09, RF10.
    /// </summary>
    public class NavigationLogic
    {
        public float WarnMeters = 1.0f;        // avisa a curva quando faltar esta distancia
        public float OvershootMeters = 1.5f;   // passou do trecho por mais que isso -> novo QR
        public float RescanGapSeconds = 1.5f;  // ignora o mesmo QR repetido dentro deste intervalo

        // Orientacao do QR: o QR fica numa coluna virado para o corredor; o usuario le olhando para
        // ele, entao pode estar de lado em relacao ao trecho. Com o dado cadastrado, o app diz quanto girar.
        public bool UseQrOrientation = true;
        public float AlignToleranceDegrees = 30f; // ate este erro de giro, o usuario conta como alinhado
        public float AlignWindowMeters = 1.5f;    // depois de andar isto, nao pede mais o giro inicial
        public float ViaTurnConfirmDegrees = 60f; // giro que confirma a curva no meio do trecho

        readonly TrajData data;
        readonly Dictionary<string, TrajPoint> points = new Dictionary<string, TrajPoint>();
        readonly Dictionary<string, TrajRoute> routes = new Dictionary<string, TrajRoute>();

        TrajRoute route;
        string currentId = "";
        bool arrived;
        float lastQrTime = -999f;
        float straightMeters;
        float yawDelta;     // giro (graus, + = direita) desde a leitura do QR
        bool aligned;       // ja ficou alinhado ao trecho (ou ja andou): nao pede mais o giro inicial
        bool viaDone;       // ja fez a curva do meio do trecho (so em trechos com curva intermediaria)
        float viaBase;      // distancia em linha reta (m) no momento da curva do meio do trecho

        public NavState State { get; private set; } = NavState.Idle;

        /// <summary>Mensagem curta para o usuario (QR invalido, ponto sem rota). Vazia se nao houver.</summary>
        public string Message { get; private set; } = "";

        public string CurrentId => currentId;
        public string ExitId => data.exitId;
        public string ExitName => data.exitName;
        public TrajData Data => data;

        public NavigationLogic(TrajData data)
        {
            this.data = data;
            foreach (var p in data.points) points[p.id] = p;
            foreach (var r in data.routes) routes[r.startId] = r;
        }

        public static NavigationLogic FromJson(string json)
        {
            return new NavigationLogic(JsonUtility.FromJson<TrajData>(json));
        }

        // ---------- Ponto e trecho atuais ----------

        public TrajPoint CurrentPoint =>
            currentId != "" && points.TryGetValue(currentId, out var p) ? p : null;

        public TrajLeg CurrentLeg => route != null ? route.legs[0] : null;

        /// <summary>Proximos trechos depois do atual (para a lista de proximos passos).</summary>
        public IEnumerable<TrajLeg> NextLegs
        {
            get
            {
                if (route == null) yield break;
                for (int i = 1; i < route.legs.Count; i++) yield return route.legs[i];
            }
        }

        public bool IsLastLeg => route != null && route.legs.Count == 1;
        public bool IsStairs => CurrentLeg != null && CurrentLeg.IsStairs;

        /// <summary>Alerta de elevador (RF07): sempre que houver um ponto atual.</summary>
        public bool ShowElevatorAlert => CurrentPoint != null && CurrentPoint.noElevator;

        /// <summary>Texto do extintor cadastrado para o ponto atual (RF07); vazio se nao houver.</summary>
        public string ExtinguisherText => CurrentPoint != null ? CurrentPoint.extinguisher ?? "" : "";

        /// <summary>O trecho atual tem uma curva no meio (ex.: P6 -> P7).</summary>
        public bool HasVia => route != null && route.legs[0].HasVia && !route.legs[0].IsStairs;

        /// <summary>Ainda na primeira parte de um trecho com curva no meio.</summary>
        public bool InViaStage => HasVia && !viaDone;

        /// <summary>Curva da etapa atual: a do meio do trecho (1a etapa) ou a do fim do trecho.</summary>
        public string CurrentTurn =>
            route == null ? "none" : InViaStage ? route.legs[0].viaTurn : route.legs[0].turn;

        /// <summary>Texto de etapa para o painel de debug ("" se o trecho nao tem curva no meio).</summary>
        public string StageText => !HasVia ? "" : (viaDone ? "etapa 2/2" : "etapa 1/2");

        // Depois da curva do meio, a distancia andada na 2a etapa sai do deslocamento em linha reta (a curva e de 90 graus).
        float AfterViaWalked => Mathf.Sqrt(Mathf.Max(0f, straightMeters * straightMeters - viaBase * viaBase));

        /// <summary>Metros que faltam para o fim do trecho (ou da etapa) atual (nunca negativo).</summary>
        public float RemainingLegMeters
        {
            get
            {
                if (route == null) return 0f;
                var leg = route.legs[0];
                if (HasVia)
                    return Mathf.Max(0f, viaDone ? leg.afterViaMeters - AfterViaWalked : leg.viaMeters - straightMeters);
                return Mathf.Max(0f, leg.meters - straightMeters);
            }
        }

        /// <summary>Metros ate a saida: o que falta do trecho atual + os proximos trechos.</summary>
        public float RemainingTotalMeters
        {
            get
            {
                if (route == null) return 0f;
                float total = RemainingLegMeters;
                if (InViaStage) total += route.legs[0].afterViaMeters;
                for (int i = 1; i < route.legs.Count; i++) total += route.legs[i].meters;
                return total;
            }
        }

        // ---------- Orientacao do QR ----------

        /// <summary>Ha dado de orientacao para o ponto e a rota atuais (e o recurso esta ligado).</summary>
        public bool HasInitialTurn =>
            UseQrOrientation && route != null && route.hasStartHeading &&
            CurrentPoint != null && CurrentPoint.hasQrFacing;

        /// <summary>
        /// Giro (graus, + = direita, - = esquerda, ate 180) que o usuario precisa fazer depois de ler o QR
        /// para ficar virado no sentido do primeiro trecho. Supoe que ele leu o QR de frente, ou seja,
        /// olhando na direcao oposta a da face do QR.
        /// </summary>
        public float InitialTurnDegrees =>
            HasInitialTurn ? Mathf.DeltaAngle(CurrentPoint.qrFacingDeg + 180f, route.startHeadingDeg) : 0f;

        /// <summary>Quanto ainda falta girar para ficar alinhado ao trecho (+ = direita).</summary>
        public float AlignErrorDegrees =>
            HasInitialTurn ? Mathf.DeltaAngle(yawDelta, InitialTurnDegrees) : 0f;

        /// <summary>O app deve pedir o giro inicial (o usuario ainda esta de lado em relacao ao trecho).</summary>
        public bool NeedsAlignment =>
            HasInitialTurn && !aligned && !IsStairs &&
            (State == NavState.Following || State == NavState.TurnWarning) &&
            Mathf.Abs(AlignErrorDegrees) > AlignToleranceDegrees;

        /// <summary>Giro (graus, + = direita) desde que o usuario ficou alinhado ao trecho; base da "curva confirmada".</summary>
        public float TurnProgressDegrees
        {
            get
            {
                float g = HasInitialTurn ? Mathf.DeltaAngle(InitialTurnDegrees, yawDelta) : yawDelta;
                if (HasVia && viaDone) // desconta a curva de 90 graus do meio do trecho
                    g = Mathf.DeltaAngle(route.legs[0].viaTurn == "left" ? -90f : 90f, g);
                return g;
            }
        }

        public static string AlignText(float errorDegrees)
        {
            if (Mathf.Abs(errorDegrees) > 150f) return "Dê meia-volta";
            return errorDegrees > 0f ? "Vire à direita" : "Vire à esquerda";
        }

        // ---------- Leitura de QR (RF02, RF03) ----------

        /// <param name="now">Tempo em segundos (Time.realtimeSinceStartup no app).</param>
        public QrResult OnQrRead(string id, float now)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Message = "QR não reconhecido. Tente outro código";
                return QrResult.Unknown;
            }

            // So leituras do QR atual contam para o intervalo. Um QR estranho nao
            // deve impedir a recuperacao da posicao por uma nova leitura valida.
            if (id == currentId)
            {
                bool gap = now - lastQrTime >= RescanGapSeconds;
                lastQrTime = now;
                if (!gap) return QrResult.Ignored; // mesmo QR ainda no campo de visao
            }

            if (id == data.exitId) // a saida tambem esta em "points": checar antes
            {
                lastQrTime = now;
                currentId = id;
                route = null;
                arrived = true;
                State = NavState.Arrived;
                Message = "";
                return QrResult.Arrived;
            }

            if (!points.ContainsKey(id))
            {
                Message = "QR não reconhecido. Tente outro código";
                return QrResult.Unknown;
            }

            if (!routes.TryGetValue(id, out var r) || r.legs == null || r.legs.Count == 0)
            {
                Message = "Sem rota cadastrada para este ponto";
                return QrResult.NoRoute;
            }

            lastQrTime = now;
            currentId = id;
            route = r;
            arrived = false;
            straightMeters = 0f;
            yawDelta = 0f;
            aligned = false;
            viaDone = false;
            viaBase = 0f;
            State = NavState.Following;
            Message = "";
            return QrResult.RouteLoaded;
        }

        // ---------- Estado da navegacao (RF04, RF06, RF09, RF10) ----------

        /// <param name="straight">Distancia em linha reta desde o ultimo QR (estimador do ARCore).</param>
        /// <param name="trackingLost">Rastreamento do ARCore perdido.</param>
        /// <param name="yawDeltaDegrees">Giro (graus, + = direita) desde a leitura do QR (estimador do ARCore).</param>
        public void Tick(float straight, bool trackingLost, float yawDeltaDegrees = 0f)
        {
            if (arrived) { State = NavState.Arrived; return; }
            if (route == null) { State = NavState.Idle; return; }
            // Depois de perder a referencia, so um QR valido pode liberar a navegacao.
            if (State == NavState.NeedNewQr) return;

            straightMeters = straight;
            yawDelta = yawDeltaDegrees;
            if (HasInitialTurn && !aligned &&
                (Mathf.Abs(AlignErrorDegrees) <= AlignToleranceDegrees || straight > AlignWindowMeters))
                aligned = true; // alinhou (ou ja esta andando): nao volta a pedir o giro neste trecho
            // Curva no meio do trecho: confirmada pelo giro (depois de alinhado e de andar a metade da 1a etapa).
            if (HasVia && !viaDone && (!HasInitialTurn || aligned) && straight >= 0.5f * route.legs[0].viaMeters)
            {
                float g = TurnProgressDegrees;
                bool left = route.legs[0].viaTurn == "left";
                if (left ? g <= -ViaTurnConfirmDegrees : g >= ViaTurnConfirmDegrees)
                {
                    viaDone = true;
                    viaBase = straight;
                }
            }
            if (trackingLost) { State = NavState.NeedNewQr; return; }

            var leg = route.legs[0];

            // Escada: a distancia medida na horizontal nao vale. Sem aviso por distancia;
            // o QR no fim da escada continua a rota.
            if (leg.IsStairs) { State = NavState.Following; return; }

            float remaining = RemainingLegMeters;
            float warn = InViaStage ? Mathf.Min(WarnMeters, 0.5f * leg.viaMeters) : WarnMeters;
            if (straight > leg.meters + OvershootMeters) State = NavState.NeedNewQr;
            else if (IsLastLeg && !InViaStage && remaining <= 0f) { arrived = true; State = NavState.Arrived; }
            else if (remaining <= warn) State = NavState.TurnWarning;
            else State = NavState.Following;
        }

        // ---------- Textos ----------

        public static string TurnText(string turn)
        {
            switch (turn)
            {
                case "right": return "vire à direita";
                case "left": return "vire à esquerda";
                default: return "continue em frente";
            }
        }

        public string Instruction
        {
            get
            {
                if (NeedsAlignment)
                    return $"{AlignText(AlignErrorDegrees)} e siga em frente {RemainingLegMeters:0} m";
                switch (State)
                {
                    case NavState.Idle:
                        return "Aponte a câmera para um QR Code para começar";
                    case NavState.Following:
                        if (IsStairs) return "Use a escada";
                        return $"Siga em frente {RemainingLegMeters:0} m";
                    case NavState.TurnWarning:
                        if (InViaStage)
                            return RemainingLegMeters > 0.2f
                                ? $"Em {RemainingLegMeters:0.0} m, {TurnText(CurrentTurn)}"
                                : $"Agora, {TurnText(CurrentTurn)}";
                        if (IsLastLeg) return "A saída está logo à frente";
                        return RemainingLegMeters > 0.2f
                            ? $"Em {RemainingLegMeters:0.0} m, {TurnText(CurrentLeg.turn)}"
                            : "Leia o próximo QR Code neste ponto";
                    case NavState.NeedNewQr:
                        return "Posição incerta. Leia o próximo QR Code";
                    case NavState.Arrived:
                        return "Você chegou à saída";
                }
                return "";
            }
        }
    }
}
