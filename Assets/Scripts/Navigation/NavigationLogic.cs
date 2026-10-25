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

        readonly TrajData data;
        readonly Dictionary<string, TrajPoint> points = new Dictionary<string, TrajPoint>();
        readonly Dictionary<string, TrajRoute> routes = new Dictionary<string, TrajRoute>();

        TrajRoute route;
        string currentId = "";
        bool arrived;
        float lastQrTime = -999f;
        float straightMeters;

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

        /// <summary>Metros que faltam para o fim do trecho atual (nunca negativo).</summary>
        public float RemainingLegMeters =>
            route == null ? 0f : Mathf.Max(0f, route.legs[0].meters - straightMeters);

        /// <summary>Metros ate a saida: o que falta do trecho atual + os proximos trechos.</summary>
        public float RemainingTotalMeters
        {
            get
            {
                if (route == null) return 0f;
                float total = RemainingLegMeters;
                for (int i = 1; i < route.legs.Count; i++) total += route.legs[i].meters;
                return total;
            }
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
            State = NavState.Following;
            Message = "";
            return QrResult.RouteLoaded;
        }

        // ---------- Estado da navegacao (RF04, RF06, RF09, RF10) ----------

        /// <param name="straight">Distancia em linha reta desde o ultimo QR (estimador do ARCore).</param>
        /// <param name="trackingLost">Rastreamento do ARCore perdido.</param>
        public void Tick(float straight, bool trackingLost)
        {
            if (arrived) { State = NavState.Arrived; return; }
            if (route == null) { State = NavState.Idle; return; }
            // Depois de perder a referencia, so um QR valido pode liberar a navegacao.
            if (State == NavState.NeedNewQr) return;

            straightMeters = straight;
            if (trackingLost) { State = NavState.NeedNewQr; return; }

            var leg = route.legs[0];

            // Escada: a distancia medida na horizontal nao vale. Sem aviso por distancia;
            // o QR no fim da escada continua a rota.
            if (leg.IsStairs) { State = NavState.Following; return; }

            float remaining = leg.meters - straight;
            if (straight > leg.meters + OvershootMeters) State = NavState.NeedNewQr;
            else if (IsLastLeg && remaining <= 0f) { arrived = true; State = NavState.Arrived; }
            else if (remaining <= WarnMeters) State = NavState.TurnWarning;
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
                switch (State)
                {
                    case NavState.Idle:
                        return "Aponte a câmera para um QR Code para começar";
                    case NavState.Following:
                        if (IsStairs) return "Use a escada";
                        return $"Siga em frente {RemainingLegMeters:0} m";
                    case NavState.TurnWarning:
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
