// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

$(function () {
    function showAlert(type, text) {
        $('#ajax-alert').removeClass('d-none alert-success alert-danger alert-warning').addClass('alert-' + type).text(text);
    }

    $(document).on('submit', 'form[data-ajax-form]', function (e) {
        e.preventDefault();
        var $form = $(this);
        var $button = $form.find('button[type=submit]');
        if ($form.data('busy')) { return; }

        $form.data('busy', true);
        $button.prop('disabled', true);

        $.ajax({
            url: $form.attr('action'),
            method: 'POST',
            data: $form.serialize(),
            timeout: 15000,
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        }).done(function (data) {
            showAlert('success', (data && data.message) || 'Saved.');
            setTimeout(function () { window.location.reload(); }, 600);
        }).fail(function (xhr, textStatus) {
            var data = xhr.responseJSON || {};
            var message = (textStatus === 'timeout' || xhr.status === 0)
                ? AppAjax.messageForException(textStatus === 'timeout')
                : AppAjax.messageFor(xhr.status, data);
            showAlert(xhr.status === 409 ? 'warning' : 'danger', message);
            $form.data('busy', false);
            $button.prop('disabled', false);
        });
    });
});
