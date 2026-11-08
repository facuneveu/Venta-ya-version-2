# Pruebas de ejecución con Postman

Colección con **75 requests y 124 aserciones** que recorren toda la API de VentaYa en orden:
autenticación → negocio → productos → envíos → cupones → pedidos → respuestas rápidas →
catálogo público → panel → equipo → plan → aislamiento entre negocios.

Cada carpeta usa los tokens e ids que guardan las anteriores, así que **se ejecuta completa y en orden**.
Se puede repetir las veces que quieras: cada corrida crea usuarios nuevos.

| Archivo | Qué es |
|---|---|
| `VentaYa.postman_collection.json` | La colección |
| `VentaYa.local.postman_environment.json` | Entorno con `baseUrl = http://localhost:5080` |
| `VentaYa.produccion.postman_environment.json` | Entorno para MonsterASP (editar `baseUrl`) |
| `ejecucion_local.txt` | Resultado de la última corrida con Newman |

## Con la interfaz de Postman

1. **Import** → seleccioná los tres `.json`.
2. Elegí el entorno (arriba a la derecha) y, si es producción, poné tu URL real en `baseUrl`.
3. Clic derecho en la colección → **Run collection** → **Run**. Todas las pruebas deben salir en verde.

## Con la línea de comandos (Newman)

```bash
npx newman run VentaYa.postman_collection.json -e VentaYa.local.postman_environment.json
```

Para probar el despliegue en MonsterASP, usá el entorno de producción:

```bash
npx newman run VentaYa.postman_collection.json -e VentaYa.produccion.postman_environment.json
```

## Qué verifican

- **Seguridad:** token obligatorio (401), contraseñas incorrectas, empleado sin acceso a equipo/perfil/plan (403) y aislamiento: un negocio no puede ver ni modificar datos de otro (404).
- **Reglas de negocio:** total = ítems + envío − descuento, descuento de stock al pedir y devolución al cancelar, transiciones de estado válidas, cupones (código único, porcentaje ≤ 100, uso), límites del plan Gratis.
- **Catálogo público:** un cliente sin cuenta ve el catálogo y envía un pedido.

> La subida de imágenes (`multipart/form-data`) no está en la colección porque requiere elegir un archivo; se prueba desde Swagger (`/swagger`).
