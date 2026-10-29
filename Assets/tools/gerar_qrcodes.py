"""
Gera os QR codes (PNG + PDF para impressao) a partir do trajeto.json e valida.
Uso: python3 gerar_qrcodes.py trajeto.json pasta_saida
Validacao: decodifica cada PNG (inclusive reduzido e borrado) e compara com o ID;
confere que toda rota termina na saida e que todo ponto citado tem QR.
"""
import sys, os, json
import cv2, numpy as np
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.pdfgen import canvas

src, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
d = json.load(open(src, encoding="utf-8"))
pts = {p["id"]: p for p in d["points"]}
exit_id = d["exitId"]
floors = {"0": "Térreo", "1": "1º andar", "2": "2º andar"}

enc = cv2.QRCodeEncoder.create()
def make_png(pid):
    img = enc.encode(pid)
    big = cv2.resize(img, None, fx=20, fy=20, interpolation=cv2.INTER_NEAREST)
    big = cv2.copyMakeBorder(big, 80, 80, 80, 80, cv2.BORDER_CONSTANT, value=255)  # zona silenciosa (4 modulos)
    path = os.path.join(out, f"{pid}.png")
    cv2.imwrite(path, big)
    return path, big

det = cv2.QRCodeDetector()
def decode(img):
    t, _, _ = det.detectAndDecode(img)
    return t

report, errors = [], []
imgs = {}
for pid in pts:
    path, big = make_png(pid)
    imgs[pid] = path
    ok_full = decode(big) == pid
    scales = (0.3, 0.35, 0.4, 0.45, 0.5, 0.6, 0.7)
    hits = 0
    for f in scales:
        small = cv2.GaussianBlur(cv2.resize(big, None, fx=f, fy=f, interpolation=cv2.INTER_AREA), (3, 3), 0)
        hits += decode(small) == pid
    report.append((pid, ok_full, hits, len(scales)))
    if not ok_full: errors.append(f"QR de {pid} nao decodifica em tamanho cheio")
    if hits < 5: errors.append(f"QR de {pid} decodifica so em {hits}/{len(scales)} escalas reduzidas")

# consistencia com as rotas
ids = set(pts)
if len(ids) != len(d["points"]): errors.append("IDs duplicados em points")
if exit_id not in ids: errors.append("saida sem QR")
legs_m = []
edges_u = {}
for r in d["routes"]:
    if r["startId"] not in ids: errors.append(f"rota parte de ponto sem QR: {r['startId']}")
    if r["legs"][-1]["toId"] != exit_id: errors.append(f"rota {r['startId']} nao termina na saida")
    prev = r["startId"]
    for l in r["legs"]:
        legs_m.append(l["meters"])
        edges_u[tuple(sorted((prev, l["toId"])))] = l["meters"]
        prev = l["toId"]
        if l["toId"] not in ids: errors.append(f"destino sem QR: {l['toId']}")
sem_rota = ids - {r["startId"] for r in d["routes"]} - {exit_id}
if sem_rota: errors.append(f"pontos sem rota: {sorted(sem_rota)}")

# PDF: 1 QR por pagina A4. Imagem de 14 cm => area do codigo (25 de 33 modulos) = 10,6 cm (>= 10 cm)
W, H = A4
c = canvas.Canvas(os.path.join(out, "QRCodes_Predio_L.pdf"), pagesize=A4)
order = sorted(pts, key=lambda k: (k.split("-")[1], k))
SIZE = 14 * cm
for i, pid in enumerate(order):
    if i > 0: c.showPage()
    p = pts[pid]
    fl = floors[pid.split("-")[1]]
    is_exit = pid == exit_id
    c.setFont("Helvetica-Bold", 18)
    c.drawCentredString(W / 2, H - 2.2 * cm, f"PRÉDIO L · {fl}" + ("  ·  SAÍDA" if is_exit else ""))
    c.drawImage(imgs[pid], (W - SIZE) / 2, H - 3.2 * cm - SIZE, SIZE, SIZE)
    c.setFont("Helvetica-Bold", 54)
    c.drawCentredString(W / 2, H - 3.2 * cm - SIZE - 2.2 * cm, pid)
    c.setFont("Helvetica", 12)
    desc = (p.get("description") or "")
    c.drawCentredString(W / 2, H - 3.2 * cm - SIZE - 3.4 * cm, desc[:90])
    c.setFont("Helvetica", 8)
    c.drawCentredString(W / 2, 1.2 * cm, "SafeSight · imprimir a 100% (sem ajustar à página), papel fosco")
c.save()

n = len(report)
em = list(edges_u.values())
over8 = sum(1 for m in em if m > 8)
lines = [
    "Validacao dos QR codes - Predio L",
    f"Pontos/QR gerados: {n} (saida: {exit_id})",
    f"Decodificacao do PNG cheio: {sum(1 for r in report if r[1])}/{n}",
    f"Decodificacao reduzida+borrada (7 escalas, 30-70%, OpenCV): total {sum(r[2] for r in report)}/{n*7}; pior QR {min(r[2] for r in report)}/7",
    f"Rotas: {len(d['routes'])} (todas terminam na saida: {'sim' if not any('nao termina' in e for e in errors) else 'NAO'})",
    f"Trechos usados nas rotas: {len(em)} (distancia entre QRs vizinhos); media = {sum(em)/len(em):.1f} m; maior = {max(em)} m; acima de 8 m: {over8}",
    "Erros: " + ("nenhum" if not errors else "; ".join(errors)),
]
open(os.path.join(out, "validacao.txt"), "w", encoding="utf-8").write("\n".join(lines) + "\n")
print("\n".join(lines))
