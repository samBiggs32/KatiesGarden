window.CartStorage = {
    get: () => localStorage.getItem('kg-cart') || '[]',
    set: (json) => localStorage.setItem('kg-cart', json),
    clear: () => localStorage.removeItem('kg-cart')
};

window.ClipboardHelper = {
    copy: (text) => navigator.clipboard.writeText(text)
};

// Convert a URL-safe base64 VAPID key to a Uint8Array for the push API
function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
}

window.PushManager = {
    isSupported: () => 'serviceWorker' in navigator && 'PushManager' in window,

    isSubscribed: async () => {
        if (!window.PushManager.isSupported()) return false;
        try {
            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.getSubscription();
            return sub !== null;
        } catch {
            return false;
        }
    },

    subscribe: async (vapidPublicKey) => {
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
        });
        return JSON.stringify(sub.toJSON());
    },

    unsubscribe: async () => {
        try {
            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.getSubscription();
            if (sub) await sub.unsubscribe();
        } catch {
            // Ignore errors on unsubscribe
        }
    }
};
