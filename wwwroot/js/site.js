
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

    $('.favorite-toggle').on('click', function(e) {
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
})