using System.IO;
using NUnit.Framework;
using SafeSight.Navigation;
using UnityEngine;

/// <summary>
/// Testes da navegacao com TODAS as rotas reais do Predio L (Assets/Resources/trajeto.json),
/// partindo de cada QR de partida cadastrado. Cobrem: percurso completo, aviso de curva,
/// QR invalido, rastreamento perdido e chegada.
/// </summary>
public class NavigationRotasReaisTests
{
    static NavigationLogic NewLogic()
    {
        string path = Path.Combine(Application.dataPath, "Resources", "trajeto.json");
        return NavigationLogic.FromJson(File.ReadAllText(path));
    }

    [Test]
    public void TodasAsRotas_PercorridasDaPartidaAteAChegada()
    {
        var data = NewLogic().Data;
        Assert.Greater(data.routes.Count, 0);

        foreach (var r in data.routes)
        {
            var logic = NewLogic();
            float t = 0f;
            string id = r.startId;
            int guard = 0;

            while (id != logic.ExitId)
            {
                Assert.Less(guard++, 30, $"rota {r.startId}: loop sem fim");
                Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(id, t),
                    $"rota {r.startId}: o QR {id} deveria carregar uma rota");

                var leg = logic.CurrentLeg;
                Assert.IsNotNull(leg, $"rota {r.startId}: ponto {id} sem trecho");
                Assert.Greater(leg.meters, 0f, $"rota {r.startId}: trecho de {id} com 0 m");
                Assert.AreEqual(NavState.Following, logic.State, $"rota {r.startId}: apos ler {id}");

                if (logic.IsStairs)
                {
                    logic.Tick(leg.meters + 10f, false);           // escada: sem aviso por distancia
                    Assert.AreEqual(NavState.Following, logic.State, $"rota {r.startId}: escada em {id}");
                    StringAssert.Contains("escada", logic.Instruction);
                }
                else
                {
                    logic.Tick(leg.meters - 0.5f, false);          // faltam 0,5 m: aviso de curva
                    Assert.AreEqual(NavState.TurnWarning, logic.State, $"rota {r.startId}: aviso em {id}");
                    if (logic.IsLastLeg) StringAssert.Contains("saída", logic.Instruction);
                    else StringAssert.Contains(NavigationLogic.TurnText(leg.turn), logic.Instruction);
                }

                t += 5f;
                id = leg.toId;                                      // le o QR do proximo ponto
            }

            Assert.AreEqual(QrResult.Arrived, logic.OnQrRead(id, t), $"rota {r.startId}: QR da saida");
            Assert.AreEqual(NavState.Arrived, logic.State, $"rota {r.startId}: estado final");
            StringAssert.Contains("chegou", logic.Instruction);
        }
    }

    [Test]
    public void QrInvalido_NoMeioDeQualquerRota_NaoMudaOEstado()
    {
        foreach (var r in NewLogic().Data.routes)
        {
            var logic = NewLogic();
            logic.OnQrRead(r.startId, 0f);
            logic.Tick(0.5f, false);
            var state = logic.State;
            float remaining = logic.RemainingLegMeters;

            Assert.AreEqual(QrResult.Unknown, logic.OnQrRead("QR-INVALIDO", 5f), $"rota {r.startId}");
            StringAssert.Contains("não reconhecido", logic.Message);
            Assert.AreEqual(state, logic.State, $"rota {r.startId}: estado mudou");
            Assert.AreEqual(r.startId, logic.CurrentId, $"rota {r.startId}: ponto mudou");
            Assert.AreEqual(remaining, logic.RemainingLegMeters, 0.001f, $"rota {r.startId}: distancia mudou");
        }
    }

    [Test]
    public void RastreamentoPerdido_EmQualquerRota_PedeNovoQr_E_RecuperaComOProximo()
    {
        foreach (var r in NewLogic().Data.routes)
        {
            var logic = NewLogic();
            logic.OnQrRead(r.startId, 0f);
            string next = logic.CurrentLeg.toId;

            logic.Tick(1.0f, true);
            Assert.AreEqual(NavState.NeedNewQr, logic.State, $"rota {r.startId}");
            StringAssert.Contains("QR", logic.Instruction);

            var res = logic.OnQrRead(next, 5f);
            if (next == logic.ExitId)
            {
                Assert.AreEqual(QrResult.Arrived, res, $"rota {r.startId}: recuperar na saida");
                Assert.AreEqual(NavState.Arrived, logic.State, $"rota {r.startId}");
            }
            else
            {
                Assert.AreEqual(QrResult.RouteLoaded, res, $"rota {r.startId}: recuperar em {next}");
                Assert.AreEqual(NavState.Following, logic.State, $"rota {r.startId}");
            }
        }
    }

    [Test]
    public void QrDaSaida_DeQualquerPartida_ChegouNaSaida()
    {
        foreach (var r in NewLogic().Data.routes)
        {
            var logic = NewLogic();
            logic.OnQrRead(r.startId, 0f);
            Assert.AreEqual(QrResult.Arrived, logic.OnQrRead(logic.ExitId, 5f), $"rota {r.startId}");
            Assert.AreEqual(NavState.Arrived, logic.State, $"rota {r.startId}");
            StringAssert.Contains("chegou", logic.Instruction);
        }
    }
}
