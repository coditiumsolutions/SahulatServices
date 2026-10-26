// Provider document upload: pick an image, crop it to the slot's ratio, downscale it and put the result
// back into the slot's file input. The server re-checks type and size, so this is a convenience only.
(function () {
    var form = document.getElementById('documentsForm');
    var modalEl = document.getElementById('cropModal');
    if (!form || !modalEl || typeof Cropper === 'undefined') return;

    var modal = new bootstrap.Modal(modalEl);
    var cropImage = document.getElementById('cropImage');
    var cropTitle = document.getElementById('cropModalTitle');
    var cropUse = document.getElementById('cropUse');
    var cropper = null;
    var active = null; // { slot, input, file }
    var MAX_BYTES = 5 * 1024 * 1024;

    function slotParts(slot) {
        return {
            slot: slot,
            input: slot.querySelector('input[type=file]'),
            preview: slot.querySelector('.doc-slot-preview'),
            empty: slot.querySelector('.doc-slot-empty'),
            clear: slot.querySelector('.doc-slot-clear'),
            label: slot.getAttribute('data-title') || 'Image',
            ratio: parseFloat(slot.getAttribute('data-ratio') || '0') || NaN,
            maxSide: parseInt(slot.getAttribute('data-maxside') || '1600', 10),
            quality: parseFloat(slot.getAttribute('data-quality') || '0.85')
        };
    }

    function showError(msg) {
        var box = document.getElementById('documentsClientError');
        if (!box) return;
        box.textContent = msg || '';
        box.classList.toggle('d-none', !msg);
    }

    function refreshSubmit() {
        var btn = document.getElementById('documentsSubmit');
        var hint = document.getElementById('documentsHint');
        var onboarding = form.getAttribute('data-onboarding') === 'true';
        var parts = Array.prototype.slice.call(form.querySelectorAll('.doc-slot')).map(slotParts);
        var changed = parts.some(function (p) { return p.input && p.input.files && p.input.files.length; });
        var requiredReady = parts.every(function (p) {
            if (p.slot.getAttribute('data-required') !== 'true') return true;
            return (p.input && p.input.files && p.input.files.length) || p.slot.getAttribute('data-has-existing') === 'true';
        });
        var ok = requiredReady && (onboarding ? true : changed);
        btn.disabled = !ok;
        if (hint) {
            hint.textContent = !requiredReady
                ? 'Profile photo, CNIC front and CNIC back are all required.'
                : (!onboarding && !changed ? 'Replace a photo above to save changes.' : '');
        }
    }

    function setPreview(p, url) {
        if (url) {
            p.preview.src = url;
            p.preview.classList.remove('d-none');
            p.empty.classList.add('d-none');
        } else {
            var original = p.slot.getAttribute('data-existing-url');
            if (original) {
                p.preview.src = original;
                p.preview.classList.remove('d-none');
                p.empty.classList.add('d-none');
            } else {
                p.preview.removeAttribute('src');
                p.preview.classList.add('d-none');
                p.empty.classList.remove('d-none');
            }
        }
        p.clear.classList.toggle('d-none', !(p.input.files && p.input.files.length));
    }

    function clearSlot(p) {
        p.input.value = '';
        setPreview(p, null);
        refreshSubmit();
    }

    form.querySelectorAll('.doc-slot').forEach(function (slot) {
        var p = slotParts(slot);
        if (!p.input) return; // locked slot
        p.input.addEventListener('change', function () {
            showError('');
            var file = p.input.files && p.input.files[0];
            if (!file) { clearSlot(p); return; }
            if (!/^image\/(jpeg|png)$/.test(file.type)) {
                showError('Only jpg, jpeg, and png images are allowed.');
                clearSlot(p);
                return;
            }
            active = { p: p, file: file };
            var url = URL.createObjectURL(file);
            cropTitle.textContent = 'Crop ' + p.label;
            cropImage.src = url;
            modal.show();
        });
        p.clear.addEventListener('click', function () { clearSlot(p); });
    });

    modalEl.addEventListener('shown.bs.modal', function () {
        if (cropper) cropper.destroy();
        var p = active.p;
        cropper = new Cropper(cropImage, {
            aspectRatio: isNaN(p.ratio) ? NaN : p.ratio,
            viewMode: 1,
            autoCropArea: 1,
            background: false
        });
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        if (cropper) { cropper.destroy(); cropper = null; }
        if (cropImage.src) { URL.revokeObjectURL(cropImage.src); cropImage.removeAttribute('src'); }
        // Dismissed without "Use image": drop the pick.
        if (active && !active.done) clearSlot(active.p);
        active = null;
    });

    cropUse.addEventListener('click', function () {
        if (!cropper || !active) return;
        var p = active.p;
        var canvas = cropper.getCroppedCanvas({
            maxWidth: p.maxSide,
            maxHeight: p.maxSide,
            imageSmoothingQuality: 'high',
            fillColor: '#fff'
        });
        if (!canvas) { showError('Could not process the selected image. Please try again.'); return; }
        canvas.toBlob(function (blob) {
            if (!blob) { showError('Could not process the selected image. Please try again.'); return; }
            if (blob.size > MAX_BYTES) { showError('File exceeds the maximum size of 5 MB.'); return; }
            var file = new File([blob], p.slot.getAttribute('data-filename') || 'image.jpg', { type: 'image/jpeg' });
            var dt = new DataTransfer();
            dt.items.add(file);
            p.input.files = dt.files;
            active.done = true;
            setPreview(p, URL.createObjectURL(blob));
            refreshSubmit();
            modal.hide();
        }, 'image/jpeg', p.quality);
    });

    form.addEventListener('submit', function (e) {
        var btn = document.getElementById('documentsSubmit');
        var verified = form.getAttribute('data-verified') === 'true';
        var cnicChanged = ['cnicFront', 'cnicBack'].some(function (n) {
            var el = form.querySelector('input[name=' + n + ']');
            return el && el.files && el.files.length;
        });
        if (verified && cnicChanged) {
            if (!window.confirm('Replacing your CNIC will send your account back for review. You will not be able to use the provider dashboard until it is verified again. Continue?')) {
                e.preventDefault();
                return;
            }
            document.getElementById('confirmCnicReset').value = 'true';
        }
        btn.disabled = true;
        btn.querySelector('.doc-submit-label').textContent = 'Uploading...';
        var bar = document.getElementById('documentsUploading');
        if (bar) bar.classList.remove('d-none');
    });

    refreshSubmit();
})();
