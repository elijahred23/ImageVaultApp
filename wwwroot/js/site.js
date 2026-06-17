
$(function(){
    let currentImageIndex = 0;

    let $galleryItems = [];
    let lightboxZoom = 1;
    let lightboxFitSize = { width: 0, height: 0 };
    let lightboxPan = { x: 0, y: 0 };

    function clampZoom(value) {
        return Math.min(4, Math.max(0.25, value));
    }

    function calculateLightboxFitSize() {
        const image = document.getElementById('lightboxImage');
        const stage = document.getElementById('lightboxStage');

        if(!image || !stage || !image.naturalWidth || !image.naturalHeight) return;

        const stageWidth = Math.max(stage.clientWidth - 32, 1);
        const stageHeight = Math.max(stage.clientHeight - 32, 1);
        const fitRatio = Math.min(stageWidth / image.naturalWidth, stageHeight / image.naturalHeight, 1);

        lightboxFitSize = {
            width: Math.round(image.naturalWidth * fitRatio),
            height: Math.round(image.naturalHeight * fitRatio)
        };

        applyLightboxZoom();
    }

    function applyLightboxZoom() {
        const image = document.getElementById('lightboxImage');

        if(!image || !lightboxFitSize.width || !lightboxFitSize.height) return;

        const imageWidth = Math.round(lightboxFitSize.width * lightboxZoom);
        const imageHeight = Math.round(lightboxFitSize.height * lightboxZoom);

        image.style.setProperty('--lightbox-image-width', `${imageWidth}px`);
        image.style.setProperty('--lightbox-image-height', `${imageHeight}px`);

        $('#lightboxZoomSlider').val(Math.round(lightboxZoom * 100));
        $('#lightboxZoomLabel').text(`${Math.round(lightboxZoom * 100)}%`);
        clampLightboxPan();
        applyLightboxPan();
    }

    function setLightboxZoom(value) {
        lightboxZoom = clampZoom(value);
        applyLightboxZoom();
    }

    function getLightboxPanBounds() {
        const stage = document.getElementById('lightboxStage');

        if(!stage || !lightboxFitSize.width || !lightboxFitSize.height) {
            return { x: 0, y: 0 };
        }

        const imageWidth = lightboxFitSize.width * lightboxZoom;
        const imageHeight = lightboxFitSize.height * lightboxZoom;
        const stageWidth = Math.max(stage.clientWidth - 32, 1);
        const stageHeight = Math.max(stage.clientHeight - 32, 1);

        return {
            x: Math.max((imageWidth - stageWidth) / 2, 0),
            y: Math.max((imageHeight - stageHeight) / 2, 0)
        };
    }

    function clampLightboxPan() {
        const bounds = getLightboxPanBounds();

        lightboxPan = {
            x: Math.min(bounds.x, Math.max(-bounds.x, lightboxPan.x)),
            y: Math.min(bounds.y, Math.max(-bounds.y, lightboxPan.y))
        };
    }

    function applyLightboxPan() {
        const image = document.getElementById('lightboxImage');

        if(!image) return;

        image.style.setProperty('--lightbox-pan-x', `${Math.round(lightboxPan.x)}px`);
        image.style.setProperty('--lightbox-pan-y', `${Math.round(lightboxPan.y)}px`);
    }

    function moveLightboxPan(deltaX, deltaY) {
        lightboxPan = {
            x: lightboxPan.x + deltaX,
            y: lightboxPan.y + deltaY
        };

        clampLightboxPan();
        applyLightboxPan();
    }

    function resetLightboxView() {
        lightboxPan = { x: 0, y: 0 };
        setLightboxZoom(1);
        applyLightboxPan();
    }

    function updateLightbox(index) 
    {
        if(index < 0 || index >= $galleryItems.length) return;


        currentImageIndex = index;

        const $item = $($galleryItems[currentImageIndex])

        $('#lightboxImage').attr('src', $item.attr('src'));
        $('#lightboxCaption').text($item.data('title') || 'No Title');
        resetLightboxView();
    }


    $('body').on('click', '.gallery-img', function(e) {
        e.preventDefault();

        $galleryItems = $('.gallery-img').toArray();

        currentImageIndex = $galleryItems.indexOf(this);

        updateLightbox(currentImageIndex);

        const modal = new bootstrap.Modal(document.getElementById('lightboxModal'));
        modal.show();
    });

    $('#prevImage').on('click', function(){
        updateLightbox((currentImageIndex - 1 + $galleryItems.length) % $galleryItems.length);
    });
    $('#nextImage').on('click', function(){
        updateLightbox((currentImageIndex + 1) % $galleryItems.length);
    });

    $('#zoomOutImage').on('click', function(){
        setLightboxZoom(lightboxZoom - 0.25);
    });

    $('#zoomInImage').on('click', function(){
        setLightboxZoom(lightboxZoom + 0.25);
    });

    $('#resetImageZoom').on('click', function(){
        resetLightboxView();
    });

    $('#lightboxZoomSlider').on('input', function(){
        setLightboxZoom(Number(this.value) / 100);
    });

    $('#lightboxImage').on('load', function() {
        calculateLightboxFitSize();
    });

    $('#lightboxStage').on('wheel', function(e) {
        if(!$('#lightboxModal').hasClass('show')) return;

        e.preventDefault();
        setLightboxZoom(lightboxZoom + (e.originalEvent.deltaY < 0 ? 0.25 : -0.25));
    });

    $('#panImageLeft').on('click', function() {
        moveLightboxPan(80, 0);
    });

    $('#panImageRight').on('click', function() {
        moveLightboxPan(-80, 0);
    });

    $('#panImageUp').on('click', function() {
        moveLightboxPan(0, 80);
    });

    $('#panImageDown').on('click', function() {
        moveLightboxPan(0, -80);
    });

    $('#lightboxModal').on('shown.bs.modal', function() {
        calculateLightboxFitSize();
    });

    $(window).on('resize', function() {
        if($('#lightboxModal').hasClass('show')) {
            calculateLightboxFitSize();
        }
    });

    $(document).on('keydown', function(e) {
        if(!$('#lightboxModal').hasClass('show')) return;

        if(e.key == 'ArrowLeft') {
            if(lightboxZoom > 1) moveLightboxPan(80, 0);
            else $('#prevImage').trigger('click');
        }
        else if (e.key == 'ArrowRight') {
            if(lightboxZoom > 1) moveLightboxPan(-80, 0);
            else $('#nextImage').trigger('click');
        }
        else if (e.key == 'ArrowUp' && lightboxZoom > 1) {
            e.preventDefault();
            moveLightboxPan(0, 80);
        }
        else if (e.key == 'ArrowDown' && lightboxZoom > 1) {
            e.preventDefault();
            moveLightboxPan(0, -80);
        }
        else if (e.key == '+' || e.key == '=') $('#zoomInImage').trigger('click');
        else if (e.key == '-' || e.key == '_') $('#zoomOutImage').trigger('click');
        else if (e.key == '0') $('#resetImageZoom').trigger('click');
    })

    $('body').on('click', '.favorite-toggle', function(e) {
        e.preventDefault();

        const $btn = $(this);

        const imageId = $btn.data('image-id');

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Image/ToggleFavorite',
            type: "POST",
            data: {
                imageId: imageId, // Renamed from id
                __RequestVerificationToken: token
             },
            headers: {
                "RequestVerificationToken": token
            },
            success: function(response) {
                if(response.success){
                    if(response.isFavorited){
                        $btn.removeClass('btn-outline-danger').addClass('btn-danger');
                    } else {
                        $btn.removeClass("btn-danger").addClass("btn-outline-danger");
                    }

                    $btn.find('.favorite-icon').text(response.isFavorited ? "❤️" : "🤍");
                }
            },
            error: function() {
                alert("An error occurred while toggling favorite status. Please try again.");
            }
        })

    })

    $('body').on('click', '.edit-image-button', function() {
        const imageId = $(this).data('image-id');
        const $item = $(`.gallery-item[data-image-id="${imageId}"]`);
        const imageUrl = $item.find('.gallery-img').attr('src');

        $('#editImageForm')[0].reset();
        $('#editImageError').addClass('d-none').text('');
        $('#editImageId').val(imageId);
        $('#editImageTitle').val($item.data('title') || '');
        $('#editImageDescription').val($item.data('description') || '');
        $('#editImageIsNSFW').prop('checked', $item.data('is-nsfw') === true || $item.data('is-nsfw') === 'true');
        $('#editCurrentImage').attr('src', imageUrl);
        $('#editNewImagePreview').attr('src', '#').addClass('d-none');

        const modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('editImageModal'));
        modal.show();
    });

    $('#editImageFile').on('change', function() {
        const file = this.files[0];
        const $preview = $('#editNewImagePreview');

        if(!file) {
            $preview.attr('src', '#').addClass('d-none');
            return;
        }

        const reader = new FileReader();

        reader.onload = function(e) {
            $preview.attr('src', e.target.result).removeClass('d-none');
        };

        reader.readAsDataURL(file);
    });

    $('#editImageForm').on('submit', function(e) {
        e.preventDefault();

        const form = this;
        const $form = $(form);
        const $submit = $('#editImageSubmit');
        const $error = $('#editImageError');
        const token = $form.find('input[name="__RequestVerificationToken"]').val();
        const formData = new FormData(form);

        $submit.prop('disabled', true).text('Saving...');
        $error.addClass('d-none').text('');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false,
            headers: {
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                if(!response.success) {
                    $error.text(response.message || 'Unable to save changes. Please try again.').removeClass('d-none');
                    return;
                }

                const image = response.image;
                const isNSFW = image.isNSFW === true || image.isNsfw === true;
                const $item = $(`.gallery-item[data-image-id="${image.id}"]`);
                const allowNSFW = $('#galleryGrid').data('allow-nsfw') === true || $('#galleryGrid').data('allow-nsfw') === 'true';
                const blurNSFW = $('#galleryGrid').data('blur-nsfw') === true || $('#galleryGrid').data('blur-nsfw') === 'true';

                $item
                    .data('title', image.title || '')
                    .data('description', image.description || '')
                    .data('is-nsfw', isNSFW);

                $item.attr({
                    'data-title': image.title || '',
                    'data-description': image.description || '',
                    'data-is-nsfw': isNSFW.toString()
                });

                const $galleryImage = $item.find('.gallery-img');

                $galleryImage
                    .attr('src', image.imageUrl || '')
                    .attr('alt', image.title || '')
                    .attr('data-title', image.title || '')
                    .data('title', image.title || '')
                    .toggleClass('nsfw-blur-effect', isNSFW && blurNSFW);

                $item.find('.card-title').text(image.title || '');
                $item.find('.card-text.text-truncate').text(image.description || '');

                bootstrap.Modal.getInstance(document.getElementById('editImageModal')).hide();

                if(isNSFW && !allowNSFW) {
                    $item.fadeOut(150, function() {
                        $(this).remove();

                        const remainingCount = $('.gallery-item').length;
                        $('#galleryImageCount').text(remainingCount);

                        if(remainingCount === 0) {
                            $('#galleryGrid').replaceWith(
                                '<div id="emptyGalleryMessage" class="text-center py-5 border-rounded-4 bg-light shadow-sm">' +
                                    '<p class="text-muted mb-0">No images found in your vault.</p>' +
                                '</div>'
                            );
                        }
                    });
                }
            },
            error: function(xhr) {
                const message = xhr.responseJSON && xhr.responseJSON.message
                    ? xhr.responseJSON.message
                    : 'An error occurred while saving changes. Please try again.';

                $error.text(message).removeClass('d-none');
            },
            complete: function() {
                $submit.prop('disabled', false).text('Save Changes');
            }
        });
    });

    $('body').on('submit', '.delete-image-form', function(e) {
        e.preventDefault();

        const $form = $(this);
        const $button = $form.find('button[type="submit"]');
        const $galleryItem = $form.closest('.gallery-item');
        const token = $form.find('input[name="__RequestVerificationToken"]').val()
            || $('input[name="__RequestVerificationToken"]').val();

        $button.prop('disabled', true).text('Deleting...');

        $.ajax({
            url: $form.attr('action'),
            type: 'POST',
            data: $form.serialize(),
            headers: {
                'RequestVerificationToken': token,
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function(response) {
                if(!response.success) {
                    $button.prop('disabled', false).text('Delete');
                    alert(response.message || 'Unable to delete image. Please try again.');
                    return;
                }

                $galleryItem.fadeOut(150, function() {
                    $(this).remove();

                    const $count = $('#galleryImageCount');
                    const remainingCount = $('.gallery-item').length;

                    $count.text(remainingCount);

                    if(remainingCount === 0) {
                        $('#galleryGrid').replaceWith(
                            '<div id="emptyGalleryMessage" class="text-center py-5 border-rounded-4 bg-light shadow-sm">' +
                                '<p class="text-muted mb-0">No images found in your vault.</p>' +
                            '</div>'
                        );
                    }
                });
            },
            error: function() {
                $button.prop('disabled', false).text('Delete');
                alert('An error occurred while deleting the image. Please try again.');
            }
        });
    });
})
