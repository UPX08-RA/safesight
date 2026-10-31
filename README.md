## Visão Geral

O **SafeSight** é um aplicativo Android de Realidade Aumentada que orienta pessoas que não conhecem o prédio até a **saída-alvo** de uma rota segura previamente cadastrada, em uma **simulação controlada de incêndio** (sem fogo ou fumaça reais).

O usuário lê com a câmera o **QR code** de um ponto do corredor. O app identifica o ponto, carrega a rota cadastrada daquele ponto até a saída e, enquanto a pessoa caminha, mostra sobre a imagem da câmera uma **seta, a instrução do trecho, a distância restante, os próximos passos e alertas** (elevador proibido e extintor cadastrado). Entre dois QR codes, a distância percorrida é estimada pelo **rastreamento de movimento do ARCore**. Se o rastreamento se perde ou o trecho é excedido, o app pede uma nova leitura de QR.

A Realidade Aumentada entra como a camada que une a câmera ao vivo, o rastreamento de movimento do ARCore e uma interface de orientação (seta, instruções e alertas) sobreposta à imagem da câmera. Essa interface é fixa na tela e associada ao estado da rota; as indicações não são objetos virtuais ancorados ao piso ou ao ambiente. Tudo funciona **offline**, sem servidor.

![Visão geral do projeto](docs/images/hud_rota.jpeg)

## Problema

Placas de saída fixas pressupõem que a pessoa já conhece o prédio. Isso não funciona bem para calouros, visitantes e professores substitutos, principalmente sob estresse. A orientação para a saída segura depende da memória do usuário e de sinalização estática, que não indica por onde ir a partir do ponto em que a pessoa está.

O impacto é a perda de tempo e a possibilidade de escolher um caminho errado durante uma evacuação. O SafeSight propõe uma orientação passo a passo a partir da posição atual do usuário.

## Objetivos

### Objetivo Geral

Desenvolver uma aplicação de Realidade Aumentada que oriente o usuário, por rota cadastrada, até a saída-alvo do Prédio L em uma simulação controlada de incêndio.

### Objetivos Técnicos

- Ler QR codes pela câmera do ARCore (AR Foundation + ZXing.Net) e identificar o ponto atual.
- Carregar a rota cadastrada de cada ponto a partir de um arquivo local (`trajeto.json`).
- Estimar a distância percorrida entre QR codes pelo rastreamento de movimento do ARCore.
- Avisar a aproximação de curvas e pedir novo QR quando o rastreamento é perdido ou o trecho é excedido.
- Exibir um HUD com seta, instrução, distância restante, próximos passos e alertas.
- Executar a aplicação em dispositivo Android e verificar o funcionamento por testes.

## Público-Alvo

Pessoas que não conhecem o Prédio L da UniFacens: calouros, visitantes e professores substitutos. O uso ocorre em simulação de incêndio, em movimento e com pouca atenção disponível para a tela; por isso o HUD usa texto grande, poucas informações e nenhum menu.

- **Familiaridade tecnológica:** pressupõe-se apenas o uso básico de um smartphone ou tablet (abrir o app e autorizar a câmera); não há cadastro nem configuração.
- **Necessidades:** saber para onde ir a partir do ponto onde está, a cada trecho, e ser avisado da curva e do elevador proibido.
- **Limitações do uso:** o app depende de o usuário encontrar e ler os QR codes e de o dispositivo ter suporte ao ARCore; o experimento com usuários ainda não foi executado.

## Funcionalidades

| ID | Funcionalidade | Descrição | Status |
|---|---|---|---|
| RF01 | Exibir câmera com seta e instruções | Imagem da câmera ao vivo com seta e instruções sobrepostas | ✅ |
| RF02 | Ler QR code | Lê o ID do ponto pela câmera do ARCore | ✅ |
| RF03 | Definir posição e carregar rota | Valida o ID e carrega a rota cadastrada do ponto; nova leitura a qualquer momento | ✅ |
| RF04 | Orientar até a saída-alvo | Segue a rota cadastrada, trecho a trecho, sem elevador | ✅ |
| RF05 | Identificar a saída-alvo | Mostra o destino na tela | ✅ |
| RF06 | Instrução, próximos passos, distância e chegada | Instrução atual, próximos passos, distância restante; chegada ao ler o QR da saída | ✅ |
| RF07 | Alertas | "Não utilize o elevador" e extintor cadastrado do ponto atual | ✅ |
| RF08 | Estimar distância entre QR codes | Distância percorrida pelo rastreamento de movimento do ARCore | ✅ |
| RF09 | Avisar curva | Avisa a aproximação da curva (cerca de 1 m do fim do trecho) | ✅ |
| RF10 | Pedir novo QR | Pede nova leitura se o rastreamento é perdido ou o trecho é excedido | ✅ |

**Legenda:** ✅ Implementado · 🚧 Em desenvolvimento · ⬜ Planejado · ❌ Cancelado

## Requisitos Não Funcionais

| ID | Categoria | Requisito |
|---|---|---|
| RNF01 | Conectividade | Funcionar offline, sem servidor |
| RNF02 | Compatibilidade | Executar em Android com suporte ao ARCore |
| RNF03 | Usabilidade | Interface legível, com texto grande e sem menus |
| RNF04 | Desempenho | Ler QR sem comprometer a fluidez (30 fps ou mais) |
| RNF05 | Infraestrutura | Não depender de nada além dos QR codes impressos |

## Tecnologias Utilizadas

| Tecnologia | Versão | Utilização |
|---|---|---|
| Unity | 6000.3.25f1 (6.3 LTS) | Desenvolvimento principal (template 3D, URP) |
| C# | compatível com Unity 6 | Scripts da aplicação |
| AR Foundation | 6.3.5 | Camada de abstração de RA |
| Google ARCore XR Plugin | 6.3.5 | Rastreamento de movimento e câmera em Android |
| ZXing.Net | 0.16.11 | Decodificação de QR codes |
| NuGetForUnity | pacote Git (GlitchEnzo/NuGetForUnity, `src/NuGetForUnity`) | Instalação do ZXing.Net |
| Unity Test Framework (NUnit) | 1.6.0 | Testes automatizados (EditMode) |
| Universal Render Pipeline (URP) | 17.3.0 | Pipeline gráfico |
| Input System | 1.20.0 | Entrada (novo Input System) |
| XR Plugin Management | 4.6.1 | Carregamento do ARCore no Android |
| Python 3.13 e openpyxl 3.1.5 | versões do ambiente em que os scripts foram executados (25/10); na máquina do Tech Lead há Python 3.12.7 (sem openpyxl instalado) | Conversor planilha→JSON (`Assets/tools/gerar_trajeto.py`) |
| OpenCV (Python) 5.0.0, NumPy 2.5.3 e ReportLab 5.0.1 | mesmo ambiente de execução (Python 3.13, 25/10) | Geração e validação dos QR codes e do PDF de impressão (`Assets/tools/gerar_qrcodes.py`, escrito com apoio de IA) |
| Git e GitHub | — | Controle de versão e hospedagem |

## Hardware Utilizado

| Equipamento | Modelo | Característica relevante | Utilização |
|---|---|---|---|
| Tablet | Samsung Galaxy Tab S9 FE | Android 16 / One UI 8 | Build e verificação do HUD |
| Celular | Samsung Galaxy M54 5G | Android 16 / One UI 8 | Testes de deslocamento e avisos |
| Celular | Samsung Galaxy A54 5G | Android 16 / One UI 8 | Testes de deslocamento e avisos |
| Computador | Notebook com Intel Core i7-9750H 2,60 GHz, 32 GB DDR4, Intel UHD Graphics 630 e NVIDIA GeForce GTX 1660 Ti (6 GB) | CPU, RAM e GPU | Desenvolvimento (Gustavo) |
| Computador | Notebook com Intel Core i7-1165G7 2,80 GHz, 8 GB de RAM e Intel Iris Xe Graphics | CPU, RAM e GPU | Desenvolvimento (João) |

> Foram usados apenas os três aparelhos acima. Nenhuma compatibilidade foi declarada para outros equipamentos.

## Arquitetura do Sistema

O app é 100% Unity e roda localmente. O `QrScanner` lê o QR; o `NavigationController` liga leitura, estimativa de deslocamento e lógica; a `NavigationLogic` (sem dependência de ARCore, testável isoladamente) decide o estado a partir das rotas do `trajeto.json`; o `NavigationHud` desenha a interface.

![Arquitetura do sistema](docs/diagrams/arquitetura.png)

### Fluxo resumido

```text
Usuário → Câmera (ARCore) → QrScanner → NavigationController → NavigationLogic (trajeto.json)
                                    ↘ DisplacementEstimator (pose do ARCore) ↗
NavigationLogic → NavigationHud → Seta, instruções e alertas sobre a câmera
```
