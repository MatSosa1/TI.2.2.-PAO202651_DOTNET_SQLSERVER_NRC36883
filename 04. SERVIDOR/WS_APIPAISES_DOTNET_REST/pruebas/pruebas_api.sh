#!/usr/bin/env bash
# Pruebas de punta a punta del servidor APIPaises: recorre los flujos básicos y alternos del
# ECUD contra el servidor desplegado y verifica el código HTTP y el estado de cada respuesta.
#
# Uso (Git Bash):
#   ./pruebas_api.sh [URL_BASE]
#   API_EXTERNA=si ./pruebas_api.sh       # con API key de REST Countries configurada
#
# URL_BASE por defecto: http://localhost:5090/api
# API_EXTERNA=no (por defecto): sin API key, las operaciones que consultan REST Countries
#   deben responder 502 «La API REST Countries no está disponible, intente más tarde».
# API_EXTERNA=si: prueba consultar, importar, registrar, actualizar y eliminar con la API.
#   Deja guardados los países importados (Ecuador y todos los de «Importar todos»).
#
# Requisitos: curl; la base de 03. BDD levantada (docker compose up -d); el usuario admin/admin123 de la migración.

BASE="${1:-http://localhost:5090/api}"
API="${API_EXTERNA:-no}"
CUERPO="$(mktemp)"
trap 'rm -f "$CUERPO"' EXIT

pasaron=0
fallaron=0

# probar <descripción> <método> <ruta> <código esperado> <estado esperado|*> [cuerpo JSON] [token] [texto esperado]
probar() {
    local descripcion="$1" metodo="$2" ruta="$3" codigo="$4" estado="$5" json="$6" token="$7" texto="$8"
    local argumentos=(-s -o "$CUERPO" -w "%{http_code}" -X "$metodo" --max-time 120)
    [ -n "$token" ] && argumentos+=(-H "Authorization: $token")
    [ -n "$json" ] && argumentos+=(-H "Content-Type: application/json" --data-binary "$json")

    local real
    real="$(curl "${argumentos[@]}" "$BASE$ruta")"
    local respuesta
    respuesta="$(cat "$CUERPO")"

    local bien=1
    [ "$real" = "$codigo" ] || bien=0
    if [ "$estado" != "*" ] && [[ "$respuesta" != *"\"estado\":\"$estado\""* ]]; then bien=0; fi
    if [ -n "$texto" ] && [[ "$respuesta" != *"$texto"* ]]; then bien=0; fi

    if [ "$bien" = 1 ]; then
        pasaron=$((pasaron + 1))
        printf '  OK    %-62s %s\n' "$descripcion" "$real"
    else
        fallaron=$((fallaron + 1))
        printf '  FALLA %-62s esperado %s/%s, recibido %s\n        %s\n' \
            "$descripcion" "$codigo" "$estado" "$real" "${respuesta:0:300}"
    fi
}

ultimo() { cat "$CUERPO"; }

echo "Servidor: $BASE  (API externa: $API)"

echo "== ECUD1 Iniciar sesión"
probar "Sin usuario: 400 Ingrese el usuario"         POST /Login/login 400 ERROR '{"clave":"x"}' "" "Ingrese el usuario"
probar "Sin clave: 400 Ingrese la clave"             POST /Login/login 400 ERROR '{"usuario":"admin"}' "" "Ingrese la clave"
probar "Clave incorrecta: 401"                       POST /Login/login 401 ERROR '{"usuario":"admin","clave":"mala"}' "" "Credenciales incorrectas"
probar "Usuario inexistente: 401 (mismo mensaje)"    POST /Login/login 401 ERROR '{"usuario":"nadie","clave":"x"}' "" "Credenciales incorrectas"
probar "JSON mal formado: 400"                       POST /Login/login 400 ERROR '{malo'
probar "Credenciales correctas: 200 con token"       POST /Login/login 200 OK '{"usuario":"admin","clave":"admin123"}' "" "Ingreso de sesión exitoso"
TOKEN="$(ultimo | sed -n 's/.*"token":"\([0-9a-f]\{64\}\)".*/\1/p')"
if [ -z "$TOKEN" ]; then
    echo "No se obtuvo el token; se detienen las pruebas."
    exit 1
fi

echo "== ECUD8 Validar token"
probar "Sin encabezado Authorization: 401"           GET /Paises 401 ERROR "" "" "Token inválido o sesión no activa"
probar "Token mal formado: 401"                      GET /Paises 401 ERROR "" "abc"
probar "Token con formato válido no registrado: 401" GET /Paises 401 ERROR "" "$(printf 'a%.0s' {1..64})"
probar "Prefijo Bearer aceptado: 200"                GET /Paises 200 OK "" "Bearer $TOKEN"

echo "== ECUD10 Respuesta estructurada ante errores del protocolo"
probar "Ruta inexistente: 404"                       GET /NoExiste 404 ERROR "" "$TOKEN"
probar "Método no permitido: 405"                    DELETE /Paises 405 ERROR "" "$TOKEN"

echo "== ECUD4 Listar / ECUD5 Buscar / ECUD6 Filtrar"
probar "Listar países: 200"                          GET /Paises 200 OK "" "$TOKEN"
probar "Buscar sin criterios: 200 (como listar)"     GET /Paises/buscar 200 OK "" "$TOKEN" "Búsqueda realizada correctamente"
probar "Filtrar por región con tilde: 200"           GET "/Paises/buscar?region=Am%C3%A9rica" 200 OK "" "$TOKEN" "Filtro aplicado correctamente"
probar "Filtrar por región sin tilde: 200"           GET "/Paises/buscar?region=africa" 200 OK "" "$TOKEN" "\"total\":"
probar "Buscar por nombre y región: 200"             GET "/Paises/buscar?nombre=zz&region=Europa" 200 OK "" "$TOKEN" "Búsqueda realizada correctamente"
probar "Región no válida: 400"                       GET "/Paises/buscar?region=Marte" 400 ERROR "" "$TOKEN" "Región no válida"

echo "== ECUD3 Consultar en la API: validaciones"
LARGO="$(printf 'x%.0s' {1..101})"
probar "Nombre de más de 100 caracteres: 400"        GET "/Paises/externo/$LARGO" 400 ERROR "" "$TOKEN" "no puede superar 100"
probar "Importar sin nombre: 400"                    POST /Paises/importar 400 ERROR '{"consulta":"ecuador"}' "$TOKEN" "Ingrese el nombre del país"
probar "Actualizar país inexistente: 404"            PUT "/Paises/ZZ%20No%20Existe" 404 ERROR "" "$TOKEN" "El país no existe"
probar "Eliminar país inexistente: 404"              DELETE "/Paises/ZZ%20No%20Existe" 404 ERROR "" "$TOKEN" "El país no existe"

if [ "$API" = "si" ]; then
    echo "== ECUD3 / ECUD2 / ECUD4 con la API REST Countries"
    probar "Consultar sin coincidencias: 404"        GET /Paises/externo/zzzzqq 404 ERROR "" "$TOKEN" "No se encontraron países con ese nombre en la API"
    probar "Consultar ecuador: 200"                  GET /Paises/externo/ecuador 200 OK "" "$TOKEN" "\"nombre\":\"Ecuador\""
    probar "Importar Ecuador: 200"                   POST /Paises/importar 200 OK '{"consulta":"ecuador","nombre":"Ecuador"}' "$TOKEN" "País importado correctamente"
    probar "Consultar de nuevo: guardado=true"       GET /Paises/externo/ecuador 200 OK "" "$TOKEN" "\"guardado\":true"
    probar "Importar otra vez: actualizado"          POST /Paises/importar 200 OK '{"consulta":"ecuador","nombre":"Ecuador"}' "$TOKEN" "\"accion\":\"actualizado\""
    probar "Importar país que no vino en la consulta: 404" POST /Paises/importar 404 ERROR '{"consulta":"ecuador","nombre":"Narnia"}' "$TOKEN"
    probar "Registrar Alemania (consulta germany): 201" POST /Paises 201 OK '{"consulta":"germany","nombre":"Alemania"}' "$TOKEN" "País registrado correctamente"
    probar "Registrar duplicado: 409"                POST /Paises 409 ERROR '{"consulta":"germany","nombre":"Alemania"}' "$TOKEN" "Ya existe un país con ese nombre"
    probar "Actualizar Alemania (nombre en español): 200" PUT /Paises/Alemania 200 OK "" "$TOKEN" "País actualizado correctamente"
    probar "Buscar por capital parcial (berl): 200"  GET "/Paises/buscar?capital=berl" 200 OK "" "$TOKEN" "\"total\":1"
    probar "Eliminar Alemania: 200"                  DELETE /Paises/Alemania 200 OK "" "$TOKEN" "País eliminado correctamente"
    probar "Eliminar de nuevo: 404"                  DELETE /Paises/Alemania 404 ERROR "" "$TOKEN"
    probar "Importar todos: 200 con resumen"         POST /Paises/importar/todos 200 OK "" "$TOKEN" "\"recibidos\":"

    echo "== ECUD7 Informes"
    probar "Informes: 200 con las tres secciones"    GET /Reportes 200 OK "" "$TOKEN" "\"paisMasPoblado\":"
else
    echo "== Sin API key: las operaciones con REST Countries responden 502 (ECUD9, alterno 1)"
    probar "Consultar en la API: 502"                GET /Paises/externo/ecuador 502 ERROR "" "$TOKEN" "La API REST Countries no está disponible"
    probar "Importar: 502"                           POST /Paises/importar 502 ERROR '{"consulta":"ecuador","nombre":"Ecuador"}' "$TOKEN"
    probar "Importar todos: 502"                     POST /Paises/importar/todos 502 ERROR "" "$TOKEN"
    probar "Registrar: 502"                          POST /Paises 502 ERROR '{"consulta":"ecuador","nombre":"Ecuador"}' "$TOKEN"

    echo "== ECUD7 Informes"
    # Con la base vacía: estado ERROR y HTTP 200 (diagrama de secuencia); con países: OK
    probar "Informes: 200"                           GET /Reportes 200 "*" "" "$TOKEN"
fi

echo "== ECUD1 Cerrar sesión"
probar "Cerrar sesión: 200"                          POST /Login/logout 200 OK "" "$TOKEN" "Sesión cerrada"
probar "Token cerrado ya no sirve: 401"              GET /Paises 401 ERROR "" "$TOKEN"
probar "Cerrar sesión otra vez: 401"                 POST /Login/logout 401 ERROR "" "$TOKEN"

echo
echo "Resultado: $pasaron pasaron, $fallaron fallaron"
[ "$fallaron" = 0 ]
