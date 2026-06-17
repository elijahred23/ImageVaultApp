
$(function(){
    let currentImageIndex = 0;

    let $galleryItems = [];

    function updateLightbox(index) 
    {
        if(index < 0 || index >= $galleryItems.length) return;


        currentImageIndex = index;

        const $item = $($galleryItems[currentImageIndex])

        $('#lightboxImage').attr('src', $item.attr('src'));
        $('#lightboxCaption').text($item.data('title') || 'No Title');
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

    $(document).on('keydown', function(e) {
        if(!$('#lightboxModal').hasClass('show')) return;

        if(e.key == 'ArrowLeft') $('#prevImage').trigger('click');
        else if (e.key == 'ArrowRight') $('#nextImage').trigger('click');
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
