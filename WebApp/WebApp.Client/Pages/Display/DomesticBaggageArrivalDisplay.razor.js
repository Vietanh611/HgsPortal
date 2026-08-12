export function setArrivalManifest() {
    let manifest = document.querySelector('link[rel="manifest"]');

    if (!manifest) {
        manifest = document.createElement('link');
        manifest.rel = 'manifest';
        document.head.appendChild(manifest);
    }

    manifest.href = '/Arrival-Display-manifest.webmanifest';
}