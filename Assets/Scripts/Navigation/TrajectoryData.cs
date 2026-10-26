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
    }

    /// <summary>Trecho da rota. "turn" vale no FIM do trecho: left | right | none.</summary>
    [Serializable]
    public class TrajLeg
    {
        public string toId;
        public float meters;
        public string turn;
        public string kind; // "stairs" na escada; vazio nos demais

        public bool IsStairs => kind == "stairs";
    }

    [Serializable]
    public class TrajRoute
    {
        public string startId;
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
