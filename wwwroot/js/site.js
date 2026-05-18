
$(function() {
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