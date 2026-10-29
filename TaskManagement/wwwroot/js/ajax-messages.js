/*
 * Shared AJAX helpers used by site.js (jQuery) and modal-form.js (fetch).
 * One place for safe, user-facing error messages (never raw server text).
 */
window.AppAjax = (function () {
    'use strict';

    function messageFor(status, data) {
        data = (data && typeof data === 'object') ? data : {};
        var ref = data.correlationId ? ' Reference: ' + data.correlationId : '';
        if (status === 400 && data.errors) {
            return Object.keys(data.errors).map(function (k) { return data.errors[k]; }).join(' ');
        }
        if (status === 404) { return 'The item was not found.'; }
        if (status === 409) { return data.error || 'The item was changed by someone else. Please reload and try again.'; }
        if (status === 503) { return 'The service is temporarily unavailable. Please try again later.'; }
        return 'Something went wrong.' + ref;
    }

    function messageForException(timedOut) {
        return timedOut ? 'The request timed out. Please try again.' : 'Network error. Please check your connection.';
    }

    return { messageFor: messageFor, messageForException: messageForException };
})();
