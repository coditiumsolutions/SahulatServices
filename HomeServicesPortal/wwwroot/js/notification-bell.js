// Navbar notification bell: shows the unread count and re-checks it every 60 seconds
// (the website has no push). The count URL depends on the signed-in role and is on the bell link.
(function () {
    var bell = document.querySelector('.hs-bell[data-count-url]');
    if (!bell) return;
    var url = bell.getAttribute('data-count-url');
    var badges = document.querySelectorAll('.js-bell-badge');

    function show(n) {
        var text = n > 99 ? '99+' : String(n);
        badges.forEach(function (b) {
            b.textContent = text;
            b.classList.toggle('d-none', !(n > 0));
        });
        bell.setAttribute('aria-label', n > 0 ? 'Notifications (' + text + ' unread)' : 'Notifications');
    }

    function refresh() {
        if (document.hidden) return;
        fetch(url, { credentials: 'same-origin', headers: { 'X-Requested-With': 'fetch' } })
            .then(function (r) { return r.ok ? r.json() : null; })
            .then(function (d) { if (d && typeof d.unread === 'number') show(d.unread); })
            .catch(function () { /* try again next tick */ });
    }

    refresh();
    setInterval(refresh, 60000);
    document.addEventListener('visibilitychange', function () { if (!document.hidden) refresh(); });
})();
