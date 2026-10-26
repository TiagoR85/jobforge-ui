initTheme();
initAccent();

document.addEventListener('DOMContentLoaded', function () {
    var dismiss = document.querySelector('#blazor-error-ui .dismiss');
    if (dismiss) {
        dismiss.addEventListener('click', function () {
            document.getElementById('blazor-error-ui').style.display = 'none';
        });
    }
});
