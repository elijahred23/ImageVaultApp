
$(function() {
    $('.favorite-toggle').on('click', function(e) {
        const $btn = $(this);

        const imageId = $btn.data('id');

        const token = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: '/Image/ToggleFavorite/',
            type: "POST",
            data: {id: imageId },
            headers: {
                "RequestVerificationToken": token
            },
            success: function(response) {
                if(response.success){
                    $btn.toggleClass("btn-danger btn-outline-danger");
                    $btn.find('.star-icon').text(response.isFavorite ? "❤" : "♡");
                }
            },
            error: function() {
                alert("An error occurred while toggling favorite status. Please try again.");
            }
        })

    })
})