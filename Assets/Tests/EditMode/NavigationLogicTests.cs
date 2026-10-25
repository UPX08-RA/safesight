using System.IO;
using NUnit.Framework;
using SafeSight.Navigation;
using UnityEngine;

/// <summary>Testes da navegacao com as rotas reais do Predio L (Assets/Resources/trajeto.json).</summary>
public class NavigationLogicTests
{
    NavigationLogic logic;

    [SetUp]
    public void SetUp()
    {
        string path = Path.Combine(Application.dataPath, "Resources", "trajeto.json");
        logic = NavigationLogic.FromJson(File.ReadAllText(path));
    }

    // L-0-04: primeiro trecho de 9,0 m (L-0-03); rota total 27,4 m (9,0 + 9,4 + 9,0)
    const string P9 = "L-0-04";

    [Test]
    public void QrValido_CarregaRota_E_ComecaSeguindo()
    {
        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(P9, 0f));
        Assert.AreEqual(NavState.Following, logic.State);
        Assert.AreEqual("L-0-03", logic.CurrentLeg.toId);
        Assert.AreEqual(9.0f, logic.CurrentLeg.meters, 0.001f);
    }

    [Test]
    public void QrDesconhecido_NaoMudaEstado_E_AvisaMensagem()
    {
        Assert.AreEqual(QrResult.Unknown, logic.OnQrRead("https://github.com/x", 0f));
        Assert.AreEqual(NavState.Idle, logic.State);
        StringAssert.Contains("não reconhecido", logic.Message);
    }

    [Test]
    public void QrDaSaida_ChegouNaSaida()
    {
        Assert.AreEqual(QrResult.Arrived, logic.OnQrRead(logic.ExitId, 0f));
        Assert.AreEqual(NavState.Arrived, logic.State);
        StringAssert.Contains("chegou", logic.Instruction);
    }

    [Test]
    public void TrechoDe9m_Following_Aviso_E_NovoQr()
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(5.0f, false);
        Assert.AreEqual(NavState.Following, logic.State);
        logic.Tick(8.2f, false);                        // faltam 0,8 m
        Assert.AreEqual(NavState.TurnWarning, logic.State);
        logic.Tick(10.6f, false);                       // passou 9 + 1,5
        Assert.AreEqual(NavState.NeedNewQr, logic.State);
    }

    [Test]
    public void UltimoTrecho_DistanciaZero_Chegou()
    {
        // L-0-02: um unico trecho de 9 m ate a saida
        logic.OnQrRead("L-0-02", 0f);
        Assert.IsTrue(logic.IsLastLeg);
        logic.Tick(9.0f, false);
        Assert.AreEqual(NavState.Arrived, logic.State);
    }

    [Test]
    public void RastreamentoPerdido_PedeNovoQr()
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(2.0f, true);
        Assert.AreEqual(NavState.NeedNewQr, logic.State);
    }

    [Test]
    public void Escada_DistanciaNaoGeraNovoQr()
    {
        // L-1-01: o primeiro trecho e a escada (13,2 m)
        logic.OnQrRead("L-1-01", 0f);
        Assert.IsTrue(logic.IsStairs);
        logic.Tick(20.0f, false);
        Assert.AreEqual(NavState.Following, logic.State);
        StringAssert.Contains("escada", logic.Instruction);
        logic.Tick(5.0f, true);                         // perda de rastreamento continua valendo
        Assert.AreEqual(NavState.NeedNewQr, logic.State);
    }

    [Test]
    public void MesmoQrRepetido_NaoReinicia_ATempoMasReiniciaDepois()
    {
        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(P9, 0f));
        logic.Tick(5.0f, false);
        Assert.AreEqual(QrResult.Ignored, logic.OnQrRead(P9, 0.5f));
        Assert.AreEqual(4.0f, logic.RemainingLegMeters, 0.001f);   // nao zerou
        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(P9, 5.0f));
        Assert.AreEqual(9.0f, logic.RemainingLegMeters, 0.001f);   // reiniciou
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("QR-DESCONHECIDO")]
    public void QrInvalidoDuranteRota_PreservaPosicao_E_Avisa(string qr)
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(4f, false);

        Assert.AreEqual(QrResult.Unknown, logic.OnQrRead(qr, 2f));
        Assert.AreEqual(NavState.Following, logic.State);
        Assert.AreEqual(P9, logic.CurrentId);
        Assert.AreEqual(5f, logic.RemainingLegMeters, 0.001f);
        Assert.AreEqual(23.4f, logic.RemainingTotalMeters, 0.01f);
        StringAssert.Contains("não reconhecido", logic.Message);
    }

    [Test]
    public void QrDesconhecido_NaoAdiaReleituraDoQrAtual()
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(5f, false);

        Assert.AreEqual(QrResult.Unknown, logic.OnQrRead("QR-DESCONHECIDO", 1.9f));
        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(P9, 2f));
        Assert.AreEqual(9f, logic.RemainingLegMeters, 0.001f);
        Assert.IsEmpty(logic.Message);
    }

    [Test]
    public void PontoSemRota_PreservaNavegacao_E_NaoAdiaQrValido()
    {
        // O JSON real tem rotas para todos os pontos exceto a saida.
        var data = logic.Data;
        data.points.Add(new TrajPoint { id = "SEM-ROTA" });
        logic = new NavigationLogic(data);
        logic.OnQrRead(P9, 0f);
        logic.Tick(4f, false);

        Assert.AreEqual(QrResult.NoRoute, logic.OnQrRead("SEM-ROTA", 1.9f));
        Assert.AreEqual(NavState.Following, logic.State);
        Assert.AreEqual(P9, logic.CurrentId);
        Assert.AreEqual(5f, logic.RemainingLegMeters, 0.001f);
        StringAssert.Contains("Sem rota cadastrada", logic.Message);
        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(P9, 2f));
        Assert.IsEmpty(logic.Message);
    }

    [Test]
    public void MesmoQr_IntervaloExatoDeUmSegundoEMeio_PermiteReleitura()
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(5f, false);

        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(P9, 1.5f));
        Assert.AreEqual(9f, logic.RemainingLegMeters, 0.001f);
    }

    [Test]
    public void MesmoQrContinuamenteVisivel_NaoReiniciaDistancia()
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(5f, false);

        for (int i = 1; i <= 5; i++)
            Assert.AreEqual(QrResult.Ignored, logic.OnQrRead(P9, i * 0.5f));

        Assert.AreEqual(4f, logic.RemainingLegMeters, 0.001f);
    }

    [TestCase(P9, 10.6f, false)]
    [TestCase(P9, 2f, true)]
    [TestCase("L-1-01", 5f, true)]
    public void PosicaoIncerta_SoRetomaComQrValido(string ponto, float distancia, bool perdeuRastreamento)
    {
        logic.OnQrRead(ponto, 0f);
        logic.Tick(distancia, perdeuRastreamento);
        Assert.AreEqual(NavState.NeedNewQr, logic.State);

        // Voltar para dentro do trecho ou recuperar o ARCore nao confirma a posicao.
        logic.Tick(2f, false);
        Assert.AreEqual(NavState.NeedNewQr, logic.State);
        Assert.AreEqual(QrResult.Unknown, logic.OnQrRead("QR-DESCONHECIDO", 1.9f));
        logic.Tick(2f, false);
        Assert.AreEqual(NavState.NeedNewQr, logic.State);

        Assert.AreEqual(QrResult.RouteLoaded, logic.OnQrRead(ponto, 2f));
        Assert.AreEqual(NavState.Following, logic.State);
        Assert.AreEqual(logic.CurrentLeg.meters, logic.RemainingLegMeters, 0.001f);
    }

    [Test]
    public void QrDaSaida_ConfirmaChegadaMesmoComPosicaoIncerta()
    {
        logic.OnQrRead(P9, 0f);
        logic.Tick(2f, true);

        Assert.AreEqual(QrResult.Arrived, logic.OnQrRead(logic.ExitId, 0.5f));
        logic.Tick(20f, true);
        Assert.AreEqual(NavState.Arrived, logic.State);
        Assert.AreEqual(0f, logic.RemainingTotalMeters);
    }

    [Test]
    public void DistanciaTotalAteASaida()
    {
        logic.OnQrRead(P9, 0f);
        Assert.AreEqual(27.4f, logic.RemainingTotalMeters, 0.01f);
        logic.Tick(4.0f, false);
        Assert.AreEqual(23.4f, logic.RemainingTotalMeters, 0.01f);
    }

    [Test]
    public void Alertas_ElevadorEExtintor()
    {
        logic.OnQrRead("L-0-02", 0f);                    // ponto com extintor
        Assert.IsTrue(logic.ShowElevatorAlert);
        Assert.IsNotEmpty(logic.ExtinguisherText);
        logic.OnQrRead("L-0-03", 10f);                   // coluna, sem extintor
        Assert.IsEmpty(logic.ExtinguisherText);
    }

    [Test]
    public void TodaRotaDoJson_TerminaNaSaida_ETemDestinosValidos()
    {
        var ids = new System.Collections.Generic.HashSet<string>();
        foreach (var p in logic.Data.points) ids.Add(p.id);
        Assert.Greater(logic.Data.routes.Count, 0);
        foreach (var r in logic.Data.routes)
        {
            Assert.IsTrue(ids.Contains(r.startId), $"partida sem ponto: {r.startId}");
            Assert.AreEqual(logic.ExitId, r.legs[r.legs.Count - 1].toId, $"rota {r.startId} nao termina na saida");
            foreach (var l in r.legs) Assert.IsTrue(ids.Contains(l.toId), $"destino inexistente: {l.toId} (rota {r.startId})");
        }
    }
}
