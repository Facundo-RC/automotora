/**
 * Automotora — lógica de la interfaz.
 * Estructura: estado en un solo objeto, funciones que renderizan a partir
 * de ese estado, y listeners que sólo modifican el estado y vuelven a pedir.
 */
document.addEventListener('DOMContentLoaded', iniciar);

const estado = {
  pagina: 1,
  tamanoPagina: 12,
  marcas: [],
  cargando: false,
  // La vista de cliente es la que se ve al entrar. Las acciones de gestion
  // (publicar, editar, marcar vendido, borrar) viven detras del modo administrador.
  modoAdmin: false
};

const CLAVE_MODO = 'automotora:modoAdmin';

// Cache de nodos usados seguido.
const el = {};

function iniciar() {
  cachearElementos();
  registrarEventos();

  // api.js avisa si tuvo que caer al modo demo por falta de backend.
  document.addEventListener('api:modo-demo', () => {
    const banner = document.getElementById('bannerDemo');
    if (banner) banner.hidden = false;
  });

  recuperarModo();
  cargarMarcas().then(buscar);
}

function cachearElementos() {
  const ids = [
    'fTexto','fMarca','fCombustible','fTransmision','fAnioMin','fAnioMax',
    'fPrecioMin','fPrecioMax','fOrden','fDisponibles','btnLimpiar',
    'grilla','resumen','paginado','modal','modalTitulo','formVehiculo',
    'btnNuevo','btnCerrar','btnCancelar','btnGuardar','formError','aviso',
    'btnModo','modalDetalle','detalleTitulo','detalleCuerpo','btnCerrarDetalle',
    'vId','vMarca','vModelo','vAnio','vPrecio','vKm','vCombustible',
    'vTransmision','vColor','vImagen','vDescripcion','vVendido'
  ];
  ids.forEach(id => { el[id] = document.getElementById(id); });
}

/** Muestra u oculta las acciones de gestion y deja el estado guardado. */
function aplicarModo() {
  document.body.classList.toggle('modo-admin', estado.modoAdmin);
  el.btnModo.textContent = estado.modoAdmin ? 'Salir del modo administrador' : 'Modo administrador';
  el.btnModo.setAttribute('aria-pressed', String(estado.modoAdmin));

  try {
    localStorage.setItem(CLAVE_MODO, estado.modoAdmin ? '1' : '0');
  } catch {
    // Si el navegador bloquea el almacenamiento, el modo dura lo que dure la visita.
  }
}

function recuperarModo() {
  try {
    estado.modoAdmin = localStorage.getItem(CLAVE_MODO) === '1';
  } catch {
    estado.modoAdmin = false;
  }
  aplicarModo();
}

function registrarEventos() {
  el.btnModo.addEventListener('click', () => {
    estado.modoAdmin = !estado.modoAdmin;
    aplicarModo();
    mostrarAviso(estado.modoAdmin
      ? 'Modo administrador activado: ya podes publicar, editar y dar de baja.'
      : 'Volviste a la vista de cliente.');
  });

  el.btnCerrarDetalle.addEventListener('click', cerrarDetalle);
  el.modalDetalle.addEventListener('click', (e) => { if (e.target === el.modalDetalle) cerrarDetalle(); });

  // Los filtros de texto y número esperan a que el usuario deje de escribir.
  ['fTexto','fAnioMin','fAnioMax','fPrecioMin','fPrecioMax']
    .forEach(id => el[id].addEventListener('input', conRetraso(reiniciarYBuscar, 400)));

  ['fMarca','fCombustible','fTransmision','fOrden','fDisponibles']
    .forEach(id => el[id].addEventListener('change', reiniciarYBuscar));

  el.btnLimpiar.addEventListener('click', limpiarFiltros);
  el.btnNuevo.addEventListener('click', () => abrirModal());
  el.btnCerrar.addEventListener('click', cerrarModal);
  el.btnCancelar.addEventListener('click', cerrarModal);
  el.formVehiculo.addEventListener('submit', guardar);

  // Cerrar el modal con clic afuera o con Escape.
  el.modal.addEventListener('click', (e) => { if (e.target === el.modal) cerrarModal(); });
  document.addEventListener('keydown', (e) => {
    if (e.key !== 'Escape') return;
    if (!el.modal.hidden) cerrarModal();
    if (!el.modalDetalle.hidden) cerrarDetalle();
  });
}

/* ============================ Datos ============================ */

async function cargarMarcas() {
  try {
    estado.marcas = await api.listarMarcas();

    const opciones = estado.marcas
      .map(m => `<option value="${m.id}">${escapar(m.nombre)}</option>`)
      .join('');

    el.fMarca.innerHTML = `<option value="">Todas</option>${opciones}`;
    el.vMarca.innerHTML = opciones;
  } catch (error) {
    mostrarAviso(error.message, true);
  }
}

function leerFiltros() {
  return {
    texto: el.fTexto.value.trim(),
    marcaId: el.fMarca.value,
    combustible: el.fCombustible.value,
    transmision: el.fTransmision.value,
    anioMin: el.fAnioMin.value,
    anioMax: el.fAnioMax.value,
    precioMin: el.fPrecioMin.value,
    precioMax: el.fPrecioMax.value,
    ordenarPor: el.fOrden.value,
    vendido: el.fDisponibles.checked ? 'false' : '',
    pagina: estado.pagina,
    tamanoPagina: estado.tamanoPagina
  };
}

async function buscar() {
  if (estado.cargando) return;
  estado.cargando = true;
  el.grilla.innerHTML = '<div class="cargando">Cargando vehículos…</div>';

  try {
    const resultado = await api.listarVehiculos(leerFiltros());
    pintarGrilla(resultado.items);
    pintarResumen(resultado);
    pintarPaginado(resultado);
  } catch (error) {
    el.grilla.innerHTML = `<div class="vacio">${escapar(error.message)}</div>`;
    el.resumen.textContent = '';
    el.paginado.innerHTML = '';
  } finally {
    estado.cargando = false;
  }
}

function reiniciarYBuscar() {
  estado.pagina = 1;
  buscar();
}

/* ============================ Render ============================ */

function pintarGrilla(vehiculos) {
  if (!vehiculos.length) {
    el.grilla.innerHTML = '<div class="vacio">No hay vehículos que coincidan con la búsqueda.</div>';
    return;
  }

  el.grilla.innerHTML = vehiculos.map(plantillaTarjeta).join('');

  el.grilla.querySelectorAll('[data-detalle]').forEach(boton =>
    boton.addEventListener('click', () => abrirDetalle(Number(boton.dataset.detalle))));

  el.grilla.querySelectorAll('[data-editar]').forEach(boton =>
    boton.addEventListener('click', () => abrirModal(Number(boton.dataset.editar))));

  el.grilla.querySelectorAll('[data-vender]').forEach(boton =>
    boton.addEventListener('click', () => alternarVenta(Number(boton.dataset.vender), boton.dataset.vendido === 'true')));

  el.grilla.querySelectorAll('[data-eliminar]').forEach(boton =>
    boton.addEventListener('click', () => eliminar(Number(boton.dataset.eliminar), boton.dataset.nombre)));
}

function plantillaTarjeta(v) {
  const imagen = v.imagenUrl || 'img/vehiculos/sin-foto.svg';

  return `
    <article class="tarjeta ${v.vendido ? 'tarjeta--vendida' : ''}">
      <div class="tarjeta__figura">
        <img src="${escapar(imagen)}" alt="${escapar(v.marca)} ${escapar(v.modelo)}" loading="lazy"
             onerror="this.src='img/vehiculos/sin-foto.svg'">
        ${v.vendido ? '<span class="etiqueta">Vendido</span>' : ''}
      </div>

      <div class="tarjeta__cuerpo">
        <p class="tarjeta__marca">${escapar(v.marca)}</p>
        <h3 class="tarjeta__titulo">${escapar(v.modelo)}</h3>

        <ul class="fichas">
          <li>${v.anio}</li>
          <li>${formatearKm(v.kilometraje)}</li>
          <li>${escapar(etiqueta(v.combustible))}</li>
          <li>${escapar(etiqueta(v.transmision))}</li>
        </ul>

        <div class="tarjeta__pie">
          <p class="tarjeta__precio">${formatearPrecio(v.precio)}</p>
          <button class="boton boton--primario boton--chico" data-detalle="${v.id}">Ver ficha</button>
        </div>

        <div class="tarjeta__gestion" data-solo-admin>
          <button class="boton boton--secundario boton--chico" data-editar="${v.id}">Editar</button>
          <button class="boton boton--secundario boton--chico" data-vender="${v.id}" data-vendido="${v.vendido}">
            ${v.vendido ? 'Reactivar' : 'Marcar vendido'}
          </button>
          <button class="boton boton--peligro boton--chico" data-eliminar="${v.id}"
                  data-nombre="${escapar(v.marca)} ${escapar(v.modelo)}">Borrar</button>
        </div>
      </div>
    </article>`;
}

/* ============================ Ficha ============================ */

async function abrirDetalle(id) {
  try {
    const v = await api.obtenerVehiculo(id);
    const imagen = v.imagenUrl || 'img/vehiculos/sin-foto.svg';

    el.detalleTitulo.textContent = `${v.marca} ${v.modelo}`;
    el.detalleCuerpo.innerHTML = `
      <img class="ficha__imagen" src="${escapar(imagen)}" alt="${escapar(v.marca)} ${escapar(v.modelo)}"
           onerror="this.src='img/vehiculos/sin-foto.svg'">

      <p class="ficha__precio">${formatearPrecio(v.precio)}</p>
      ${v.vendido ? '<p class="ficha__estado">Este vehículo ya fue vendido.</p>' : ''}

      <dl class="ficha__datos">
        <div><dt>Año</dt><dd>${v.anio}</dd></div>
        <div><dt>Kilometraje</dt><dd>${formatearKm(v.kilometraje)}</dd></div>
        <div><dt>Combustible</dt><dd>${escapar(etiqueta(v.combustible))}</dd></div>
        <div><dt>Transmisión</dt><dd>${escapar(etiqueta(v.transmision))}</dd></div>
        <div><dt>Color</dt><dd>${escapar(v.color || 'Sin especificar')}</dd></div>
        <div><dt>Publicado</dt><dd>${formatearFecha(v.fechaPublicacion)}</dd></div>
      </dl>

      ${v.descripcion ? `<p class="ficha__descripcion">${escapar(v.descripcion)}</p>` : ''}
    `;

    el.modalDetalle.hidden = false;
  } catch (error) {
    mostrarAviso(error.message, true);
  }
}

function cerrarDetalle() {
  el.modalDetalle.hidden = true;
}

function pintarResumen(r) {
  if (r.totalItems === 0) { el.resumen.textContent = ''; return; }
  const desde = (r.pagina - 1) * r.tamanoPagina + 1;
  const hasta = Math.min(r.pagina * r.tamanoPagina, r.totalItems);
  el.resumen.textContent = `Mostrando ${desde}–${hasta} de ${r.totalItems} vehículo(s).`;
}

function pintarPaginado(r) {
  if (r.totalPaginas <= 1) { el.paginado.innerHTML = ''; return; }

  el.paginado.innerHTML = `
    <button class="boton boton--secundario" ${r.hayPaginaAnterior ? '' : 'disabled'} data-ir="${r.pagina - 1}">Anterior</button>
    <span>Página ${r.pagina} de ${r.totalPaginas}</span>
    <button class="boton boton--secundario" ${r.hayPaginaSiguiente ? '' : 'disabled'} data-ir="${r.pagina + 1}">Siguiente</button>`;

  el.paginado.querySelectorAll('[data-ir]').forEach(boton =>
    boton.addEventListener('click', () => {
      estado.pagina = Number(boton.dataset.ir);
      buscar();
      window.scrollTo({ top: 0, behavior: 'smooth' });
    }));
}

/* ============================ Modal ============================ */

async function abrirModal(id = null) {
  limpiarErroresDelFormulario();
  el.formVehiculo.reset();
  el.vId.value = '';

  if (id === null) {
    el.modalTitulo.textContent = 'Publicar vehículo';
    el.vAnio.value = new Date().getFullYear();
  } else {
    el.modalTitulo.textContent = 'Editar vehículo';
    try {
      const v = await api.obtenerVehiculo(id);
      el.vId.value = v.id;
      el.vMarca.value = v.marcaId;
      el.vModelo.value = v.modelo;
      el.vAnio.value = v.anio;
      el.vPrecio.value = v.precio;
      el.vKm.value = v.kilometraje;
      el.vCombustible.value = v.combustible;
      el.vTransmision.value = v.transmision;
      el.vColor.value = v.color ?? '';
      el.vImagen.value = v.imagenUrl ?? '';
      el.vDescripcion.value = v.descripcion ?? '';
      el.vVendido.checked = v.vendido;
    } catch (error) {
      mostrarAviso(error.message, true);
      return;
    }
  }

  el.modal.hidden = false;
  el.vModelo.focus();
}

function cerrarModal() {
  el.modal.hidden = true;
}

async function guardar(evento) {
  evento.preventDefault();
  limpiarErroresDelFormulario();

  const datos = {
    marcaId: Number(el.vMarca.value),
    modelo: el.vModelo.value.trim(),
    anio: Number(el.vAnio.value),
    precio: Number(el.vPrecio.value),
    kilometraje: Number(el.vKm.value),
    combustible: el.vCombustible.value,
    transmision: el.vTransmision.value,
    color: el.vColor.value.trim() || null,
    imagenUrl: el.vImagen.value.trim() || null,
    descripcion: el.vDescripcion.value.trim() || null,
    vendido: el.vVendido.checked
  };

  el.btnGuardar.disabled = true;
  el.btnGuardar.textContent = 'Guardando…';

  try {
    const id = el.vId.value;
    if (id) {
      await api.actualizarVehiculo(Number(id), datos);
      mostrarAviso('Vehículo actualizado.');
    } else {
      await api.crearVehiculo(datos);
      mostrarAviso('Vehículo publicado.');
    }
    cerrarModal();
    buscar();
  } catch (error) {
    pintarErroresDelFormulario(error);
  } finally {
    el.btnGuardar.disabled = false;
    el.btnGuardar.textContent = 'Guardar';
  }
}

/* ============================ Acciones ============================ */

async function alternarVenta(id, vendidoActual) {
  try {
    await api.cambiarEstadoVenta(id, !vendidoActual);
    mostrarAviso(vendidoActual ? 'Vehículo reactivado.' : 'Vehículo marcado como vendido.');
    buscar();
  } catch (error) {
    mostrarAviso(error.message, true);
  }
}

async function eliminar(id, nombre) {
  if (!confirm(`¿Eliminar ${nombre}? Esta acción no se puede deshacer.`)) return;

  try {
    await api.eliminarVehiculo(id);
    mostrarAviso('Vehículo eliminado.');
    // Si era el último de la página, se retrocede una.
    if (el.grilla.querySelectorAll('.tarjeta').length === 1 && estado.pagina > 1) estado.pagina--;
    buscar();
  } catch (error) {
    mostrarAviso(error.message, true);
  }
}

function limpiarFiltros() {
  ['fTexto','fAnioMin','fAnioMax','fPrecioMin','fPrecioMax'].forEach(id => { el[id].value = ''; });
  ['fMarca','fCombustible','fTransmision','fOrden'].forEach(id => { el[id].value = ''; });
  el.fDisponibles.checked = true;
  reiniciarYBuscar();
}

/* ============================ Errores y avisos ============================ */

function limpiarErroresDelFormulario() {
  el.formError.hidden = true;
  el.formError.textContent = '';
  el.formVehiculo.querySelectorAll('.error').forEach(nodo => { nodo.textContent = ''; });
}

/** Muestra los errores de validación que devuelve la API debajo de cada campo. */
function pintarErroresDelFormulario(error) {
  if (error.errores) {
    Object.entries(error.errores).forEach(([campo, mensajes]) => {
      const nodo = el.formVehiculo.querySelector(`[data-error="${campo}"]`);
      if (nodo) nodo.textContent = mensajes[0];
    });
  }

  el.formError.textContent = error.message;
  el.formError.hidden = false;
}

let temporizadorAviso;
function mostrarAviso(mensaje, esError = false) {
  el.aviso.textContent = mensaje;
  el.aviso.classList.toggle('aviso--error', esError);
  el.aviso.hidden = false;

  clearTimeout(temporizadorAviso);
  temporizadorAviso = setTimeout(() => { el.aviso.hidden = true; }, 3200);
}

/* ============================ Utilidades ============================ */

/** Ejecuta la función recién cuando pasaron N ms sin nuevas llamadas. */
function conRetraso(fn, ms) {
  let temporizador;
  return (...args) => {
    clearTimeout(temporizador);
    temporizador = setTimeout(() => fn(...args), ms);
  };
}

/** Los enums llegan sin tildes desde C#; acá se muestran como corresponde. */
const ETIQUETAS = {
  Diesel: 'Diésel', Hibrido: 'Híbrido', Electrico: 'Eléctrico', Gnc: 'GNC',
  Automatica: 'Automática'
};
const etiqueta = (valor) => ETIQUETAS[valor] ?? valor;

const formateadorPrecio = new Intl.NumberFormat('es-UY', {
  style: 'currency', currency: 'USD', maximumFractionDigits: 0
});
const formateadorNumero = new Intl.NumberFormat('es-UY');

const formatearPrecio = (valor) => formateadorPrecio.format(valor);

const formatearFecha = (iso) => new Date(iso).toLocaleDateString('es-UY', {
  day: '2-digit', month: 'long', year: 'numeric'
});
const formatearKm = (valor) => `${formateadorNumero.format(valor)} km`;

/** Escapa texto antes de insertarlo en el HTML, para no abrir la puerta a XSS. */
function escapar(texto) {
  if (texto === null || texto === undefined) return '';
  return String(texto)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
