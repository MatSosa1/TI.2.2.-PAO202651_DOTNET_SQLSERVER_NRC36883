/*
 * Mejoras opcionales (la aplicación funciona igual sin JavaScript):
 * 1. Indicador de progreso: al enviar un formulario con data-procesando se deshabilita su botón
 *    y se muestra «Procesando...» (ECUD: el cliente muestra un indicador mientras el servidor procesa).
 * 2. Filtro inmediato: un <select data-autoenviar> envía su formulario al cambiar (ECUD6: la región
 *    se aplica sin presionar «Buscar»).
 */
document.addEventListener('submit', function (evento) {
  var formulario = evento.target;
  if (!formulario.hasAttribute('data-procesando')) {
    return;
  }
  formulario.querySelectorAll('button[type="submit"]').forEach(function (boton) {
    // Se deshabilita después del envío para que el valor del botón pulsado viaje en la petición
    setTimeout(function () { boton.disabled = true; }, 0);
  });
  var indicador = formulario.querySelector('.procesando') || document.querySelector('.procesando');
  if (indicador) {
    indicador.classList.add('procesando--activo');
  }
});

document.querySelectorAll('select[data-autoenviar]').forEach(function (lista) {
  lista.addEventListener('change', function () {
    var formulario = lista.form;
    if (formulario.requestSubmit) {
      formulario.requestSubmit();
    } else {
      formulario.submit();
    }
  });
});
