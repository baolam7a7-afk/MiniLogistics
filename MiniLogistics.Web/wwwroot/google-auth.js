window.googleAuth = {

    dotNetRef: null,

    initialize: function (clientId, dotNetRef) {

        this.dotNetRef = dotNetRef;

        if (!window.google || !window.google.accounts) {

            console.error(
                "Google Identity Services chưa được load."
            );

            return;
        }

        google.accounts.id.initialize({

            client_id: clientId,

            callback: (response) => {

                console.log(
                    "Google ID Token đã nhận."
                );

                if (this.dotNetRef) {

                    this.dotNetRef.invokeMethodAsync(
                        "OnGoogleLogin",
                        response.credential
                    );

                }
            }
        });


        const buttonContainer =
            document.getElementById(
                "google-login-button"
            );


        if (!buttonContainer) {

            console.error(
                "Không tìm thấy #google-login-button"
            );

            return;
        }


        buttonContainer.innerHTML = "";


        google.accounts.id.renderButton(

            buttonContainer,

            {
                theme: "outline",
                size: "large",
                width: 350,
                text: "signin_with"
            }

        );
    }
};