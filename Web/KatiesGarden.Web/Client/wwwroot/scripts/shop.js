window.CartStorage = {
    get: () => localStorage.getItem('kg-cart') || '[]',
    set: (json) => localStorage.setItem('kg-cart', json),
    clear: () => localStorage.removeItem('kg-cart')
};

window.ClipboardHelper = {
    copy: (text) => navigator.clipboard.writeText(text)
};

window.clickElement = (id) => {
    const el = document.getElementById(id);
    if (el) el.click();
};

// Convert a URL-safe base64 VAPID key to a Uint8Array for the push API
function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    return Uint8Array.from([...raw].map(c => c.charCodeAt(0)));
}

// Capture native browser PushManager support BEFORE we define window.KgPush —
// using a distinct name avoids shadowing the native `window.PushManager` interface.
const KG_PUSH_NATIVE_SUPPORTED = 'serviceWorker' in navigator && 'PushManager' in window;

window.KgPush = {
    isSupported: () => KG_PUSH_NATIVE_SUPPORTED,

    isSubscribed: async () => {
        if (!KG_PUSH_NATIVE_SUPPORTED) return false;
        try {
            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.getSubscription();
            return sub !== null;
        } catch {
            return false;
        }
    },

    getEndpoint: async () => {
        if (!KG_PUSH_NATIVE_SUPPORTED) return null;
        try {
            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.getSubscription();
            return sub?.endpoint ?? null;
        } catch {
            return null;
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

window.KgMap = {
    init: function (elementId, lat, lng, radiusMetres) {
        if (!window._kgMapInstances) window._kgMapInstances = {};
        const existing = window._kgMapInstances[elementId];
        if (existing) {
            existing.remove();
            delete window._kgMapInstances[elementId];
        }

        const map = L.map(elementId, { zoomControl: true, scrollWheelZoom: false })
            .setView([lat, lng], 11);

        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 18,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        L.circle([lat, lng], {
            radius: radiusMetres,
            color: '#4A7C59',
            fillColor: '#4A7C59',
            fillOpacity: 0.12,
            weight: 2,
            dashArray: '6, 4'
        }).addTo(map);

        const icon = L.divIcon({
            className: '',
            html: '<div style="width:16px;height:16px;border-radius:50% 50% 50% 0;background:#4A7C59;transform:rotate(-45deg);border:3px solid white;box-shadow:0 2px 8px rgba(0,0,0,0.35)"></div>',
            iconSize: [16, 16],
            iconAnchor: [8, 16],
            popupAnchor: [0, -20]
        });

        L.marker([lat, lng], { icon })
            .bindPopup("<div style='text-align:center;padding:4px 2px;font-family:serif'><strong>Katie's Garden</strong><br><small style='color:#666'>Milverton, TA4 1PZ</small></div>", { maxWidth: 180 })
            .addTo(map)
            .openPopup();

        window._kgMapInstances[elementId] = map;
    },

    dispose: function (elementId) {
        if (window._kgMapInstances?.[elementId]) {
            window._kgMapInstances[elementId].remove();
            delete window._kgMapInstances[elementId];
        }
    }
};
