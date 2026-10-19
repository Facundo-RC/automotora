/** Error de API con el detalle de validación que devuelve el backend. */
class ErrorApi extends Error {
  constructor(mensaje, estado = 0, errores = null) {
    super(mensaje);
    this.name = 'ErrorApi';
    this.estado = estado;
    this.errores = errores;
  }
}

/**
 * Capa de acceso a la API. Todo el fetch vive acá: el resto de la app
 * llama métodos con nombre y no se entera de rutas ni de códigos HTTP.
 * Si la API no responde, delega en demo.js (ver "Modo demo" más abajo).
 */
const api = (() => {
  const BASE = '/api';

  /**
   * Envuelve fetch: arma la URL, parsea el JSON y convierte
   * cualquier respuesta de error en una excepción con mensaje usable.
   */
  async function pedir(ruta, opciones = {}) {
    let respuesta;

    try {
      respuesta = await fetch(BASE + ruta, {
        headers: { 'Content-Type': 'application/json' },
        ...opciones
      });
    } catch {
      // Falla de red: el servidor no respondió.
      throw new ErrorApi('No se pudo conectar con el servidor. ¿Está corriendo la API?');
    }

    if (respuesta.status === 204) return null;

    let cuerpo = null;
    const texto = await respuesta.text();
    if (texto) {
      try { cuerpo = JSON.parse(texto); } catch { cuerpo = null; }
    }

    if (!respuesta.ok) {
      const mensaje = cuerpo?.mensaje || `Error ${respuesta.status} al comunicarse con la API.`;
      const error = new ErrorApi(mensaje, respuesta.status, cuerpo?.errores || null);

      // La API siempre contesta JSON, hasta cuando falla. Si el cuerpo no es JSON,
      // del otro lado no hay API: es el 404 de un servidor de archivos estáticos.
      error.respuestaNoEsDeLaApi = cuerpo === null;

      throw error;
    }

    return cuerpo;
  }

  /** Convierte un objeto de filtros en query string, salteando lo vacío. */
  function aQueryString(filtros) {
    const params = new URLSearchParams();

    Object.entries(filtros).forEach(([clave, valor]) => {
      if (valor === '' || valor === null || valor === undefined) return;
      params.append(clave, valor);
    });

    const qs = params.toString();
    return qs ? `?${qs}` : '';
  }

  /* ---------------------- Modo demo ----------------------
   * La demo publicada en GitHub Pages no tiene backend detrás. Cuando la API
   * no contesta, se pasa a modo demo una sola vez y a partir de ahí todas las
   * llamadas las atiende demo.js con datos de ejemplo en memoria.
   *
   * Se pasa a demo en dos casos: no hubo respuesta (estado 0), o respondió algo
   * que no es JSON —el 404 en HTML de un servidor de archivos estáticos—. Un 400
   * o un 404 con cuerpo JSON son respuestas legítimas de la API y se propagan.
   */
  let enModoDemo = false;

  function activarModoDemo() {
    if (enModoDemo) return;

    enModoDemo = true;
    document.dispatchEvent(new CustomEvent('api:modo-demo'));
  }

  /** Intenta contra la API; si no hay servidor, resuelve con los datos de ejemplo. */
  function conRespaldo(nombreMetodo, llamarApi) {
    return async (...args) => {
      if (enModoDemo) return demo[nombreMetodo](...args);

      try {
        return await llamarApi(...args);
      } catch (error) {
        const noHayApi = error instanceof ErrorApi &&
                         (error.estado === 0 || error.respuestaNoEsDeLaApi);

        if (noHayApi) {
          activarModoDemo();
          return demo[nombreMetodo](...args);
        }
        throw error;
      }
    };
  }

  return {
    get enModoDemo() { return enModoDemo; },

    listarVehiculos: conRespaldo('listarVehiculos',
      (filtros) => pedir(`/vehiculos${aQueryString(filtros)}`)),

    obtenerVehiculo: conRespaldo('obtenerVehiculo',
      (id) => pedir(`/vehiculos/${id}`)),

    crearVehiculo: conRespaldo('crearVehiculo',
      (datos) => pedir('/vehiculos', { method: 'POST', body: JSON.stringify(datos) })),

    actualizarVehiculo: conRespaldo('actualizarVehiculo',
      (id, datos) => pedir(`/vehiculos/${id}`, { method: 'PUT', body: JSON.stringify(datos) })),

    cambiarEstadoVenta: conRespaldo('cambiarEstadoVenta',
      (id, vendido) => pedir(`/vehiculos/${id}/estado-venta`,
        { method: 'PATCH', body: JSON.stringify({ vendido }) })),

    eliminarVehiculo: conRespaldo('eliminarVehiculo',
      (id) => pedir(`/vehiculos/${id}`, { method: 'DELETE' })),

    listarMarcas: conRespaldo('listarMarcas',
      () => pedir('/marcas'))
  };
})();
