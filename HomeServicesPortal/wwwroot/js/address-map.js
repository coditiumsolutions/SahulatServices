// Pin picker for the client address form: search a place, click the map or drag the pin, or use the
// browser location. The pin fills the Latitude/Longitude fields, and (only where they are still empty)
// the address fields from reverse geocoding. Uses the public /api/geocoding endpoints.
(function () {
    var mapEl = document.getElementById('addressMap');
    if (!mapEl || typeof L === 'undefined') return;

    var latInput = document.getElementById('Latitude');
    var lngInput = document.getElementById('Longitude');
    var searchInput = document.getElementById('addressMapSearch');
    var results = document.getElementById('addressMapResults');
    var status = document.getElementById('addressMapStatus');
    var fields = {
        road: document.getElementById('FullAddress'),
        area: document.getElementById('Area'),
        city: document.getElementById('City')
    };

    var map = L.map(mapEl).setView([30.3753, 69.3451], 5);
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    var marker = null;
    var reverseToken = 0;

    function say(text) { status.textContent = text; }

    function round(n) { return Math.round(n * 1000000) / 1000000; }

    function setCoords(lat, lng) {
        latInput.value = round(lat);
        lngInput.value = round(lng);
    }

    function placePin(lat, lng, zoom) {
        if (!marker) {
            marker = L.marker([lat, lng], { draggable: true }).addTo(map);
            marker.on('dragend', function () {
                var p = marker.getLatLng();
                setCoords(p.lat, p.lng);
                reverse(p.lat, p.lng);
            });
        } else {
            marker.setLatLng([lat, lng]);
        }
        if (zoom) map.setView([lat, lng], zoom);
    }

    function fill(input, value) {
        if (input && value && !input.value.trim()) input.value = value;
    }

    function reverse(lat, lng) {
        var token = ++reverseToken;
        say('Looking up the address…');
        fetch('/api/geocoding/reverse?lat=' + encodeURIComponent(lat) + '&lng=' + encodeURIComponent(lng))
            .then(function (r) { return r.json(); })
            .then(function (d) {
                if (token !== reverseToken) return;
                if (!d || !d.success || !d.data) { say('Pin set. We could not look up the address; type it in.'); return; }
                fill(fields.road, d.data.road || d.data.displayName);
                fill(fields.area, d.data.area);
                fill(fields.city, d.data.city);
                say('Pin set. Empty address fields were filled in; check them before saving.');
            })
            .catch(function () { if (token === reverseToken) say('Pin set. We could not look up the address; type it in.'); });
    }

    function pick(lat, lng) {
        placePin(lat, lng);
        setCoords(lat, lng);
        reverse(lat, lng);
    }

    map.on('click', function (e) { pick(e.latlng.lat, e.latlng.lng); });

    // Existing coordinates (edit form), or typed ones.
    function fromInputs(zoom) {
        var lat = parseFloat(latInput.value), lng = parseFloat(lngInput.value);
        if (isFinite(lat) && isFinite(lng) && !(lat === 0 && lng === 0)) { placePin(lat, lng, zoom); return true; }
        return false;
    }
    if (fromInputs(16)) say('Drag the pin or click the map to move it.');
    [latInput, lngInput].forEach(function (el) { el.addEventListener('change', function () { fromInputs(16); }); });

    // Search, debounced by 500 ms like the app.
    var timer = null;
    searchInput.addEventListener('input', function () {
        clearTimeout(timer);
        var q = searchInput.value.trim();
        if (q.length < 3) { results.innerHTML = ''; return; }
        timer = setTimeout(function () { search(q); }, 500);
    });
    searchInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); clearTimeout(timer); var q = searchInput.value.trim(); if (q) search(q); }
    });

    function search(q) {
        say('Searching…');
        fetch('/api/geocoding/search?limit=5&q=' + encodeURIComponent(q))
            .then(function (r) { return r.json(); })
            .then(function (d) {
                results.innerHTML = '';
                var list = d && d.success && d.data ? d.data : [];
                if (!list.length) { say('No places found. Try a nearby landmark, or click the map.'); return; }
                say('Choose a result, or click the map.');
                list.forEach(function (item) {
                    var li = document.createElement('li');
                    var btn = document.createElement('button');
                    btn.type = 'button';
                    btn.className = 'list-group-item list-group-item-action small';
                    btn.textContent = item.displayName;
                    btn.addEventListener('click', function () {
                        results.innerHTML = '';
                        searchInput.value = item.displayName;
                        placePin(item.latitude, item.longitude, 17);
                        setCoords(item.latitude, item.longitude);
                        fill(fields.road, item.road || item.displayName);
                        fill(fields.area, item.area);
                        fill(fields.city, item.city);
                        say('Pin set. Drag it to fine-tune, then check the address fields.');
                    });
                    li.appendChild(btn);
                    results.appendChild(li);
                });
            })
            .catch(function () { say('Search is unavailable right now. Click the map instead.'); });
    }

    document.getElementById('addressMapLocate').addEventListener('click', function () {
        if (!navigator.geolocation) { say('Your browser does not support location.'); return; }
        say('Finding your location…');
        navigator.geolocation.getCurrentPosition(function (pos) {
            placePin(pos.coords.latitude, pos.coords.longitude, 17);
            setCoords(pos.coords.latitude, pos.coords.longitude);
            reverse(pos.coords.latitude, pos.coords.longitude);
        }, function () { say('We could not get your location. Search or click the map instead.'); },
        { enableHighAccuracy: true, timeout: 10000 });
    });

    document.getElementById('addressMapClear').addEventListener('click', function () {
        if (marker) { map.removeLayer(marker); marker = null; }
        latInput.value = ''; lngInput.value = '';
        say('Pin removed. Saving without a pin is fine.');
    });
})();
