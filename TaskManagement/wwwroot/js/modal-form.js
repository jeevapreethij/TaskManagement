/*
 * Reusable AJAX modal form.
 *
 * Trigger : <a href="/X/Create" data-modal-target="#appModal">New</a>   (href or data-modal-url is loaded)
 * Dialog  : <partial name="_ModalDialog" model="..." />
 * Server  : GET  -> returns a partial containing <form data-modal-form>
 *           POST -> JSON { success: true, redirectUrl?: string }, or the partial again (200) with validation errors,
 *                   or a non-2xx JSON { error, errors?, correlationId? } shown as a safe alert.
 * Events  : 'modalform:success' is fired on the dialog element (detail = server response).
 *           Call event.preventDefault() to skip the default reload/redirect.
 * Safety  : 15s timeout, network/timeout/4xx/5xx messages, duplicate-submit guard, no raw server text shown.
 */
(function () {
    'use strict';

    var AJAX = { 'X-Requested-With': 'XMLHttpRequest' };
    var TIMEOUT_MS = 15000;

    function request(url, options) {
        var controller = new AbortController();
        var timer = setTimeout(function () { controller.abort(); }, TIMEOUT_MS);
        options = options || {};
        options.headers = AJAX;
        options.signal = controller.signal;
        return fetch(url, options).finally(function () { clearTimeout(timer); });
    }

    function readBody(response) {
        var isJson = (response.headers.get('Content-Type') || '').indexOf('json') >= 0;
        if (isJson) {
            return response.json().catch(function () { return {}; });
        }
        return response.text();
    }

    function messageFor(status, data) { return AppAjax.messageFor(status, data); }

    function messageForException(err) {
        return AppAjax.messageForException(err && err.name === 'AbortError');
    }

    function init(dialogEl) {
        if (dialogEl._modalForm) { return dialogEl._modalForm; }

        var modal = new bootstrap.Modal(dialogEl);
        var body = dialogEl.querySelector('[data-modal-body]');
        var save = dialogEl.querySelector('[data-modal-save]');
        var form = null;
        var busy = false;

        function setSaveEnabled(enabled) { if (save) { save.disabled = !enabled; } }

        function showError(text) {
            var el = body.querySelector('[data-modal-error]');
            if (!el) {
                el = document.createElement('div');
                el.className = 'alert alert-danger';
                el.setAttribute('role', 'alert');
                el.setAttribute('data-modal-error', '');
                body.prepend(el);
            }
            el.textContent = text; // textContent: never render server text as HTML
        }

        function bind() {
            form = body.querySelector('form[data-modal-form]');
            setSaveEnabled(!!form);
            if (!form) { return; }
            if (window.jQuery && jQuery.validator && jQuery.validator.unobtrusive) {
                jQuery.validator.unobtrusive.parse(form);
            }
            form.addEventListener('submit', submit);
        }

        function submit(e) {
            e.preventDefault();
            if (busy) { return; }
            if (window.jQuery && jQuery.fn.valid && !jQuery(form).valid()) { return; }

            busy = true;
            setSaveEnabled(false);

            request(form.action, { method: 'POST', body: new FormData(form) })
                .then(function (r) { return readBody(r).then(function (b) { return { ok: r.ok, status: r.status, body: b }; }); })
                .then(function (res) {
                    if (res.ok && res.body && res.body.success) {
                        var ev = new CustomEvent('modalform:success', { detail: res.body, cancelable: true });
                        if (dialogEl.dispatchEvent(ev)) {
                            if (res.body.redirectUrl) { window.location.href = res.body.redirectUrl; }
                            else { window.location.reload(); }
                        }
                        return;
                    }
                    if (res.ok && typeof res.body === 'string') {
                        body.innerHTML = res.body; // server-rendered partial (Razor-encoded) with validation messages
                        bind();
                        return;
                    }
                    showError(messageFor(res.status, res.body));
                })
                .catch(function (err) { showError(messageForException(err)); })
                .finally(function () { busy = false; if (form) { setSaveEnabled(true); } });
        }

        if (save) {
            save.addEventListener('click', function () { if (form) { form.requestSubmit(); } });
        }

        dialogEl.addEventListener('hidden.bs.modal', function () { body.innerHTML = ''; form = null; busy = false; });

        dialogEl._modalForm = {
            open: function (url) {
                body.innerHTML = '<div class="text-muted">Loading…</div>';
                setSaveEnabled(false);
                modal.show();
                return request(url)
                    .then(function (r) { return readBody(r).then(function (b) { return { ok: r.ok, status: r.status, body: b }; }); })
                    .then(function (res) {
                        if (res.ok && typeof res.body === 'string') {
                            body.innerHTML = res.body;
                            bind();
                            return;
                        }
                        body.innerHTML = '';
                        showError(messageFor(res.status, res.body));
                    })
                    .catch(function (err) { body.innerHTML = ''; showError(messageForException(err)); });
            }
        };
        return dialogEl._modalForm;
    }

    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-modal-target]');
        if (!trigger) { return; }
        var dialogEl = document.querySelector(trigger.getAttribute('data-modal-target'));
        if (!dialogEl) { return; }
        e.preventDefault();
        document.querySelectorAll('[data-modal-dialog].show').forEach(function (el) {
            var inst = bootstrap.Modal.getInstance(el);
            if (inst && el !== dialogEl) { inst.hide(); }
        });
        init(dialogEl).open(trigger.getAttribute('data-modal-url') || trigger.getAttribute('href'));
    });
})();
