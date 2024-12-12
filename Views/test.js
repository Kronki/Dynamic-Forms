window.addEventListener(
    "message",
    function (event) {
        const allowedOrigins = [
            "https://aicoaches.live",
            "https://myagencycoach.agency",
            "http://aicoach.local.com",
            "https://" + window.location.hostname,
        ];

        if (!allowedOrigins.includes(event.origin)) {
            return;
        }
        console.log("test1", event);
        // Check if the message contains chat data
        if (event.data && typeof event.data === "object" && event.data.type === "chat_response") {
            // Save chat data to your backend
            console.log("test2", event);
            const chatData = {
                userMessage: event.data.userMessage,
                botResponse: event.data.botResponse,
                timestamp: new Date(),
            };

        }

        if (event.data === "removeIframe") {
            removeIframe();
        }
        if (event.data === "openPopup") {
            openPopup();
        }
        if (event.data === "closePopup") {
            closePopup();
        }
    },
    false
);