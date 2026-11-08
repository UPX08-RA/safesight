# SafeSight

> **Projeto UPX — Realidade Aumentada (Engenharia da Computação, UniFacens)**
> Orientação por Realidade Aumentada até a saída-alvo em uma simulação controlada de evacuação no Prédio L, a partir de QR codes.

![Status](https://img.shields.io/badge/status-em%20desenvolvimento-yellow)
![Plataforma](https://img.shields.io/badge/plataforma-Android-green)
![Tecnologia](https://img.shields.io/badge/RA-AR%20Foundation%20%2B%20ARCore-blue)
![Unity](https://img.shields.io/badge/Unity-6000.3.25f1-black)
![Versão](https://img.shields.io/badge/vers%C3%A3o-v1.1-blue)

---

## 1. Identificação do Projeto

| Item            | Informação                |
| --------------- | ------------------------- |
| **Projeto**     | SafeSight                 |
| **Equipe**      | RotAR                     |
| **Curso**       | Engenharia da Computação  |
| **Disciplina**  | UPX — Realidade Aumentada |
| **Professor**   | Daniel Ohata              |
| **Semestre**    | 8º semestre (2026.2)      |
| **Instituição** | UniFacens                 |

### Integrantes

| Nome                                       | RA     | Turma | Responsabilidade                                                                                                                    |
| ------------------------------------------ | ------ | ----- | ----------------------------------------------------------------------------------------------------------------------------------- |
| Gustavo Oliveira Gomes                     | 235413 | TIN1  | Tech Lead: arquitetura, leitura de QR (ZXing.Net), estimativa de deslocamento com ARCore, dados do trajeto (JSON e conversor) e HUD |
| João Victor Cardoso Engler Rizzi de Araujo | 236602 | TIN2  | Lógica de navegação em C#: revisão e correções, testes de regressão (EditMode) e pull request                                       |
| Giulia Oliveira                            | 210071 | TIN1  | Mapeamento do Prédio L (planilha de pontos e conexões, plantas), pesquisa e QR codes (impressão e validação em campo)               |
| Luiz Augusto Marques Zocca                 | 235623 | TIN1  | Scrum Master: organização e Trello, logística e participação nos testes de campo                                                    |
| Henrique Seiji Kuroiwa                     | 236060 | TIN1  | Pesquisa, artigo científico e documentação                                                                                          |

---

## 2. Visão Geral

O **SafeSight** é um aplicativo Android de Realidade Aumentada que orienta pessoas que não conhecem o prédio até a **saída-alvo** de uma rota segura previamente cadastrada, em uma **simulação controlada de evacuação** (sem fogo ou fumaça reais). O SafeSight não avalia a segurança de saídas em incêndios reais.

O usuário lê com a câmera o **QR code** de um ponto do corredor. O app identifica o ponto, carrega a rota cadastrada daquele ponto até a saída e, enquanto a pessoa caminha, mostra sobre a imagem da câmera uma **seta, a instrução do trecho, a distância restante, os próximos passos e alertas** (elevador proibido e extintor cadastrado). Entre dois QR codes, a distância percorrida é estimada pelo **rastreamento de movimento do ARCore**. Se o rastreamento se perde ou o trecho é excedido, o app pede uma nova leitura de QR.

A Realidade Aumentada entra como a camada que une a câmera ao vivo, o rastreamento de movimento do ARCore e uma interface de orientação (seta, instruções e alertas) sobreposta à imagem da câmera. Essa interface é fixa na tela e associada ao estado da rota; as indicações não são objetos virtuais ancorados ao piso ou ao ambiente. Tudo funciona **offline**, sem servidor.

A **versão v1.1** (tag `v1.1`, usada no experimento com usuários de 05 e 06/11/2026) acrescenta ao fluxo acima dois recursos: o app diz quanto o usuário deve girar depois de ler o QR, a partir da direção cadastrada para onde o QR está virado, e trata a curva no meio de um trecho (coluna P6 → P7).

![Visão geral do projeto](docs/images/hud_rota.jpeg)

---

## 3. Problema

Placas de saída fixas pressupõem que a pessoa já conhece o prédio. Isso não funciona bem para calouros, visitantes e professores substitutos, principalmente sob estresse. A orientação para a saída segura depende da memória do usuário e de sinalização estática, que não indica por onde ir a partir do ponto em que a pessoa está.

O impacto é a perda de tempo e a possibilidade de escolher um caminho errado durante uma evacuação. O SafeSight propõe uma orientação passo a passo a partir da posição atual do usuário.

---

## 4. Objetivos

### 4.1 Objetivo Geral

Desenvolver uma aplicação de Realidade Aumentada que oriente o usuário, por rota cadastrada, até a saída-alvo do Prédio L em uma simulação controlada de evacuação.

### 4.2 Objetivos Técnicos

- Ler QR codes pela câmera do ARCore (AR Foundation + ZXing.Net) e identificar o ponto atual.
- Carregar a rota cadastrada de cada ponto a partir de um arquivo local (`trajeto.json`).
- Estimar a distância percorrida entre QR codes pelo rastreamento de movimento do ARCore.
- Avisar a aproximação de curvas e pedir novo QR quando o rastreamento é perdido ou o trecho é excedido.
- Exibir um HUD com seta, instrução, distância restante, próximos passos e alertas.
- Executar a aplicação em dispositivo Android e verificar o funcionamento por testes.

---

## 5. Público-Alvo

Pessoas que não conhecem o Prédio L da UniFacens: calouros, visitantes e professores substitutos. O uso ocorre em simulação de evacuação, em movimento e com pouca atenção disponível para a tela; por isso o HUD usa texto grande, poucas informações e nenhum menu.

- **Familiaridade tecnológica:** pressupõe-se apenas o uso básico de um smartphone ou tablet (abrir o app e autorizar a câmera); não há cadastro nem configuração.
- **Necessidades:** saber para onde ir a partir do ponto onde está, a cada trecho, e ser avisado da curva e do elevador proibido.
- **Limitações do uso:** o app depende de o usuário encontrar e ler os QR codes e de o dispositivo ter suporte ao ARCore; o experimento com usuários (10 participantes, 05 e 06/11/2026) é preliminar e não permite generalizar os resultados.

---

## 6. Funcionalidades

| ID   | Funcionalidade                                  | Descrição                                                                                                                         | Status |
| ---- | ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- | ------ |
| RF01 | Exibir câmera com seta e instruções             | Imagem da câmera ao vivo com seta e instruções sobrepostas                                                                        | ✅     |
| RF02 | Ler QR code                                     | Lê o ID do ponto pela câmera do ARCore                                                                                            | ✅     |
| RF03 | Definir posição e carregar rota                 | Valida o ID e carrega a rota cadastrada do ponto; nova leitura a qualquer momento                                                 | ✅     |
| RF04 | Orientar até a saída-alvo                       | Segue a rota cadastrada, trecho a trecho, sem elevador                                                                            | ✅     |
| RF05 | Identificar a saída-alvo                        | Mostra o destino na tela                                                                                                          | ✅     |
| RF06 | Instrução, próximos passos, distância e chegada | Instrução atual, próximos passos, distância restante; chegada ao ler o QR da saída (ou ao completar a distância do último trecho) | ✅     |
| RF07 | Alertas                                         | "Não utilize o elevador" e extintor cadastrado do ponto atual                                                                     | ✅     |
| RF08 | Estimar distância entre QR codes                | Distância percorrida pelo rastreamento de movimento do ARCore                                                                     | ✅     |
| RF09 | Avisar curva                                    | Avisa a aproximação da curva (cerca de 1 m do fim do trecho)                                                                      | ✅     |
| RF10 | Pedir novo QR                                   | Pede nova leitura se o rastreamento é perdido ou o trecho é excedido                                                              | ✅     |

**Legenda:** ✅ Implementado · 🚧 Em desenvolvimento · ⬜ Planejado · ❌ Cancelado

> A v1.1 não altera a lista acima. Ela acrescenta a orientação inicial pelo QR (giro pedido depois da leitura) e a curva no meio do trecho, descritas na seção 11.2.

---

## 7. Requisitos Não Funcionais

| ID    | Categoria       | Requisito                                         |
| ----- | --------------- | ------------------------------------------------- |
| RNF01 | Conectividade   | Funcionar offline, sem servidor                   |
| RNF02 | Compatibilidade | Executar em Android com suporte ao ARCore         |
| RNF03 | Usabilidade     | Interface legível, com texto grande e sem menus   |
| RNF04 | Desempenho      | Ler QR sem comprometer a fluidez (30 fps ou mais) |
| RNF05 | Infraestrutura  | Não depender de nada além dos QR codes impressos  |

---

## 8. Tecnologias Utilizadas

| Tecnologia                                           | Versão                                                                                                                            | Utilização                                                                                                        |
| ---------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| Unity                                                | 6000.3.25f1 (6.3 LTS)                                                                                                             | Desenvolvimento principal (template 3D, URP)                                                                      |
| C#                                                   | compatível com Unity 6                                                                                                            | Scripts da aplicação                                                                                              |
| AR Foundation                                        | 6.3.5                                                                                                                             | Camada de abstração de RA                                                                                         |
| Google ARCore XR Plugin                              | 6.3.5                                                                                                                             | Rastreamento de movimento e câmera em Android                                                                     |
| ZXing.Net                                            | 0.16.11                                                                                                                           | Decodificação de QR codes                                                                                         |
| NuGetForUnity                                        | pacote Git (GlitchEnzo/NuGetForUnity, `src/NuGetForUnity`)                                                                        | Instalação do ZXing.Net                                                                                           |
| Unity Test Framework (NUnit)                         | 1.6.0                                                                                                                             | Testes automatizados (EditMode)                                                                                   |
| Universal Render Pipeline (URP)                      | 17.3.0                                                                                                                            | Pipeline gráfico                                                                                                  |
| Input System                                         | 1.20.0                                                                                                                            | Entrada (novo Input System)                                                                                       |
| XR Plugin Management                                 | 4.6.1                                                                                                                             | Carregamento do ARCore no Android                                                                                 |
| Python 3.13 e openpyxl 3.1.5                         | versões do ambiente em que os scripts foram executados (25/10); na máquina do Tech Lead há Python 3.12.7 (sem openpyxl instalado) | Conversor planilha→JSON (`Assets/tools/gerar_trajeto.py`)                                                         |
| OpenCV (Python) 5.0.0, NumPy 2.5.3 e ReportLab 5.0.1 | mesmo ambiente de execução (Python 3.13, 25/10)                                                                                   | Geração e validação dos QR codes e do PDF de impressão (`Assets/tools/gerar_qrcodes.py`, escrito com apoio de IA) |
| Git e GitHub                                         | —                                                                                                                                 | Controle de versão e hospedagem                                                                                   |

---

## 9. Hardware Utilizado

| Equipamento | Modelo                                                                                                            | Característica relevante | Utilização                      |
| ----------- | ----------------------------------------------------------------------------------------------------------------- | ------------------------ | ------------------------------- |
| Tablet      | Samsung Galaxy Tab S9 FE                                                                                          | Android 16 / One UI 8    | Build e verificação do HUD      |
| Celular     | Samsung Galaxy M54 5G                                                                                             | Android 16 / One UI 8    | Testes de deslocamento e avisos |
| Celular     | Samsung Galaxy A54 5G                                                                                             | Android 16 / One UI 8    | Testes de deslocamento e avisos |
| Computador  | Notebook com Intel Core i7-9750H 2,60 GHz, 32 GB DDR4, Intel UHD Graphics 630 e NVIDIA GeForce GTX 1660 Ti (6 GB) | CPU, RAM e GPU           | Desenvolvimento (Gustavo)       |
| Computador  | Notebook com Intel Core i7-1165G7 2,80 GHz, 8 GB de RAM e Intel Iris Xe Graphics                                  | CPU, RAM e GPU           | Desenvolvimento (João)          |

> Foram usados apenas os três aparelhos acima. Nenhuma compatibilidade foi declarada para outros equipamentos.

---

## 10. Arquitetura do Sistema

O app é 100% Unity e roda localmente. O `QrScanner` lê o QR; o `NavigationController` liga leitura, estimativa de deslocamento e lógica; a `NavigationLogic` (sem dependência de ARCore, testável isoladamente) decide o estado a partir das rotas do `trajeto.json`; o `NavigationHud` desenha a interface.

![Arquitetura do sistema](docs/diagrams/arquitetura.png)

### Fluxo resumido

```text
Usuário → Câmera (ARCore) → QrScanner → NavigationController → NavigationLogic (trajeto.json)
                                    ↘ DisplacementEstimator (pose do ARCore) ↗
NavigationLogic → NavigationHud → Seta, instruções e alertas sobre a câmera
```

---

## 11. Funcionamento da Realidade Aumentada

### 11.1 Técnica utilizada

**QR Code como referência de posição, mais rastreamento de movimento do ARCore (odometria visual-inercial).** O QR só informa a posição no instante da leitura (o ZXing.Net decodifica o texto, sem pose). Entre dois QR codes, a distância é estimada pela pose do dispositivo fornecida pelo ARCore. A seta e as instruções são um **HUD fixo na tela**, não ancorado ao piso.

### 11.2 Fluxo técnico

![Diagrama de sequência](docs/diagrams/sequencia.png)

1. A câmera do ARCore é iniciada pelo AR Foundation.
2. O `QrScanner` obtém o frame (`XRCpuImage`), copia o plano de luminância e o decodifica com o ZXing.Net, a cada 5 frames.
3. O ID lido é validado contra os pontos do `trajeto.json`; a rota cadastrada do ponto é carregada.
4. O `DisplacementEstimator` guarda a pose inicial e acompanha a pose da câmera no plano horizontal: distância em linha reta, caminho acumulado e giro.
5. A `NavigationLogic` compara a distância com o comprimento do trecho: segue, avisa a curva (a cerca de 1 m do fim), pede novo QR (rastreamento perdido ou 1,5 m além do trecho) ou informa a chegada (ao ler o QR da saída ou ao completar a distância do último trecho).
6. O `NavigationHud` mostra seta, instrução, distância restante, próximos passos e alertas sobre a imagem da câmera.
7. Ao ler o QR do próximo ponto, a estimativa reinicia e a rota seguinte é carregada.

**Acréscimos da v1.1:**

- **Orientação do QR:** nos pontos mapeados, o `trajeto.json` guarda a direção para onde a face do QR aponta (`qrFacingDeg`) e o sentido do primeiro trecho (`startHeadingDeg`). Depois da leitura, a `NavigationLogic` calcula quanto o usuário precisa girar e, se o erro passar de 30°, o HUD mostra "Vire à direita" ou "Vire à esquerda" ("Dê meia-volta" acima de 150°), com a seta apontando o giro. O pedido some quando o usuário fica alinhado ou anda mais de 1,5 m.
- **Curva no meio do trecho:** o trecho L-0-14 → L-0-15 (P6 → P7) tem 1 m, uma curva à esquerda e mais 2 m (`viaMeters`, `viaTurn` e `afterViaMeters`). A curva é confirmada pelo giro (60°) e a distância restante é calculada por etapa.
- Os pontos sem dado de orientação (L-0-07, L-0-09 e os andares 1 e 2) mantêm o comportamento da v1.0.

### 11.3 Estados da navegação

![Estados da navegação](docs/diagrams/estados.png)

### 11.4 Elementos virtuais

> Os elementos abaixo são uma interface sobreposta ao vídeo da câmera (HUD), controlada pelo estado da navegação. Não há ancoragem tridimensional ao piso nem aos elementos do ambiente; o ARCore é usado para estimar o deslocamento.

- seta de direção (gira à direita ou à esquerda na curva);
- instrução do trecho atual e lista dos próximos passos;
- barra de progresso do trecho e distância até a saída;
- alertas de elevador e de extintor;
- estados: aguardando QR, rota ativa, novo QR necessário, QR inválido e chegada.

---

## 12. Estrutura do Projeto

```text
safesight/
├── Assets/
│   ├── Resources/
│   │   └── trajeto.json          # pontos e rotas do Prédio L
│   ├── Scenes/
│   │   └── ARTest.unity          # cena principal
│   ├── Packages/
│   │   └── ZXing.Net.0.16.11/    # biblioteca de QR (NuGet, versionada)
│   ├── Scripts/
│   │   ├── Navigation/           # lógica de navegação (sem MonoBehaviour)
│   │   ├── QrScanner.cs
│   │   ├── DisplacementEstimator.cs
│   │   ├── NavigationController.cs
│   │   └── NavigationHud.cs
│   ├── Tests/EditMode/           # testes automatizados
│   └── tools/
│       ├── gerar_trajeto.py      # conversor planilha → JSON
│       └── gerar_qrcodes.py      # gerador e validador dos QR codes
├── Packages/
├── ProjectSettings/
├── docs/
│   ├── Mapeamento_Predio_L_todos_andares.xlsx
│   ├── diagrams/                 # diagramas (.png e fonte .mmd)
│   ├── documents/                # documentos complementares
│   └── images/                   # prints do aplicativo
├── .gitignore
└── README.md
```

> Árvore conferida com o repositório em 31/10 (v1.0); a v1.1 só acrescentou o arquivo de testes `NavigationOrientacaoTests.cs` em `Assets/Tests/EditMode/`. Pastas geradas pelo Unity (`Library`, `Temp`, `Logs`, `UserSettings`) e os builds são ignorados pelo `.gitignore`.

### Pastas principais

| Pasta                           | Finalidade                                                                                         |
| ------------------------------- | -------------------------------------------------------------------------------------------------- |
| `Assets/Scripts`                | Scripts produzidos pela equipe                                                                     |
| `Assets/Scenes`                 | Cena principal da aplicação (`ARTest.unity`)                                                       |
| `Assets/Scripts/Navigation`     | Lógica de navegação e dados do trajeto (assembly `SafeSight.Navigation`)                           |
| `Assets/Resources/trajeto.json` | Pontos e rotas cadastradas do Prédio L                                                             |
| `Assets/Tests/EditMode`         | Testes automatizados da navegação                                                                  |
| `Assets/tools`                  | Conversor da planilha de mapeamento para JSON e gerador/validador dos QR codes                     |
| `Assets/Packages`               | ZXing.Net 0.16.11 (NuGet), versionado                                                              |
| `docs/diagrams`                 | Diagramas técnicos                                                                                 |
| `docs/images`                   | Screenshots do aplicativo                                                                          |
| `docs/documents`                | Documentos complementares (`validacao_qrcodes.txt`: resultado da geração e validação dos QR codes) |

---

## 13. Principais Scripts

| Script                                                                                    | Responsabilidade                                                                               |
| ----------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `QrScanner.cs`                                                                            | Obtém o frame da câmera do ARCore, decodifica o QR com ZXing.Net e dispara o evento de leitura |
| `DisplacementEstimator.cs`                                                                | Estima distância em linha reta, caminho e giro a partir da pose do ARCore; reinicia a cada QR  |
| `NavigationLogic.cs`                                                                      | Regras de navegação: rota do ponto, estados, alertas, distância restante e instruções          |
| `TrajectoryData.cs`                                                                       | Estrutura dos dados do `trajeto.json` (pontos, rotas e trechos)                                |
| `NavigationController.cs`                                                                 | Liga leitura de QR, estimativa de deslocamento e lógica; avisos de QR inválido                 |
| `NavigationHud.cs`                                                                        | Desenha o HUD (IMGUI): seta, instrução, progresso, próximos passos e alertas                   |
| `NavigationLogicTests.cs`, `NavigationRotasReaisTests.cs`, `NavigationOrientacaoTests.cs` | Testes automatizados: lógica, rotas reais e (v1.1) orientação do QR e curva no meio do trecho  |

---

## 14. Dependências

```text
AR Foundation 6.3.5
Google ARCore XR Plugin 6.3.5
XR Plugin Management 4.6.1
ZXing.Net 0.16.11 (via NuGetForUnity)
NuGetForUnity (pacote Git)
Unity Test Framework 1.6.0
Universal Render Pipeline 17.3.0
Input System 1.20.0
```

**Onde obter:** AR Foundation, ARCore XR Plugin, XR Plugin Management, Input System, URP e Test Framework vêm do Unity Package Manager. O ZXing.Net 0.16.11 é obtido no [NuGet](https://www.nuget.org/packages/ZXing.Net/0.16.11) (código em [micjahn/ZXing.Net](https://github.com/micjahn/ZXing.Net)) e o NuGetForUnity em [GlitchEnzo/NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).

Versões conferidas em `Packages/manifest.json` e `Packages/packages-lock.json`. O ZXing.Net 0.16.11 está **versionado no repositório** (`Assets/Packages/ZXing.Net.0.16.11/`, com `Assets/NuGet.config` e `Assets/packages.config`), então não é preciso restaurá-lo manualmente.

---

## 15. Configuração do Ambiente

### Pré-requisitos

- Windows 10 ou 11;
- Unity Hub e Unity **6000.3.25f1** com o módulo **Android Build Support** (SDK, NDK e OpenJDK pelo Hub);
- Git;
- tablet ou smartphone Android com suporte ao ARCore e depuração USB ativada.

### Procedimento

1. Instale o Unity Hub.
2. Instale o Unity 6000.3.25f1 com Android Build Support.
3. Instale o Git.
4. Clone o repositório.
5. Abra o projeto pelo Unity Hub (aguarde a importação).
6. O ZXing.Net já acompanha o repositório (`Assets/Packages`); se o Unity avisar de pacote ausente, abra **NuGet → Restore Packages**.
7. Em Build Profiles, selecione Android.

### Validação do procedimento

Em 31/10, o procedimento foi executado em um **clone novo** do repositório (`git clone` em uma pasta vazia, sem a pasta `Library`), na máquina do Tech Lead (Windows), com o Unity 6000.3.25f1. A primeira abertura e importação do projeto levou cerca de 10 minutos, o Console não mostrou erros e o Test Runner (EditMode) executou os 27 testes da v1.0 com sucesso (0,209 s). O build para Android não foi repetido nesse clone; ele foi verificado no projeto de desenvolvimento. Esse procedimento foi validado na v1.0; a v1.1 não foi repetida em clone novo (os 52 testes foram executados no projeto de desenvolvimento em 07/11).

![Test Runner e Console no clone limpo](docs/images/validacao_clone_limpo.png)

---

## 16. Como Executar

```bash
git clone https://github.com/UPX08-RA/safesight.git
cd safesight
```

1. Abra o Unity Hub e clique em **Add / Open Project**.
2. Selecione a pasta clonada e confirme a versão 6000.3.25f1.
3. Abra a cena `Assets/Scenes/ARTest.unity`.
4. Em **Build Profiles**, selecione **Android**.
5. Conecte o dispositivo por USB.
6. Execute **Build and Run**.

---

## 17. Como Utilizar a Aplicação

1. Abra o SafeSight no dispositivo e autorize o acesso à câmera.
2. Aponte a câmera para o QR code de um ponto do corredor (a cerca de 30 cm).
3. Leia a instrução e siga a seta. Se o app pedir "Vire à direita" ou "Vire à esquerda", gire até a seta apontar para a frente. A barra mostra o avanço do trecho e o texto, a distância até a saída.
4. Perto da curva, a seta gira para o lado indicado.
5. No fim do trecho, leia o QR code do próximo ponto.
6. Se aparecer o "?", leia o próximo QR code para retomar.
7. Ao completar o último trecho (ou ao ler o QR da saída), aparece a confirmação de chegada.

Os botões **Reiniciar** e **Debug** ficam no topo da tela.

![Rota ativa](docs/images/hud_rota.jpeg)

---

## 18. Fluxo de Funcionamento

```mermaid
flowchart TD
    A[Abrir app] --> B[Câmera ativa]
    B --> C{QR válido?}
    C -- Não --> B
    C -- Sim --> D[Carregar rota do ponto]
    D --> E[Seguir trecho: estimar distância]
    E --> F{Fim do trecho próximo?}
    F -- Sim --> G[Avisar curva]
    F -- Não --> E
    E --> H{Rastreamento perdido ou trecho excedido?}
    H -- Sim --> I[Pedir novo QR]
    G --> J[Ler QR do próximo ponto]
    I --> J
    J --> K{QR da saída?}
    K -- Sim --> L[Chegada]
    K -- Não --> D
```

---

## 19. Caso de Uso

O usuário (pessoa que evacua) lê o QR, acompanha as instruções e lê o próximo QR até a saída. Não há administrador nem servidor no escopo.

```mermaid
flowchart LR
    U([Usuário])
    subgraph S[SafeSight - funciona offline]
        C1(Ler QR code do ponto atual)
        C2(Seguir seta e instruções do trecho)
        C3(Receber alertas de elevador e extintor)
        C4(Receber aviso de curva)
        C5(Ler novo QR quando o app pedir)
        C6(Reiniciar a leitura)
        C7(Receber a confirmação de chegada)
    end
    U --> C1
    U --> C2
    U --> C3
    U --> C4
    U --> C5
    U --> C6
    U --> C7
```

---

## 20. Fluxo de Navegação

```text
Câmera (aguardando QR)
    ↓ lê QR
Rota ativa (seta, instrução, progresso, próximos passos)
    ↓ fim do trecho → lê QR do próximo ponto
Rota ativa (próximo trecho)
    ↓ ... até ler o QR da saída
Chegada
```

Não há tela inicial separada: o app abre direto na câmera, pois o uso ocorre em situação de pressa.

---

## 21. Interface

Telas da v1.0 (HUD sobre a câmera), capturadas no aplicativo em 29/10, com os QR codes exibidos em um monitor. A v1.1 mantém essas telas e acrescenta a instrução de giro inicial ("Vire à direita e siga em frente N m", com a seta apontando o giro), ainda sem captura no repositório.

| Tela                                 | Finalidade                     | Principais elementos                                                                     | Ação do usuário                             |
| ------------------------------------ | ------------------------------ | ---------------------------------------------------------------------------------------- | ------------------------------------------- |
| Aguardando QR (`hud_idle.jpeg`)      | Pedir a leitura do primeiro QR | Moldura de leitura e instrução "Aponte a câmera para um QR Code para começar"            | Aponta a câmera para um QR code do corredor |
| Rota ativa (`hud_rota.jpeg`)         | Orientar o trecho atual        | "Você está em" / destino, seta, instrução, barra de progresso, próximos passos e alertas | Caminha seguindo a seta e as instruções     |
| Aviso de curva (`hud_curva.jpeg`)    | Avisar a curva próxima         | Seta girada e em âmbar, com a distância até a curva                                      | Prepara-se para virar para o lado indicado  |
| Novo QR (`hud_novo_qr.jpeg`)         | Pedir nova leitura             | "?" em vermelho e mensagem de posição incerta                                            | Lê o QR code do próximo ponto               |
| QR inválido (`hud_qr_invalido.jpeg`) | Informar QR não reconhecido    | Aviso temporário "QR não reconhecido"                                                    | Lê um QR code do projeto                    |
| Chegada (`hud_chegada.jpeg`)         | Informar a chegada             | Check em verde e confirmação de chegada                                                  | Encerra a rota                              |

<p>
<img src="docs/images/hud_idle.jpeg" alt="Aguardando QR" width="220">
<img src="docs/images/hud_rota.jpeg" alt="Rota ativa" width="220">
<img src="docs/images/hud_curva.jpeg" alt="Aviso de curva" width="220">
</p>
<p>
<img src="docs/images/hud_novo_qr.jpeg" alt="Novo QR necessário" width="220">
<img src="docs/images/hud_qr_invalido.jpeg" alt="QR inválido" width="220">
<img src="docs/images/hud_chegada.jpeg" alt="Chegada" width="220">
</p>

---

## 22. Testes Técnicos

| Teste                             | Procedimento                                                                                                                                                                                                                                      | Resultado esperado                                       | Resultado obtido                                                                                                                                                                                                                                                                                                                                                                                    | Status                                   |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------- |
| Testes automatizados da navegação | Test Runner (EditMode)                                                                                                                                                                                                                            | Todos passam                                             | **v1.1 (07/11):** 52 de 52 em 0,304 s (23 de lógica + 4 que percorrem as 38 rotas reais + 25 de orientação do QR e curva no meio do trecho; print `testes_unitarios_v1.1.png`). **v1.0 (29/10):** 27 de 27 em 0,237 s                                                                                                                                                                               | ✅                                       |
| Abertura e câmera                 | Build no tablet                                                                                                                                                                                                                                   | Câmera abre                                              | Câmera liga no tablet (25/10)                                                                                                                                                                                                                                                                                                                                                                       | ✅                                       |
| Leitura de QR                     | Ler `L-0-04` no tablet                                                                                                                                                                                                                            | ID mostrado no HUD                                       | HUD mostra "L-0-04", destino, alertas e passos                                                                                                                                                                                                                                                                                                                                                      | ✅                                       |
| Leitura dos QR codes impressos    | Ler com o app todos os QR codes impressos do Prédio L (Giulia, 27/10, durante as primeiras gravações de campo)                                                                                                                                    | Todos os QR codes lidos                                  | Todos foram lidos pelo app sem falha e nenhum precisou ser reimpresso; o tamanho impresso ficou um pouco menor que o planejado (10 cm) e, mesmo assim, a leitura funcionou                                                                                                                                                                                                                          | ✅                                       |
| Deslocamento (5 m)                | 3 medições com trena                                                                                                                                                                                                                              | Erro dentro da tolerância                                | 5,1 / 4,9 / 4,9 m; erro absoluto médio 0,10 m (2,0%); maior erro 0,1 m                                                                                                                                                                                                                                                                                                                              | ✅                                       |
| Deslocamento (9 m)                | 3 medições com trena                                                                                                                                                                                                                              | Erro dentro da tolerância                                | 9,3 / 9,1 / 8,8 m; erro absoluto médio 0,20 m (2,2%); maior erro 0,3 m (3,3%)                                                                                                                                                                                                                                                                                                                       | ✅                                       |
| Aviso de curva                    | Andar até cerca de 1 m do fim do trecho                                                                                                                                                                                                           | Aviso e seta girada                                      | Partida em L-2-12 (trecho de 9,38 m, curva à esquerda): aviso apareceu ao andar ~8,7 m (28/10)                                                                                                                                                                                                                                                                                                      | ✅                                       |
| Curva à direita (L-0-14)          | Ler `L-0-14` e andar cerca de 0,7 m                                                                                                                                                                                                               | Aviso e seta à direita                                   | "Em 0.9 m, vire à direita", seta à direita em âmbar (print `hud_curva.jpeg`, QR em monitor, 29/10)                                                                                                                                                                                                                                                                                                  | ✅                                       |
| Direção e instruções              | Ler L-2-13 e depois L-2-02                                                                                                                                                                                                                        | Seta e passos acompanham o trecho                        | Seta muda de direção, lista de passos atualiza e, no final, aparece a instrução de escada; curva à direita não gravada em campo (27/10), apenas verificada com QR em monitor (linha acima)                                                                                                                                                                                                          | ✅                                       |
| Rastreamento perdido              | Cobrir a câmera ao caminhar                                                                                                                                                                                                                       | Pede novo QR                                             | App perdeu o rastreamento e mostrou "Posição incerta. Leia o próximo QR Code" (28/10)                                                                                                                                                                                                                                                                                                               | ✅                                       |
| Fluxo completo                    | Partir de E2 (`L-0-04`) e ler, em ordem, `L-0-03`, `L-0-02` e `L-0-01` (saída), caminhando os trechos de 9,0 m, 9,4 m e 9,0 m                                                                                                                     | Chegada informada                                        | Os 4 QR codes foram lidos em sequência e, ao ler `L-0-01`, apareceu "Você chegou à saída"; aviso de curva a 8,7 m de `L-0-03` (trecho de 9,4 m, curva à esquerda); sem falha da contagem. Galaxy Tab S9 FE, 30/10 ([vídeo](https://drive.google.com/file/d/18JaCDjUtASLCdQrbMRw2V49AzesUXK67/view?usp=drive_link))                                                                                  | ✅                                       |
| Desempenho                        | 5 min de uso contínuo, alternando os QR codes exibidos em um monitor e movendo o tablet, com o painel de depuração (FPS) consultado durante o teste                                                                                               | 30 fps e sem travar                                      | Nos momentos em que o painel de depuração estava aberto, o FPS permaneceu em 30, mesmo ao mover o tablet e trocar os QR codes (observação visual, sem registro contínuo dos valores). O app não travou, não fechou e não ficou lento, e não houve aquecimento perceptível. Galaxy Tab S9 FE, 31/10 ([vídeo](https://drive.google.com/file/d/1ACtUHaKLJrBJdK2zor92q3utx3Aw_69Q/view?usp=drive_link)) | ✅                                       |
| Offline                           | Desligar Wi-Fi e Bluetooth, ligar o modo avião, abrir o app e ler 2 QR codes                                                                                                                                                                      | Leitura e rota funcionam sem rede                        | Wi-Fi e Bluetooth desligados e modo avião ligado antes de abrir o app; 2 QR codes lidos normalmente, com a rota carregada. Testado apenas no Galaxy Tab S9 FE ([vídeo](https://drive.google.com/file/d/1UJedqn4sCrBkW8ny2FhqSKy3TsL88zlw/view?usp=sharing))                                                                                                                                         | ✅                                       |
| Verificação em campo da v1.1      | 12 casos de verificação técnica (fluxo completo, QR inválido, deslocamento, aviso de curva, perda de rastreamento, recuperação, orientação inicial, chegada, desempenho, interface sem ajuda, legibilidade e modo offline), de 28/10 a 04/11/2026 | Critério definido por caso                               | 9 atendidos, 2 parciais (caso 7, orientação inicial; caso 11, legibilidade) e 1 não executado (caso 6, recuperação). No caso 8, a chegada foi avaliada pela especificação da v1.1: mensagem ao completar os 9 m do último trecho, sem o QR da saída. Detalhes no artigo V3 (Quadro 2)                                                                                                               | 9 atendidos, 2 parciais, 1 não executado |
| Experimento com usuários          | 5 participantes presenciais (P01–P05, app v1.1, Galaxy Tab S9 FE) e 5 online (P06–P10, 6 fotos da sinalização por videochamada), em 05 e 06/11/2026; 6 pontos de decisão na rota L-0-11 → L-0-01 (41,22 m)                                        | Proporção de direções corretas por ponto de decisão (H1) | Presencial: 30/30, todos chegaram (pela mensagem do app), tempos de 01:13 a 01:40 (mediana 01:33), 3 avisos de "Posição incerta" e 2 hesitações no fim de L-0-15. Online: 23/30 (76,7%), com 1/5 no ponto P6. Grupos pequenos, não equivalentes e sem sorteio: compatível com H1, que fica parcialmente sustentada, sem generalizar (artigo V3, seção 4)                                            | ✅                                       |

> Tolerância de deslocamento definida antes da coleta: ±0,5 m ou 10% do trecho. As medições de deslocamento correspondem às tentativas válidas; em cerca de 1 a cada 5 tentativas nos celulares a contagem falhou e a tentativa foi descartada e repetida (ver Limitações Conhecidas).

---

## 23. Dispositivos Testados

| Dispositivo              | Sistema | Versão        | Resultado                                                                                                                                                        |
| ------------------------ | ------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Samsung Galaxy Tab S9 FE | Android | 16 (One UI 8) | Build instalado; HUD (27/10), fluxo completo (30/10), modo avião e uso contínuo de 5 minutos (31/10), experimento com usuários presencial (05 e 06/11, app v1.1) |
| Samsung Galaxy M54 5G    | Android | 16 (One UI 8) | Testes de deslocamento e avisos (28/10) e modo avião (04/11)                                                                                                     |
| Samsung Galaxy A54 5G    | Android | 16 (One UI 8) | Testes de deslocamento e avisos (28/10) e modo avião (04/11)                                                                                                     |

---

## 24. Limitações Conhecidas

- A estimativa de deslocamento entre QR codes tem erro; trechos longos (até 18 m) acumulam mais erro. A medição real está na seção de testes.
- Os QR codes ficam a mais de 8 m de distância em 30 dos 38 trechos (média de 8,6 m; máximo de 18 m), por causa do posicionamento das colunas do prédio; o recomendado era de 5 a 8 m.
- A direção da primeira curva depende de como o QR é posicionado. A v1.0 não registrava a orientação do QR; a v1.1 registra no `trajeto.json`, mas só nos pontos do térreo (veja abaixo). Nos pontos sem esse dado vale a mitigação: colar o QR virado para o sentido de caminhada, à altura do peito.
- Na escada, a estimativa mede só o plano horizontal; por isso não há aviso por distância nesse trecho. O QR no fim da escada continua a rota.
- O aviso de rastreamento perdido depende do estado da sessão do ARCore; em campo, cobrir a câmera o disparou (28/10), mas outras causas de perda de rastreamento não foram testadas.
- Os ícones em PNG/SVG foram gerados com apoio de IA e não são usados pelo aplicativo: o HUD desenha a seta e os ícones por código. Não houve arquivo de design no Figma.
- A contagem de deslocamento falhou de forma intermitente nas gravações de campo (27/10): em cerca de 1 a cada 5 tentativas, nos celulares Galaxy M54 5G e Galaxy A54 5G, o contador parou de acompanhar o percurso (por exemplo, marcou 7,5 m ao fim de um trecho de 9 m), como se o rastreamento se perdesse durante a caminhada. Isso não foi observado no Galaxy Tab S9 FE. As tentativas com falha foram descartadas e repetidas; as medições da seção de testes correspondem às tentativas válidas. A causa não foi identificada.
- Testado em três aparelhos Samsung (Tab S9 FE, Galaxy M54 5G e Galaxy A54 5G); não há garantia de compatibilidade com outros aparelhos.
- O pedido de novo QR por trecho excedido (distância maior que o trecho cadastrado mais 1,5 m) foi verificado apenas nos testes automatizados; em campo foi testada a perda de rastreamento.
- A curva à direita foi verificada apenas com o QR exibido em um monitor (print), sem caminhar um trecho real no prédio; em campo foram gravados esquerda e reto.
- O experimento com usuários (05 e 06/11/2026; 5 presenciais com o app v1.1 e 5 online com fotos da sinalização) é preliminar: grupos pequenos, não equivalentes e sem sorteio, um único aparelho (Galaxy Tab S9 FE), sem comitê de ética (CEP/Conep) e sem inspeção formal do percurso. O resultado é compatível com a hipótese H1, mas não a comprova e não permite generalizar. Detalhes no artigo V3 (seção 4).
- Na v1.1, a mensagem "Você chegou" aparece ao completar os 9 m do último trecho (L-0-02 → L-0-01), por distância estimada, antes da leitura do QR de L-0-01. No experimento, o QR da saída não foi apresentado.
- A orientação do QR da v1.1 só existe para os pontos do térreo usados no experimento: L-0-07, L-0-09 e os andares 1 e 2 não têm o dado, e o de L-0-02 foi inferido do giro pedido em campo (a face desse QR não foi conferida). Nesses pontos o app se comporta como na v1.0.
- No experimento, a seta confundiu participantes no fim de L-0-15 (a seta indicava a esquerda e o QR de L-0-02 estava à direita; 4 dos 5 participantes presenciais descreveram a confusão) e o app mostrou "Posição incerta" 3 vezes (P02, P03 e P04). O código não foi alterado depois disso.
- O app não exibe aviso de que é experimental e não substitui a sinalização oficial; isso fica como melhoria futura.
- Cobertura do mapeamento: o pilar E5 do 2º andar não foi mapeado e, por isso, não está no `trajeto.json` nem tem QR code; os pontos de rampa (`L-0-08`, `L-1-08` e `L-2-08`) também não têm QR. O aplicativo funciona sobre os pontos cadastrados; incluir o E5 exige novo mapeamento, atualização do JSON e novo QR.
- As distâncias do mapeamento vêm do levantamento da equipe e não foram remedidas com trena, exceto nos testes de campo.

---

## 25. Evidências de Funcionamento

- Fluxo completo no Prédio L, partindo de E2 até a saída, no Galaxy Tab S9 FE (`FluxoCompleto.mp4`): [Assistir demonstração](https://drive.google.com/file/d/18JaCDjUtASLCdQrbMRw2V49AzesUXK67/view?usp=drive_link)
- Print do HUD em rota ativa: `docs/images/hud_rota.jpeg`
- Vídeos de deslocamento (5 m e 9 m, 3 repetições cada), avisos e direção/instruções (`DirecaoInstrucoes.mp4`): [Pasta UPX08 no Google Drive](https://drive.google.com/drive/folders/1dR31eKBIoCvF3lHZx4ewHU1OO9hYw7vx?usp=sharing)
- Teste de desempenho de 5 minutos de uso contínuo, Galaxy Tab S9 FE: [Assistir](https://drive.google.com/file/d/1ACtUHaKLJrBJdK2zor92q3utx3Aw_69Q/view?usp=drive_link)
- Teste em modo avião, Galaxy Tab S9 FE (`AirPlaneModeRecord.mp4`): [Assistir](https://drive.google.com/file/d/1UJedqn4sCrBkW8ny2FhqSKy3TsL88zlw/view?usp=sharing)
- Print do Test Runner da v1.1 (52 testes verdes, 07/11): `docs/images/testes_unitarios_v1.1.png`
- Print do Test Runner da v1.0 (27 testes verdes, 29/10): `docs/images/testes_unitarios.png`
- Dados do experimento de 05 e 06/11 (fichas, questionários e vídeos de tela, identificados só por códigos P01–P10): disponíveis ao docente em pasta restrita do Google Drive; o link não é publicado neste repositório.

![Test Runner da v1.1: 52 testes](docs/images/testes_unitarios_v1.1.png)

---

## 26. Repositórios e Recursos

| Recurso                   | Link                                                                                                 |
| ------------------------- | ---------------------------------------------------------------------------------------------------- |
| Código-fonte              | https://github.com/UPX08-RA/safesight                                                                |
| Trello                    | [Quadro UPX 8](https://trello.com/b/XR5ZFyCX/upx-8)                                                  |
| Vídeos de teste           | [Google Drive](https://drive.google.com/drive/folders/1dR31eKBIoCvF3lHZx4ewHU1OO9hYw7vx?usp=sharing) |
| Documentação complementar | `docs/documents/`                                                                                    |

---

## 27. Cronograma Técnico

| Atividade                                      | Responsável                                                                                | Status                                         |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------ | ---------------------------------------------- |
| Configuração do projeto Unity e AR Foundation  | Gustavo                                                                                    | ✅                                             |
| Pesquisa e artigo científico                   | Equipe                                                                                     | ✅                                             |
| Mapeamento do Prédio L (planilha e plantas)    | Giulia                                                                                     | ✅                                             |
| Leitura de QR code                             | Gustavo                                                                                    | ✅                                             |
| Dados do trajeto (JSON e conversor)            | Gustavo                                                                                    | ✅                                             |
| Lógica de navegação e testes                   | Gustavo e João                                                                             | ✅                                             |
| Estimativa de deslocamento com ARCore          | Gustavo                                                                                    | ✅                                             |
| HUD de navegação                               | Gustavo                                                                                    | ✅                                             |
| Geração e validação dos QR codes               | Gustavo e Giulia                                                                           | 🚧                                             |
| Ícones e seta (PNG/SVG)                        | Gerados com apoio de IA e entregues à equipe; o HUD atual desenha seta e ícones por código | ✅ (assets entregues; ainda não usados no app) |
| Testes de campo no Prédio L                    | Equipe                                                                                     | 🚧                                             |
| Experimento com usuários (presencial e online) | Equipe                                                                                     | ✅ (05 e 06/11)                                |
| Documentação técnica                           | Equipe                                                                                     | ✅                                             |

---

## 28. Controle de Versão

As funcionalidades principais (leitura de QR, deslocamento, `trajeto.json`, controle de navegação e HUD) foram desenvolvidas em branches `feature/*` e integradas por pull requests (#1 a #4). Ajustes posteriores do HUD e a documentação (prints, diagramas e README) foram commitados direto na `main`. Exemplos de mensagens de commit:

```text
feat: implementa leitura de QR com ZXing.Net
feat: adiciona controle de navegação por rota cadastrada
test: adiciona testes de regressão da navegação
docs: atualiza README
```

As versões estão marcadas com tags:

| Tag    | Commit    | Descrição                                                                                                                                           |
| ------ | --------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `v1.0` | `e9271d5` | Entrega da documentação técnica (31/10/2026); 27 testes                                                                                             |
| `v1.1` | `d61a22c` | Versão usada na coleta de 05 e 06/11/2026: orientação do QR, curva no meio do trecho e 52 testes. Desenvolvida na branch `feature/v1.1-experimento` |

---

## 29. Problemas Encontrados e Soluções

### Problema 1 — Leitura de QR com a câmera já ocupada pelo ARCore

**Problema:** não era possível usar uma segunda câmera (`WebCamTexture`) para ler QR.
**Causa:** o ARCore ocupa a câmera.
**Solução:** obter o frame pelo `ARCameraManager` (`XRCpuImage`), copiar o plano de luminância e decodificar com o ZXing.Net a cada 5 frames, mantendo 30 fps.

### Problema 2 — O mesmo QR no campo de visão reiniciava a estimativa

**Problema:** a estimativa zerava enquanto o mesmo QR continuava visível.
**Causa:** cada leitura do mesmo QR era tratada como um novo ponto.
**Solução:** intervalo de 1,5 s para reler o QR atual, e só um QR válido reinicia a estimativa (correção revisada nos testes de regressão).

### Problema 3 — Distância horizontal na escada

**Problema:** a estimativa não representa o comprimento de uma escada.
**Causa:** o `DisplacementEstimator` mede só o plano horizontal.
**Solução:** trechos de escada não têm aviso por distância; o QR no fim da escada continua a rota.

### Problema 4 — Alterações da cena fora do commit

**Problema:** a cena e os `.meta` não apareciam no `git status`.
**Causa:** a cena não foi salva antes do commit.
**Solução:** `Ctrl+S` na cena antes de commitar e conferir o `git status` (regra da equipe).

### Problema 5 — Defeitos visuais do HUD no tablet

**Problema:** o botão "Reiniciar" cobria o aviso de elevador, o texto do destino era cortado em telas estreitas, a seta e os ícones estavam desalinhados e os painéis de depuração apareciam por padrão.
**Causa:** o layout do HUD (IMGUI) usava posições e tamanhos fixos, sem adaptar à largura útil da tela.
**Solução:** layout recalculado a partir da altura e da largura úteis, botões de ícone no canto do cartão superior, texto com redução automática e painéis de depuração ocultos por padrão (botão de depuração para exibi-los). Verificado no tablet em 27/10. Em 29/10, o mesmo tipo de defeito (texto de tamanho fixo que não cabia) apareceu na instrução inicial "Aponte a câmera para um QR Code para começar", que passou a encolher para caber no cartão (verificado no tablet). O aviso "QR não reconhecido" recebeu o mesmo ajuste de forma preventiva; nos testes ele sempre coube na tela, sem corte observado.

### Problema 6 — Contagem de deslocamento falhando de forma intermitente

**Problema:** nos testes de campo com os celulares, em cerca de 1 a cada 5 tentativas a distância percorrida mostrada pelo app ficava abaixo da real (por exemplo, 7,5 m ao final de um trecho de 9 m), como se o rastreamento se perdesse durante o percurso. Não foi observado no tablet.
**Causa:** não identificada. O comportamento é compatível com perda momentânea de rastreamento do ARCore durante a caminhada, mas isso não foi investigado.
**Solução:** sem correção definitiva. Mitigação: o app pede nova leitura de QR quando detecta a perda de rastreamento ou o trecho excedido, e as tentativas com falha foram descartadas e repetidas nos testes.

### Problema 7 — QR lido de lado em relação ao trecho

**Problema:** o QR fica numa coluna virado para o corredor; ao lê-lo, o usuário pode estar de lado em relação ao primeiro trecho, e a seta (HUD fixo) não dizia para onde girar.
**Causa:** o ZXing.Net só decodifica o texto do QR, sem pose, e o app não conhecia a direção para onde cada QR estava virado.
**Solução (v1.1):** o `trajeto.json` passou a guardar a direção da face do QR e o sentido do primeiro trecho. A `NavigationLogic` calcula o giro necessário e o HUD mostra "Vire à direita" ou "Vire à esquerda", com a seta apontando o giro, até o usuário ficar alinhado (tolerância de 30°). O dado existe só nos pontos do térreo da rota do experimento, e o de L-0-02 foi inferido do giro pedido em campo. No experimento ainda houve hesitação no fim de L-0-15 (ver Limitações Conhecidas).

### Problema 8 — Trecho com curva no meio (P6 → P7)

**Problema:** no percurso de P6 (L-0-14) até P7 (L-0-15), observado em campo em 04/11, o usuário anda cerca de 1 m para passar a coluna, vira à esquerda, anda mais 2 m e vira à esquerda para ler o QR de P7. O modelo de trecho só previa curva no fim.
**Causa:** cada trecho do `trajeto.json` tinha apenas uma curva, no fim do trecho.
**Solução (v1.1):** campos opcionais `viaMeters`, `viaTurn` e `afterViaMeters` no trecho. A curva do meio é confirmada pelo giro (60°) e a distância restante é calculada por etapa ("etapa 1/2" e "etapa 2/2" no painel de depuração). Só esse trecho usa o recurso; o `gerar_trajeto.py` o aplica à rota que passa por P6.

---

## 30. Melhorias Futuras

- registrar a orientação do QR (direção da primeira curva);
- mais QR codes ao longo dos trechos longos;
- repetir o experimento com amostra maior, grupos sorteados e mais de um aparelho (SafeSight vs. sinalização estática);
- completar o dado de orientação dos QR (L-0-07, L-0-09 e andares 1 e 2) e conferir a face do QR de L-0-02;
- tratar a confusão no fim de L-0-15 (seta e QR de L-0-02) e avaliar a chegada pela leitura do QR da saída, e não só pela distância estimada;
- aviso de "experimental" na tela;
- teste em outros dispositivos Android;
- rastreamento contínuo em mapa 2D e rota ancorada ao piso.

---

## 31. Custos e Recursos

| Recurso            | Tipo     | Custo/Condição              |
| ------------------ | -------- | --------------------------- |
| Unity              | Software | Licença Personal (gratuita) |
| Git e GitHub       | Software | Gratuito                    |
| Galaxy Tab S9 FE   | Hardware | Equipamento da equipe       |
| QR codes impressos | Material | Impressão em papel sulfite  |

---

## 32. Licença

Projeto acadêmico desenvolvido para fins educacionais na disciplina de UPX — Realidade Aumentada.

---

## 33. Autores

- **Gustavo Oliveira Gomes** — Tech Lead
- **João Victor Cardoso Engler Rizzi de Araujo** — lógica de navegação
- **Giulia Oliveira** — mapeamento e QR codes
- **Luiz Augusto Marques Zocca** — Scrum Master e testes de campo
- **Henrique Seiji Kuroiwa** — pesquisa, artigo e documentação

**Instituição:** UniFacens
**Disciplina:** UPX — Realidade Aumentada
**Semestre:** 8º semestre (2026.2)
