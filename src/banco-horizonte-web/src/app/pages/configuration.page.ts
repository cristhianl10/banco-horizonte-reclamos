import { Component } from '@angular/core';

@Component({
  template:`
    <header class="page-head"><div><p>ADMINISTRACIÓN / REGLAS</p><h1>Configuración operativa</h1><span>Catálogos y políticas que gobiernan la clasificación automática.</span></div></header>
    <section class="config-grid">
      @for(card of cards;track card.title){<article><div class="index">{{card.index}}</div><span>{{card.tag}}</span><h2>{{card.title}}</h2><p>{{card.description}}</p><div class="status"><i></i>{{card.status}}</div></article>}
    </section>
    <section class="formula"><div><p>MOTOR DE PRIORIDAD / RF-02</p><h2>La prioridad se deriva de hechos verificables.</h2></div><code>+4 NO RECONOCIDA · +3 TRANSFERENCIA/ACCESO · +3 MONTO ≥ 500 · +2 INDISPONIBILIDAD · +2 ABIERTO &gt; 24H</code><div class="scale"><span>0–2 BAJA / 24H</span><span>3–4 MEDIA / 12H</span><span>5–6 ALTA / 6H</span><span>7+ CRÍTICA / 2H</span></div></section>
  `,
  styles:[`.page-head{margin-bottom:27px}.page-head p{color:var(--signal-dark);font:800 9px var(--mono);letter-spacing:.16em}.page-head h1{font-size:36px;margin:7px 0}.page-head span{color:var(--muted);font-size:12px}.config-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:14px}.config-grid article{position:relative;min-height:210px;padding:23px;background:white;border:1px solid var(--line);overflow:hidden}.index{position:absolute;right:-8px;top:-20px;color:#edf1ee;font:700 90px var(--mono)}article>span{position:relative;color:var(--signal-dark);font:800 8px var(--mono);letter-spacing:.13em}article h2{position:relative;margin:22px 0 10px;font-size:17px}article p{position:relative;color:var(--muted);font-size:11px;line-height:1.6}.status{position:absolute;bottom:20px;display:flex;align-items:center;gap:7px;color:#53645f;font:700 8px var(--mono)}.status i{width:6px;height:6px;border-radius:50%;background:var(--positive)}.formula{margin-top:20px;padding:27px;background:var(--ink);color:white}.formula p{color:var(--signal);font:800 9px var(--mono);letter-spacing:.15em}.formula h2{font-size:22px}.formula code{display:block;margin:25px 0;padding:18px;border:1px solid #31504d;background:#0a2729;color:#b7cac6;font:12px var(--mono)}.scale{display:grid;grid-template-columns:repeat(4,1fr);gap:2px}.scale span{padding:11px;background:#183b3b;text-align:center;color:#9db0ad;font:700 9px var(--mono)}.scale span:last-child{background:#723431;color:#ffd8d4}@media(max-width:1000px){.config-grid{grid-template-columns:1fr 1fr}}@media(max-width:550px){.config-grid{grid-template-columns:1fr}.scale{grid-template-columns:1fr 1fr}}`]
})
export class ConfigurationPage {
  readonly cards=[
    {index:'01',tag:'CLASIFICACIÓN',title:'Categorías',description:'Transferencias, tarjetas, cobros, canales digitales y atención.',status:'5 categorías activas'},
    {index:'02',tag:'CICLO DE VIDA',title:'Estados',description:'Nuevo, En análisis, Resuelto y Rechazado con transiciones controladas.',status:'4 estados configurados'},
    {index:'03',tag:'TIEMPO MÁXIMO',title:'Políticas SLA',description:'Plazos de 24, 12, 6 y 2 horas; alerta al consumir 75 %.',status:'4 políticas vigentes'},
    {index:'04',tag:'ACCESO',title:'Roles y usuarios',description:'Permisos separados para operador, analista, supervisor y administrador técnico.',status:'Control por JWT activo'}
  ];
}
