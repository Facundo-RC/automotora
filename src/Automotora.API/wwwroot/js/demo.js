/**
 * Modo demo.
 *
 * GitHub Pages sirve solamente archivos estáticos: no hay forma de levantar
 * ahí la API en C#. Para que la demo publicada igual se pueda recorrer entera,
 * este módulo reimplementa en memoria lo que hace el backend —filtros, orden,
 * paginado y alta/baja/modificación— a partir de datos-demo.json.
 *
 * Se activa solo cuando la API no responde. Con la API levantada, nada de esto
 * se usa.
 */
const demo = (() => {
  const RUTA_DATOS = 'datos-demo.json';
  const TAMANO_MAXIMO = 50;

  let marcas = [];
  let vehiculos = [];
  let cargado = false;
  let proximoId = 1;

  async function asegurarDatos() {
    if (cargado) return;

    const respuesta = await fetch(RUTA_DATOS);
    if (!respuesta.ok) {
      throw new ErrorApi('No se pudieron cargar los datos de ejemplo.');
    }

    const datos = await respuesta.json();
    marcas = datos.marcas;
    vehiculos = datos.vehiculos.map(v => ({ ...v }));
    proximoId = Math.max(...vehiculos.map(v => v.id)) + 1;
    cargado = true;
  }

  /** Mismo criterio de búsqueda que el backend, pero sin distinguir acentos ni mayúsculas. */
  function normalizar(texto) {
    return String(texto ?? '')
      .toLowerCase()
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '');
  }

  function aplicarFiltros(lista, f) {
    return lista.filter(v => {
      if (f.texto) {
        const aguja = normalizar(f.texto);
        const pajar = normalizar(`${v.modelo} ${v.descripcion ?? ''} ${v.marca}`);
        if (!pajar.includes(aguja)) return false;
      }

      if (f.marcaId && v.marcaId !== Number(f.marcaId)) return false;
      if (f.anioMin && v.anio < Number(f.anioMin)) return false;
      if (f.anioMax && v.anio > Number(f.anioMax)) return false;
      if (f.precioMin && v.precio < Number(f.precioMin)) return false;
      if (f.precioMax && v.precio > Number(f.precioMax)) return false;
      if (f.combustible && v.combustible !== f.combustible) return false;
      if (f.transmision && v.transmision !== f.transmision) return false;

      if (f.vendido === 'true' && !v.vendido) return false;
      if (f.vendido === 'false' && v.vendido) return false;

      return true;
    });
  }

  function aplicarOrden(lista, orden) {
    const copia = [...lista];

    switch (orden) {
      case 'precio':      return copia.sort((a, b) => a.precio - b.precio);
      case 'precio_desc': return copia.sort((a, b) => b.precio - a.precio);
      case 'anio':        return copia.sort((a, b) => a.anio - b.anio);
      case 'anio_desc':   return copia.sort((a, b) => b.anio - a.anio);
      case 'km':          return copia.sort((a, b) => a.kilometraje - b.kilometraje);
      default:            return copia.sort((a, b) =>
                            new Date(b.fechaPublicacion) - new Date(a.fechaPublicacion));
    }
  }

  function buscarPorId(id) {
    const vehiculo = vehiculos.find(v => v.id === Number(id));
    if (!vehiculo) throw new ErrorApi(`No existe un vehículo con id ${id}.`, 404);
    return vehiculo;
  }

  /** Reproduce las validaciones del DTO para que el formulario se comporte igual. */
  function validar(datos) {
    const errores = {};

    if (!datos.modelo || !datos.modelo.trim()) {
      errores.Modelo = ['El modelo es obligatorio.'];
    } else if (datos.modelo.length > 80) {
      errores.Modelo = ['El modelo no puede superar los 80 caracteres.'];
    }

    if (!(datos.anio >= 1900 && datos.anio <= 2100)) {
      errores.Anio = ['El año debe estar entre 1900 y 2100.'];
    } else if (datos.anio > new Date().getFullYear() + 1) {
      errores.Anio = ['El año no puede ser posterior al próximo año.'];
    }

    if (!(datos.precio > 0)) errores.Precio = ['El precio debe ser mayor a cero.'];
    if (datos.kilometraje < 0) errores.Kilometraje = ['El kilometraje no puede ser negativo.'];
    if (!marcas.some(m => m.id === Number(datos.marcaId))) {
      errores.MarcaId = ['La marca indicada no es válida.'];
    }

    if (Object.keys(errores).length > 0) {
      throw new ErrorApi('Hay errores de validación en los datos enviados.', 400, errores);
    }
  }

  /** Misma regla que ValidarDuplicadoAsync en el servicio. */
  function validarDuplicado(datos, idActual) {
    const duplicado = vehiculos.some(v =>
      v.marcaId === Number(datos.marcaId) &&
      v.modelo === datos.modelo.trim() &&
      v.anio === datos.anio &&
      v.kilometraje === datos.kilometraje &&
      v.id !== idActual);

    if (duplicado) {
      throw new ErrorApi(
        'Ya existe un vehículo cargado con la misma marca, modelo, año y kilometraje.', 400);
    }
  }

  function nombreDeMarca(marcaId) {
    return marcas.find(m => m.id === Number(marcaId))?.nombre ?? '';
  }

  return {
    async listarVehiculos(filtros = {}) {
      await asegurarDatos();

      const filtrados = aplicarFiltros(vehiculos, filtros);
      const ordenados = aplicarOrden(filtrados, filtros.ordenarPor);

      let tamanoPagina = Number(filtros.tamanoPagina) || 12;
      if (tamanoPagina < 1 || tamanoPagina > TAMANO_MAXIMO) tamanoPagina = TAMANO_MAXIMO;

      const pagina = Math.max(1, Number(filtros.pagina) || 1);
      const totalItems = ordenados.length;
      const totalPaginas = Math.ceil(totalItems / tamanoPagina);

      return {
        items: ordenados.slice((pagina - 1) * tamanoPagina, pagina * tamanoPagina),
        pagina,
        tamanoPagina,
        totalItems,
        totalPaginas,
        hayPaginaSiguiente: pagina < totalPaginas,
        hayPaginaAnterior: pagina > 1
      };
    },

    async obtenerVehiculo(id) {
      await asegurarDatos();
      return { ...buscarPorId(id) };
    },

    async crearVehiculo(datos) {
      await asegurarDatos();
      validar(datos);
      validarDuplicado(datos, null);

      const nuevo = {
        ...datos,
        id: proximoId++,
        marcaId: Number(datos.marcaId),
        marca: nombreDeMarca(datos.marcaId),
        modelo: datos.modelo.trim(),
        fechaPublicacion: new Date().toISOString()
      };

      vehiculos.push(nuevo);
      return { ...nuevo };
    },

    async actualizarVehiculo(id, datos) {
      await asegurarDatos();
      const vehiculo = buscarPorId(id);
      validar(datos);
      validarDuplicado(datos, vehiculo.id);

      Object.assign(vehiculo, datos, {
        marcaId: Number(datos.marcaId),
        marca: nombreDeMarca(datos.marcaId),
        modelo: datos.modelo.trim()
      });

      return { ...vehiculo };
    },

    async cambiarEstadoVenta(id, vendido) {
      await asegurarDatos();
      const vehiculo = buscarPorId(id);
      vehiculo.vendido = vendido;
      return { ...vehiculo };
    },

    async eliminarVehiculo(id) {
      await asegurarDatos();
      const vehiculo = buscarPorId(id);
      vehiculos = vehiculos.filter(v => v.id !== vehiculo.id);
      return null;
    },

    async listarMarcas() {
      await asegurarDatos();
      return marcas.map(m => ({ ...m }));
    }
  };
})();
