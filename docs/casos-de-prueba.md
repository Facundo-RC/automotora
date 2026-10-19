# Plan de pruebas — Automotora API

**Proyecto:** Automotora — API de gestión de vehículos
**Responsable:** Facundo Robaño
**Versión probada:** 1.0
**Entorno:** local — API en `http://localhost:5186`, SQL Server LocalDB, base `AutomotoraDb`
**Herramientas:** Postman (colección `postman/Automotora.postman_collection.json`), Swagger UI

---

## 1. Alcance

Se prueban los endpoints REST de vehículos y marcas: alta, consulta, modificación,
cambio de estado de venta y baja, más los filtros y el paginado del listado.

**Fuera de alcance:** autenticación (la API no la implementa todavía), carga de
imágenes al servidor (se guarda una URL externa) y pruebas de carga.

## 2. Tipos de prueba aplicados

| Tipo | Dónde se aplica |
|---|---|
| Funcional / caja negra | Todos los endpoints, contra el contrato de la API |
| Validación de entrada | Campos obligatorios, rangos, tipos y largos máximos |
| Partición de equivalencia | Año válido / año fuera de rango; precio positivo / cero o negativo |
| Valores límite | `tamanoPagina` = 1, 50 y 9999; año 1900, 2100 y 2101 |
| Reglas de negocio | Duplicados, marca inexistente, marca con vehículos asociados |
| Manejo de errores | Códigos HTTP y formato uniforme de la respuesta de error |

## 3. Casos de prueba

Estado: ✅ Pasa · ❌ Falla · ⏳ Pendiente de ejecución

> Los casos están **diseñados pero todavía no ejecutados**. Al correr la suite hay que
> reemplazar cada ⏳ por ✅ o ❌ y registrar abajo los defectos que aparezcan.

### 3.1 Listado y filtros

| ID | Caso | Precondición | Pasos | Resultado esperado | Estado |
|---|---|---|---|---|---|
| CP-01 | Listado por defecto | Base con datos de ejemplo | `GET /api/vehiculos` | 200. Objeto con `items`, `pagina`, `tamanoPagina`, `totalItems`, `totalPaginas` | ⏳ |
| CP-02 | Paginado respeta el tamaño | ≥ 6 vehículos cargados | `GET /api/vehiculos?pagina=1&tamanoPagina=5` | 200. `items` trae como máximo 5 elementos | ⏳ |
| CP-03 | Segunda página | ≥ 13 vehículos | `GET /api/vehiculos?pagina=2` | 200. `pagina` = 2 y los ítems son distintos a los de la página 1 | ⏳ |
| CP-04 | Límite de tamaño de página | — | `GET /api/vehiculos?tamanoPagina=9999` | 200. `tamanoPagina` se topea en 50 | ⏳ |
| CP-05 | Filtro por marca | Marca 7 con vehículos | `GET /api/vehiculos?marcaId=7` | 200. Todos los ítems tienen `marcaId` = 7 | ⏳ |
| CP-06 | Filtro por rango de precio | — | `GET /api/vehiculos?precioMin=20000&precioMax=35000` | 200. Todo `precio` cae dentro del rango | ⏳ |
| CP-07 | Filtro por combustible | — | `GET /api/vehiculos?combustible=Diesel` | 200. Todos los ítems son Diesel | ⏳ |
| CP-08 | Filtro de disponibles | Al menos 1 vendido | `GET /api/vehiculos?vendido=false` | 200. Ningún ítem tiene `vendido` = true | ⏳ |
| CP-09 | Búsqueda por texto | Existe "Corolla" | `GET /api/vehiculos?texto=corolla` | 200. Devuelve el Corolla (búsqueda no sensible a mayúsculas) | ⏳ |
| CP-10 | Búsqueda sin resultados | — | `GET /api/vehiculos?texto=zzzzz` | 200. `items` vacío y `totalItems` = 0. **No** es un 404 | ⏳ |
| CP-11 | Orden por precio ascendente | ≥ 2 vehículos | `GET /api/vehiculos?ordenarPor=precio` | 200. Los precios vienen de menor a mayor | ⏳ |
| CP-12 | Combinación de filtros | — | `GET /api/vehiculos?marcaId=1&anioMin=2020&ordenarPor=precio_desc` | 200. Se cumplen las tres condiciones a la vez | ⏳ |

### 3.2 Alta de vehículos

| ID | Caso | Precondición | Pasos | Resultado esperado | Estado |
|---|---|---|---|---|---|
| CP-13 | Alta válida | Marca 7 existe | `POST /api/vehiculos` con cuerpo completo | 201. Cabecera `Location`. Devuelve el vehículo con `id` y el nombre de la marca resuelto | ⏳ |
| CP-14 | Modelo vacío | — | `POST` con `modelo: ""` | 400. `errores` contiene la clave `Modelo` | ⏳ |
| CP-15 | Año fuera de rango (bajo) | — | `POST` con `anio: 1500` | 400. El error apunta a `Anio` | ⏳ |
| CP-16 | Año futuro | — | `POST` con `anio` = año actual + 5 | 400. "El año no puede ser posterior al próximo año." | ⏳ |
| CP-17 | Precio cero | — | `POST` con `precio: 0` | 400. El error apunta a `Precio` | ⏳ |
| CP-18 | Kilometraje negativo | — | `POST` con `kilometraje: -100` | 400. El error apunta a `Kilometraje` | ⏳ |
| CP-19 | Marca inexistente | — | `POST` con `marcaId: 99999` | 400. Mensaje indicando que la marca no existe | ⏳ |
| CP-20 | Combustible inválido | — | `POST` con `combustible: "Vapor"` | 400. Error de deserialización del enum | ⏳ |
| CP-21 | URL de imagen mal formada | — | `POST` con `imagenUrl: "esto-no-es-una-url"` | 400. El error apunta a `ImagenUrl` | ⏳ |
| CP-22 | Duplicado exacto | CP-13 ejecutado | Repetir el mismo `POST` de CP-13 | 400. "Ya existe un vehículo cargado con la misma marca, modelo, año y kilometraje." | ⏳ |
| CP-23 | Descripción de 1001 caracteres | — | `POST` con descripción de 1001 caracteres | 400. El error apunta a `Descripcion` | ⏳ |
| CP-24 | JSON mal formado | — | `POST` con `{"modelo":` | 400. La API no se cae y responde con error | ⏳ |

### 3.3 Consulta, modificación y baja

| ID | Caso | Precondición | Pasos | Resultado esperado | Estado |
|---|---|---|---|---|---|
| CP-25 | Detalle existente | Vehículo creado en CP-13 | `GET /api/vehiculos/{id}` | 200. Devuelve ese vehículo | ⏳ |
| CP-26 | Detalle inexistente | — | `GET /api/vehiculos/999999` | 404. Cuerpo con `mensaje` | ⏳ |
| CP-27 | Id no numérico | — | `GET /api/vehiculos/abc` | 404. La restricción de ruta `{id:int}` no matchea | ⏳ |
| CP-28 | Actualización válida | Vehículo existente | `PUT /api/vehiculos/{id}` cambiando el precio | 200. El precio queda actualizado | ⏳ |
| CP-29 | Actualizar inexistente | — | `PUT /api/vehiculos/999999` | 404 | ⏳ |
| CP-30 | Marcar como vendido | Vehículo disponible | `PATCH /api/vehiculos/{id}/estado-venta` con `vendido: true` | 200. `vendido` = true | ⏳ |
| CP-31 | Reactivar vendido | CP-30 ejecutado | Mismo PATCH con `vendido: false` | 200. `vendido` = false | ⏳ |
| CP-32 | Baja válida | Vehículo existente | `DELETE /api/vehiculos/{id}` | 204 sin cuerpo | ⏳ |
| CP-33 | Baja repetida | CP-32 ejecutado | Repetir el `DELETE` | 404 | ⏳ |

### 3.4 Marcas

| ID | Caso | Precondición | Pasos | Resultado esperado | Estado |
|---|---|---|---|---|---|
| CP-34 | Listado de marcas | Base migrada | `GET /api/marcas` | 200. Array con las 8 marcas iniciales, cada una con `cantidadVehiculos` | ⏳ |
| CP-35 | Alta de marca | — | `POST /api/marcas` con `{"nombre":"Nissan"}` | 201. Devuelve la marca creada | ⏳ |
| CP-36 | Marca duplicada | CP-35 ejecutado | Repetir el `POST` | 400. "Ya existe una marca llamada 'Nissan'." | ⏳ |
| CP-37 | Nombre vacío | — | `POST` con `{"nombre":""}` | 400. `errores` contiene `Nombre` | ⏳ |
| CP-38 | Borrar marca con vehículos | Marca 1 tiene vehículos | `DELETE /api/marcas/1` | 400. Indica cuántos vehículos la bloquean | ⏳ |
| CP-39 | Borrar marca sin vehículos | Marca creada en CP-35 | `DELETE /api/marcas/{id}` | 204 | ⏳ |

### 3.5 Interfaz web

| ID | Caso | Pasos | Resultado esperado | Estado |
|---|---|---|---|---|
| CP-40 | Carga inicial | Abrir `http://localhost:5186` | Se listan los vehículos disponibles y el selector de marcas viene poblado | ⏳ |
| CP-41 | Filtrar desde la interfaz | Elegir una marca en el desplegable | La grilla se actualiza sin recargar la página | ⏳ |
| CP-42 | Búsqueda con retraso | Escribir en el buscador | Se dispara una sola llamada 400 ms después de dejar de tipear | ⏳ |
| CP-43 | Alta desde el formulario | "Publicar vehículo" → completar → Guardar | Aparece el aviso de confirmación y el vehículo se ve en la grilla | ⏳ |
| CP-44 | Errores del backend en el formulario | Guardar con año 1500 | El mensaje de error se muestra debajo del campo Año | ⏳ |
| CP-45 | Confirmación de borrado | Botón "Borrar" → Cancelar | El vehículo **no** se elimina | ⏳ |
| CP-46 | API caída | Detener la API y recargar | Mensaje "No se pudo conectar con el servidor", sin pantalla en blanco | ⏳ |
| CP-47 | Vista en celular | Reducir a 375 px de ancho | Los filtros se apilan y la grilla pasa a una columna | ⏳ |
| CP-48 | Imagen rota | Cargar un vehículo con una URL de imagen inválida | Se muestra "Sin imagen" en lugar del ícono de imagen rota | ⏳ |

---

## 4. Riesgos detectados y cómo los cubre el diseño

Estos son los puntos que se identificaron como riesgosos al diseñar las pruebas, y la
decisión que se tomó en el código para cubrir cada uno. Sirven además como guía de qué
mirar primero cuando la suite se ejecute.

| ID | Riesgo | Severidad | Cómo lo cubre el código | Caso que lo verifica |
|---|---|---|---|---|
| R-01 | Un cliente pide `tamanoPagina=9999` y la API devuelve toda la tabla de una | Alta | El setter del DTO topea el valor en 50 | CP-04 |
| R-02 | Borrar una marca arrastra sus vehículos por cascada | Alta | `OnDelete(DeleteBehavior.Restrict)` + validación explícita en el service | CP-38 |
| R-03 | Los enums viajan como número (`1`, `2`) y el JSON queda ilegible | Media | `JsonStringEnumConverter` en la configuración de JSON | CP-01, CP-07 |
| R-04 | Una excepción no controlada expone el stack trace al cliente | Alta | Middleware que unifica el formato de error y oculta el detalle interno | CP-24 |
| R-05 | El buscador dispara una llamada por cada tecla | Media | Debounce de 400 ms en el front | CP-42 |
| R-06 | Una URL de imagen inválida deja el ícono roto en la tarjeta | Baja | Handler `onerror` que muestra "Sin imagen" | CP-48 |
| R-07 | `decimal` sin precisión explícita genera advertencia de EF y redondeos raros | Baja | `HasColumnType("decimal(10,2)")` en el mapeo | CP-28 |
| R-08 | Se carga dos veces el mismo auto por error de tipeo | Media | Validación de duplicado por marca + modelo + año + kilometraje | CP-22 |

## 5. Resumen de ejecución

Completar al correr la suite.

| Métrica | Valor |
|---|---|
| Casos diseñados | 48 |
| Casos ejecutados | — |
| Pasan | — |
| Fallan | — |
| Defectos abiertos | — |

## 6. Pendientes para la próxima versión

- Autenticación y autorización: hoy cualquiera puede dar de alta o borrar.
- Pruebas automatizadas con xUnit sobre la capa de servicios.
- Pruebas de carga sobre el listado con volumen alto de datos.
- Carga de imágenes al servidor en lugar de guardar una URL externa.
