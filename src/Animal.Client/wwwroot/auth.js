window.animalAuth = {
    async getAccessToken() {
        const response = await fetch("/auth/access-token", {
            credentials: "same-origin",
            headers: { "Accept": "application/json" }
        });
        if (!response.ok) return null;
        const result = await response.json();
        return result.accessToken ?? null;
    }
};
