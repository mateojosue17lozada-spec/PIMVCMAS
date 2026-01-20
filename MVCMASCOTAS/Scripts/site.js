// Scripts personalizados para MVCMASCOTAS

$(document).ready(function () {
    // Inicialización de tooltips de Bootstrap
    $('[data-toggle="tooltip"]').tooltip();

    // Inicialización de popovers
    $('[data-toggle="popover"]').popover();

    // Confirmación de eliminación
    $('.btn-delete').on('click', function (e) {
        if (!confirm('¿Está seguro de que desea eliminar este elemento?')) {
            e.preventDefault();
        }
    });

    // Auto-ocultar mensajes de alerta después de 5 segundos
    setTimeout(function () {
        $('.alert').fadeOut('slow');
    }, 5000);

    // Validación de formularios
    $('form').on('submit', function () {
        var $submitBtn = $(this).find('button[type="submit"]');
        $submitBtn.prop('disabled', true);
        $submitBtn.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Procesando...');
    });

    // Preview de imagen antes de subir
    $('input[type="file"]').on('change', function () {
        var input = this;
        if (input.files && input.files[0]) {
            var reader = new FileReader();
            reader.onload = function (e) {
                var preview = $(input).data('preview');
                if (preview) {
                    $(preview).attr('src', e.target.result).show();
                }
            };
            reader.readAsDataURL(input.files[0]);
        }
    });

    // Contador de caracteres en textareas
    $('textarea[maxlength]').each(function () {
        var $textarea = $(this);
        var maxLength = $textarea.attr('maxlength');
        var $counter = $('<small class="form-text text-muted"></small>');
        $textarea.after($counter);

        var updateCounter = function () {
            var remaining = maxLength - $textarea.val().length;
            $counter.text(remaining + ' caracteres restantes');
        };

        $textarea.on('input', updateCounter);
        updateCounter();
    });

    // Filtros de búsqueda en tiempo real
    $('#searchInput').on('keyup', function () {
        var value = $(this).val().toLowerCase();
        $('.searchable-item').filter(function () {
            $(this).toggle($(this).text().toLowerCase().indexOf(value) > -1);
        });
    });

    // Agregar al carrito con AJAX
    $('.btn-add-to-cart').on('click', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var productoId = $btn.data('producto-id');
        var cantidad = $btn.closest('.product-card').find('.cantidad-input').val() || 1;

        $.ajax({
            url: '/Tienda/AgregarAlCarrito',
            type: 'POST',
            data: {
                productoId: productoId,
                cantidad: cantidad
            },
            success: function (response) {
                if (response.success) {
                    alert(response.message);
                    // Actualizar contador del carrito si existe
                    var $cartCount = $('#cart-count');
                    if ($cartCount.length) {
                        var currentCount = parseInt($cartCount.text()) || 0;
                        $cartCount.text(currentCount + parseInt(cantidad));
                    }
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function () {
                alert('Error al agregar el producto al carrito');
            }
        });
    });

    // Inscripción a actividad con AJAX
    $('.btn-inscribir-actividad').on('click', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var actividadId = $btn.data('actividad-id');

        if (!confirm('¿Desea inscribirse en esta actividad?')) {
            return;
        }

        $.ajax({
            url: '/Voluntariado/InscribirseActividad',
            type: 'POST',
            data: {
                actividadId: actividadId,
                __RequestVerificationToken: $('input[name="__RequestVerificationToken"]').val()
            },
            success: function (response) {
                if (response.success) {
                    alert(response.message);
                    $btn.prop('disabled', true).text('Inscrito');
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function () {
                alert('Error al inscribirse en la actividad');
            }
        });
    });

    // Formato de moneda
    $('.currency').each(function () {
        var value = parseFloat($(this).text());
        if (!isNaN(value)) {
            $(this).text('$' + value.toFixed(2));
        }
    });

    // Validación de cédula ecuatoriana
    $('#Cedula').on('blur', function () {
        var cedula = $(this).val();
        if (cedula && !validarCedulaEcuatoriana(cedula)) {
            $(this).addClass('is-invalid');
            $(this).after('<div class="invalid-feedback">Cédula ecuatoriana inválida</div>');
        } else {
            $(this).removeClass('is-invalid');
            $(this).next('.invalid-feedback').remove();
        }
    });

    // Función de validación de cédula ecuatoriana
    function validarCedulaEcuatoriana(cedula) {
        if (cedula.length !== 10) return false;

        var digitos = cedula.split('').map(Number);
        var provincia = digitos[0] * 10 + digitos[1];

        if (provincia < 1 || provincia > 24) return false;

        var coeficientes = [2, 1, 2, 1, 2, 1, 2, 1, 2];
        var suma = 0;

        for (var i = 0; i < 9; i++) {
            var valor = digitos[i] * coeficientes[i];
            suma += valor > 9 ? valor - 9 : valor;
        }

        var digitoVerificador = suma % 10 === 0 ? 0 : 10 - (suma % 10);
        return digitoVerificador === digitos[9];
    }

    // Actualizar cantidad en carrito
    $('.cantidad-carrito').on('change', function () {
        var $input = $(this);
        var detalleId = $input.data('detalle-id');
        var nuevaCantidad = $input.val();

        $.ajax({
            url: '/Tienda/ActualizarCantidad',
            type: 'POST',
            data: {
                detalleId: detalleId,
                cantidad: nuevaCantidad
            },
            success: function (response) {
                if (response.success) {
                    // Actualizar subtotal
                    var $row = $input.closest('tr');
                    $row.find('.subtotal').text('$' + response.nuevoSubtotal.toFixed(2));
                    $('#total-carrito').text('$' + response.nuevoTotal.toFixed(2));
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function () {
                alert('Error al actualizar la cantidad');
            }
        });
    });

    // Eliminar del carrito
    $('.btn-remove-from-cart').on('click', function (e) {
        e.preventDefault();
        var $btn = $(this);
        var detalleId = $btn.data('detalle-id');

        if (!confirm('¿Desea eliminar este producto del carrito?')) {
            return;
        }

        $.ajax({
            url: '/Tienda/EliminarDelCarrito',
            type: 'POST',
            data: {
                detalleId: detalleId
            },
            success: function (response) {
                if (response.success) {
                    $btn.closest('tr').fadeOut(function () {
                        $(this).remove();
                        $('#total-carrito').text('$' + response.nuevoTotal.toFixed(2));
                    });
                } else {
                    alert('Error: ' + response.message);
                }
            },
            error: function () {
                alert('Error al eliminar el producto');
            }
        });
    });
});
