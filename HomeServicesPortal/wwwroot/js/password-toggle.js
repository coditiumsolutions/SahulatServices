// Adds a show/hide button to every password field on the page.
// Fields already inside an .input-group (with their own toggle) are left alone.
(function () {
    function enhance(input) {
        if (input.dataset.pwToggle || (input.parentElement && input.parentElement.classList.contains('input-group'))) {
            return;
        }
        input.dataset.pwToggle = '1';

        var group = document.createElement('div');
        group.className = 'input-group' + (input.classList.contains('form-control-lg') ? ' input-group-lg' : '');
        input.parentNode.insertBefore(group, input);
        group.appendChild(input);

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'btn btn-outline-secondary';
        btn.setAttribute('aria-label', 'Show password');
        btn.setAttribute('aria-pressed', 'false');
        btn.innerHTML = '<i class="fa-solid fa-eye" aria-hidden="true"></i>';
        btn.addEventListener('click', function () {
            var show = input.type === 'password';
            input.type = show ? 'text' : 'password';
            btn.setAttribute('aria-label', show ? 'Hide password' : 'Show password');
            btn.setAttribute('aria-pressed', show ? 'true' : 'false');
            btn.firstChild.className = show ? 'fa-solid fa-eye-slash' : 'fa-solid fa-eye';
        });
        group.appendChild(btn);

        // Never leave a revealed password in a page that gets restored from the back/forward cache.
        var form = input.form;
        if (form) {
            form.addEventListener('submit', function () { input.type = 'password'; });
        }
    }

    function run() {
        document.querySelectorAll('input[type="password"]').forEach(enhance);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', run);
    } else {
        run();
    }
    // Modals and late-rendered fields.
    document.addEventListener('shown.bs.modal', run);
})();
