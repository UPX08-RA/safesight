using System;
using System.Collections.Generic;

namespace SafeSight.Navigation
{
    // Dados do trajeto (Assets/Resources/trajeto.json), lidos com JsonUtility.
    // O JSON e gerado por tools/gerar_trajeto.py a partir da planilha de mapeamento.

    [Serializable]
    public class TrajPoint
    {
        public string id;
        public string description;
        public bool noElevator = true;
        public string extinguisher;

        // Orientacao do QR: azimute (graus; 0 = topo da planta, sentido horario) do lado para onde a
        // FACE do QR aponta. O usuario le o QR olhando na direcao oposta. hasQrFacing = false: sem dado.
        public bool hasQrFacing;
        public float qrFacingDeg;
    }

    /// <summary>Trecho da rota. "turn" vale no FIM do trecho: left | right | none.</summary>
    [Serializable]
    public class TrajLeg
    {
        public string toId;
        public float meters;
        public string turn;
        public string kind; // "stairs" na escada; vazio nos demais

        // Curva no MEIO do trecho (opcional; viaMeters = 0 -> sem curva intermediaria). O usuario anda viaMeters,
        // vira (viaTurn: left | right, 90 graus) e anda afterViaMeters ate o fim do trecho, onde vale "turn".
        // "meters" continua sendo a distancia entre os dois QR na planta.
        public float viaMeters;
        public string viaTurn;
        public float afterViaMeters;

        public bool IsStairs => kind == "stairs";
        public bool HasVia => viaMeters > 0f && afterViaMeters > 0f && (viaTurn == "left" || viaTurn == "right");
    }

    [Serializable]
    public class TrajRoute
    {
        public string startId;
        // Sentido da caminhada no primeiro trecho (mesmo azimute da planta). hasStartHeading = false: sem dado.
        public bool hasStartHeading;
        public float startHeadingDeg;
        public List<TrajLeg> legs;
    }

    [Serializable]
    public class TrajData
    {
        public string exitId;
        public string exitName;
        public List<TrajPoint> points;
        public List<TrajRoute> routes;
    }
}
