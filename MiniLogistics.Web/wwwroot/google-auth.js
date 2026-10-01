window.googleAuth = {
    clientId: null,
    dotNetRef: null,
    initialized: false,

    initialize: function (clientId, dotNetRef) {
        this.clientId = clientId;
        this.dotNetRef = dotNetRef;

        if (!window.google || !window.google.accounts || !window.google.accounts.id) {
            console.error("Google Identity Services chưa được load.");
            return false;
        }

        google.accounts.id.initialize({
            client_id: clientId,
            callback: (response) => {
                if (!response || !response.credential) {
                    if (this.dotNetRef) {
                        this.dotNetRef.invokeMethodAsync(
                            "OnGoogleLoginError",
                            "Google không trả về token hợp lệ.");
                    }
                    return;
                }

                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync(
                        "OnGoogleLogin",
                        response.credential);
                }
            },
            auto_select: false,
            cancel_on_tap_outside: true
        });

        this.initialized = true;

        const buttonContainer = document.getElementById("google-login-button");
        if (buttonContainer) {
            buttonContainer.innerHTML = "";
            google.accounts.id.renderButton(buttonContainer, {
                theme: "outline",
                size: "large",
                width: 350,
                text: "continue_with",
                shape: "rectangular"
            });
        }

        return true;
    },

    prompt: function () {
        if (!this.initialized) {
            return false;
        }

        google.accounts.id.prompt((notification) => {
            if (!notification) {
                return;
            }

            if (notification.isNotDisplayed && notification.isNotDisplayed()) {
                const reason = notification.getNotDisplayedReason
                    ? notification.getNotDisplayedReason()
                    : "not_displayed";

                if (this.dotNetRef && reason !== "suppressed_by_user") {
                    this.dotNetRef.invokeMethodAsync(
                        "OnGoogleLoginError",
                        "Không thể hiển thị Google Login. Vui lòng dùng nút Google bên dưới hoặc thử lại.");
                }
            }

            if (notification.isSkippedMoment && notification.isSkippedMoment()) {
                // User dismissed / skipped — silent.
            }

            if (notification.isDismissedMoment && notification.isDismissedMoment()) {
                // User cancelled — silent.
            }
        });

        return true;
    }
};
