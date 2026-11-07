using System.IO;
using NUnit.Framework;
using SafeSight.Navigation;
using UnityEngine;

/// <summary>
/// Orientacao do QR: o QR fica numa coluna virado para o corredor, entao o usuario le olhando para ele
/// (de lado em relacao ao trecho). Com o dado cadastrado (Assets/Resources/trajeto.json), o app diz
/// quanto girar depois da leitura. Convencao: azimute da planta, 0 = topo, sentido horario; giro + = direita.
/// </summary>
public class NavigationOrientacaoTests
{
    NavigationLogic logic;

    [SetUp]
    public void SetUp()
    {
        string path = Path.Combine(Application.dataPath, "Resources", "trajeto.json");
        logic = NavigationLogic.FromJson(File.ReadAllText(path));
    }

    // Fileira de cima: QR virado para cima, trecho para a esquerda do croqui -> girar 90 graus para a direita.
    // L-0-14 (P6, canto da escada): QR virado para cima como os demais; depois de ler, vira 90 graus a direita
    // (de frente para os banheiros), anda cerca de 1 m para passar a coluna e vira a esquerda para chegar a P7.
    // L-0-15 (P7, junto a escada): QR virado para a esquerda, trecho para E1 na diagonal (225 graus) -> 135 graus a direita.
    // L-0-04 (fileira de baixo): QR virado para baixo, trecho para a esquerda -> girar 90 graus para a esquerda.
    [TestCase("L-0-11", 90f)]
    [TestCase("L-0-12", 90f)]
    [TestCase("L-0-13", 90f)]
    [TestCase("L-0-14", 90f)]
    [TestCase("L-0-15", 135f)]
    [TestCase("L-0-02", -90f)]
    [TestCase("L-0-04", -90f)]
    public void GiroInicial_PorPonto(string ponto, float esperado)
    {
        logic.OnQrRead(ponto, 0f);
        Assert.IsTrue(logic.HasInitialTurn, ponto);
        Assert.AreEqual(esperado, logic.InitialTurnDegrees, 0.01f, ponto);
    }

    [Test]
    public void ColunaDaFileiraDeCima_PedeGiroParaDireita_ESegueDepoisDeGirar()
    {
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(0f, false, 0f);
        Assert.IsTrue(logic.NeedsAlignment);
        Assert.AreEqual(90f, logic.AlignErrorDegrees, 0.01f);
        StringAssert.StartsWith("Vire à direita", logic.Instruction);

        logic.Tick(0f, false, 70f);                 // girou 70: faltam 20 (dentro da tolerancia de 30)
        Assert.IsFalse(logic.NeedsAlignment);
        StringAssert.StartsWith("Siga em frente", logic.Instruction);
    }

    [Test]
    public void FileiraDeBaixo_PedeGiroParaEsquerda()
    {
        logic.OnQrRead("L-0-05", 0f);
        logic.Tick(0f, false, 0f);
        StringAssert.StartsWith("Vire à esquerda", logic.Instruction);
        Assert.AreEqual(-90f, logic.AlignErrorDegrees, 0.01f);

        logic.Tick(0f, false, -80f);
        Assert.IsFalse(logic.NeedsAlignment);
    }

    [Test]
    public void P7_PedeGiroDe135GrausParaDireita_ESiga5mAteE1()
    {
        logic.OnQrRead("L-0-15", 0f);
        logic.Tick(0f, false, 0f);
        Assert.AreEqual(135f, logic.AlignErrorDegrees, 0.01f);
        StringAssert.StartsWith("Vire à direita", logic.Instruction);
        StringAssert.Contains("5 m", logic.Instruction);
        Assert.AreEqual("L-0-02", logic.CurrentLeg.toId);

        logic.Tick(0f, false, 130f);                // girou quase tudo
        Assert.IsFalse(logic.NeedsAlignment);
    }

    [Test]
    public void E1_PedeGiroDe90GrausParaEsquerda_ESiga9mAteAEntrada()
    {
        logic.OnQrRead("L-0-02", 0f);
        logic.Tick(0f, false, 0f);
        Assert.AreEqual(-90f, logic.AlignErrorDegrees, 0.01f);
        StringAssert.StartsWith("Vire à esquerda", logic.Instruction);
        StringAssert.Contains("9 m", logic.Instruction);
        Assert.AreEqual("L-0-01", logic.CurrentLeg.toId);
    }

    [Test]
    public void AlignText_MeiaVoltaSoQuandoMuitoPerto_De180()
    {
        Assert.AreEqual("Dê meia-volta", NavigationLogic.AlignText(170f));
        Assert.AreEqual("Dê meia-volta", NavigationLogic.AlignText(-170f));
        Assert.AreEqual("Vire à direita", NavigationLogic.AlignText(135f));
        Assert.AreEqual("Vire à esquerda", NavigationLogic.AlignText(-90f));
    }

    [Test]
    public void ColunaP6_Direita_1m_Esquerda_2m_Esquerda_ProximoQr()
    {
        // ler QR -> direita 90 -> 1 m -> esquerda 90 -> 2 m -> esquerda 90 -> ler o QR de P7
        logic.OnQrRead("L-0-14", 0f);
        Assert.AreEqual("L-0-15", logic.CurrentLeg.toId);
        Assert.IsTrue(logic.CurrentLeg.HasVia);
        Assert.AreEqual("left", logic.CurrentLeg.turn);

        logic.Tick(0f, false, 0f);                  // acabou de ler: de lado
        Assert.IsTrue(logic.NeedsAlignment);
        StringAssert.StartsWith("Vire à direita", logic.Instruction);
        StringAssert.Contains("1 m", logic.Instruction);

        logic.Tick(0f, false, 85f);                 // ficou de frente para os banheiros
        Assert.IsFalse(logic.NeedsAlignment);
        Assert.AreEqual(NavState.Following, logic.State);
        StringAssert.StartsWith("Siga em frente 1 m", logic.Instruction);

        logic.Tick(0.6f, false, 85f);               // quase no fim do 1o metro
        Assert.AreEqual(NavState.TurnWarning, logic.State);
        StringAssert.Contains("vire à esquerda", logic.Instruction);
        Assert.AreEqual("left", logic.CurrentTurn);

        logic.Tick(1.0f, false, 85f);               // chegou ao fim do 1o metro
        StringAssert.Contains("vire à esquerda", logic.Instruction);

        logic.Tick(1.0f, false, 5f);                // virou a esquerda (85 -> 5)
        Assert.IsFalse(logic.InViaStage);
        Assert.AreEqual(NavState.Following, logic.State);
        StringAssert.StartsWith("Siga em frente 2 m", logic.Instruction);
        Assert.AreEqual(0f, logic.TurnProgressDegrees, 6f);

        logic.Tick(2.0f, false, 5f);                // 2a etapa: sqrt(2^2 - 1^2) = 1,73 m andados
        Assert.AreEqual(NavState.TurnWarning, logic.State);
        Assert.AreEqual(0.27f, logic.RemainingLegMeters, 0.02f);
        StringAssert.Contains("vire à esquerda", logic.Instruction);

        logic.Tick(2.24f, false, -85f);             // 2 m andados e virou a esquerda de novo
        Assert.AreEqual(NavState.TurnWarning, logic.State);
        StringAssert.Contains("QR", logic.Instruction);
        Assert.AreEqual(-85f, logic.TurnProgressDegrees, 6f);
    }

    [Test]
    public void ColunaP6_SemVirarNoMeio_NaoAvancaParaA2aEtapa()
    {
        logic.OnQrRead("L-0-14", 0f);
        logic.Tick(0.2f, false, 85f);
        logic.Tick(1.2f, false, 85f);               // andou mais de 1 m reto, sem virar a esquerda
        Assert.IsTrue(logic.InViaStage);
        StringAssert.Contains("vire à esquerda", logic.Instruction);
    }

    [Test]
    public void SomenteP6TemCurvaNoMeio()
    {
        foreach (var r in logic.Data.routes)
            foreach (var l in r.legs)
            {
                if (l.viaMeters > 0f)
                {
                    Assert.AreEqual("L-0-15", l.toId, $"rota {r.startId}");
                    Assert.IsTrue(l.HasVia, $"rota {r.startId}: dado de curva no meio incompleto");
                }
            }
    }

    [Test]
    public void FileiraDeCima_ChegandoEmP6_NaoPedeCurva_ESegueViaP7EE1()
    {
        // L-0-13 -> L-0-14 (6,8 m, sem curva: em P6 so se vira para ler o QR) -> L-0-15 -> L-0-02 -> L-0-01
        logic.OnQrRead("L-0-13", 0f);
        Assert.AreEqual("L-0-14", logic.CurrentLeg.toId);
        Assert.AreEqual("none", logic.CurrentLeg.turn);
        var proximos = new System.Collections.Generic.List<TrajLeg>(logic.NextLegs);
        Assert.AreEqual(3, proximos.Count);
        Assert.AreEqual("L-0-15", proximos[0].toId);
        Assert.AreEqual("left", proximos[0].turn);
        Assert.AreEqual("L-0-02", proximos[1].toId);
        Assert.AreEqual("L-0-01", proximos[2].toId);
    }

    [Test]
    public void PontoJuntoAEscada_ContinuaPeloCaminhoDaPlanta()
    {
        // o atalho vale so a partir de P6; de P7 (L-0-15) segue por E1 (L-0-02)
        logic.OnQrRead("L-0-15", 0f);
        Assert.AreEqual("L-0-02", logic.CurrentLeg.toId);
    }

    [Test]
    public void SeguirGirandoEmCimaDoErro_AtualizaOQueFalta()
    {
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(0f, false, 30f);
        Assert.AreEqual(60f, logic.AlignErrorDegrees, 0.01f);
        Assert.IsTrue(logic.NeedsAlignment);
        logic.Tick(0f, false, 120f);                // passou do alvo: ja esta dentro da tolerancia
        Assert.AreEqual(-30f, logic.AlignErrorDegrees, 0.01f);
    }

    [Test]
    public void DepoisDeAlinhar_OlharDeLado_NaoVoltaAPedirGiro()
    {
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(0.1f, false, 90f);               // alinhou
        Assert.IsFalse(logic.NeedsAlignment);
        logic.Tick(0.5f, false, 20f);               // olhou para o lado
        Assert.IsFalse(logic.NeedsAlignment);
    }

    [Test]
    public void AndandoSemAlinhar_NaoPedeMaisOGiroInicial()
    {
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(2.0f, false, 0f);                // andou 2 m (> 1,5 m) sem girar
        Assert.IsFalse(logic.NeedsAlignment);
    }

    [Test]
    public void NovaLeituraDoMesmoQr_PedeOGiroDeNovo()
    {
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(0.1f, false, 90f);               // alinhou
        logic.Tick(20f, true, 90f);                 // perdeu o rastreamento
        Assert.AreEqual(NavState.NeedNewQr, logic.State);

        logic.OnQrRead("L-0-12", 10f);              // le de novo (o estimador zera o giro)
        logic.Tick(0f, false, 0f);
        Assert.IsTrue(logic.NeedsAlignment);
    }

    [Test]
    public void CurvaConfirmada_DescontaOGiroInicial()
    {
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(0f, false, 90f);                 // girou 90 para ficar alinhado
        Assert.AreEqual(0f, logic.TurnProgressDegrees, 0.01f);
        logic.Tick(5f, false, 160f);                // virou mais 70 depois de andar
        Assert.AreEqual(70f, logic.TurnProgressDegrees, 0.01f);
    }

    [Test]
    public void PontoSemDado_ComportamentoAntigo()
    {
        string semDado = null;
        foreach (var r in logic.Data.routes)
        {
            var pt = logic.Data.points.Find(p => p.id == r.startId);
            if (!pt.hasQrFacing) { semDado = r.startId; break; }
        }
        Assert.IsNotNull(semDado, "deveria existir ponto sem dado de orientacao");

        logic.OnQrRead(semDado, 0f);
        logic.Tick(0f, false, 90f);
        Assert.IsFalse(logic.HasInitialTurn);
        Assert.IsFalse(logic.NeedsAlignment);
        StringAssert.StartsWith("Siga em frente", logic.Instruction);
        Assert.AreEqual(90f, logic.TurnProgressDegrees, 0.01f);
    }

    [Test]
    public void RecursoDesligado_ComportamentoAntigo()
    {
        logic.UseQrOrientation = false;
        logic.OnQrRead("L-0-12", 0f);
        logic.Tick(0f, false, 0f);
        Assert.IsFalse(logic.HasInitialTurn);
        Assert.IsFalse(logic.NeedsAlignment);
        StringAssert.StartsWith("Siga em frente", logic.Instruction);
    }

    [Test]
    public void DadosDeOrientacao_SaoConsistentes()
    {
        foreach (var r in logic.Data.routes)
        {
            var pt = logic.Data.points.Find(p => p.id == r.startId);
            Assert.AreEqual(pt.hasQrFacing, r.hasStartHeading, $"rota {r.startId}: dado de orientacao incompleto");
            if (!r.hasStartHeading) continue;
            Assert.That(r.startHeadingDeg, Is.InRange(0f, 359.99f), r.startId);
            Assert.That(pt.qrFacingDeg, Is.InRange(0f, 359.99f), r.startId);
            // corredores ortogonais, exceto os trechos na diagonal medidos em campo (L-0-15 -> E1: 225 graus)
            if (r.startId == "L-0-15") Assert.AreEqual(225f, r.startHeadingDeg, 0.01f, r.startId);
            else Assert.AreEqual(0f, r.startHeadingDeg % 90f, 0.01f, $"rota {r.startId}: sentido fora dos multiplos de 90");
        }
    }
}
