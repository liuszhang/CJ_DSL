// Donut/Pie 纯 SVG 直出（零依赖）
export interface PieDatum {
  value: number;
  label?: string;
}

const COLORS = [
  "#1976D2", "#388E3C", "#F57C00", "#D32F2F", "#7B1FA2",
  "#0097A7", "#C2185B", "#AFB42B", "#455A64", "#5D4037",
];

export function buildDonutSvg(data: PieDatum[], width = 300, height = 300, isDonut = true): string {
  const total = data.reduce((s, d) => s + (Number(d.value) || 0), 0);
  if (total <= 0) {
    return `<div style="color:#999;padding:20px;text-align:center;">所有值为零</div>`;
  }
  const cx = width / 2;
  const cy = height / 2;
  const outerR = Math.min(width, height) / 2 - 10;
  const innerR = isDonut ? Math.round(outerR * 0.65) : 0;

  let path = "";
  let start = -90;
  for (let i = 0; i < data.length; i++) {
    const v = Number(data[i].value) || 0;
    const sweep = (v / total) * 360;
    const end = start + sweep;
    path += arcPath(cx, cy, outerR, innerR, start, end);
    const color = COLORS[i % COLORS.length];
    const label = data[i].label || `项目${i + 1}`;
    const pct = Math.round((v / total) * 1000) / 10;
    path += `<path d="${arcPath(cx, cy, outerR, innerR, start, end)}" fill="${color}" stroke="white" stroke-width="1.5"><title>${esc(label)}: ${v} (${pct}%)</title></path>`;
    start = end;
  }

  let center = "";
  if (isDonut) {
    center = `<text x="${cx}" y="${cy - 6}" text-anchor="middle" font-size="13" fill="#888" font-family="sans-serif">总计</text>` +
      `<text x="${cx}" y="${cy + 18}" text-anchor="middle" font-size="24" font-weight="bold" fill="#333" font-family="sans-serif">${total}</text>`;
  }

  return `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg" role="img">${path}${center}</svg>`;
}

function arcPath(cx: number, cy: number, outerR: number, innerR: number, startDeg: number, endDeg: number): string {
  const rad = (d: number) => (d * Math.PI) / 180;
  const x1o = cx + outerR * Math.cos(rad(startDeg));
  const y1o = cy + outerR * Math.sin(rad(startDeg));
  const x2o = cx + outerR * Math.cos(rad(endDeg));
  const y2o = cy + outerR * Math.sin(rad(endDeg));
  const largeArc = endDeg - startDeg > 180 ? 1 : 0;
  if (innerR === 0) {
    return `M${cx},${cy} L${x1o.toFixed(1)},${y1o.toFixed(1)} A${outerR},${outerR} 0 ${largeArc},1 ${x2o.toFixed(1)},${y2o.toFixed(1)} Z`;
  }
  const x1i = cx + innerR * Math.cos(rad(startDeg));
  const y1i = cy + innerR * Math.sin(rad(startDeg));
  const x2i = cx + innerR * Math.cos(rad(endDeg));
  const y2i = cy + innerR * Math.sin(rad(endDeg));
  return `M${x1o.toFixed(1)},${y1o.toFixed(1)} A${outerR},${outerR} 0 ${largeArc},1 ${x2o.toFixed(1)},${y2o.toFixed(1)} L${x2i.toFixed(1)},${y2i.toFixed(1)} A${innerR},${innerR} 0 ${largeArc},0 ${x1i.toFixed(1)},${y1i.toFixed(1)} Z`;
}

function esc(s: unknown): string {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c] as string));
}
