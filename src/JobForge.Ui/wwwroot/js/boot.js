initTheme();
initAccent();

// Blazor Server keeps the circuit on the server: a bfcache restore reconnects to a stale DOM
// and the render batches fail ("insertBefore"), so force a fresh document instead.
window.addEventListener('pageshow', function (e) {
    if (e.persisted) {
        location.reload();
    }
});

document.addEventListener('DOMContentLoaded', function () {
    var dismiss = document.querySelector('#blazor-error-ui .dismiss');
    if (dismiss) {
        dismiss.addEventListener('click', function () {
            document.getElementById('blazor-error-ui').style.display = 'none';
        });
    }
});
