(function () {
    const scanners = new Map();
    const nativeFormats = [
        "qr_code",
        "code_128",
        "code_39",
        "code_93",
        "codabar",
        "data_matrix",
        "ean_13",
        "ean_8",
        "itf",
        "pdf417",
        "upc_a",
        "upc_e"
    ];

    const libraryFormatNames = [
        "QR_CODE",
        "CODE_128",
        "CODE_39",
        "CODE_93",
        "CODABAR",
        "DATA_MATRIX",
        "EAN_13",
        "EAN_8",
        "ITF",
        "PDF_417",
        "UPC_A",
        "UPC_E"
    ];

    async function createDetector() {
        if (typeof window.BarcodeDetector.getSupportedFormats !== "function") {
            return new window.BarcodeDetector();
        }

        const supportedFormats = await window.BarcodeDetector.getSupportedFormats();
        const formats = nativeFormats.filter(format => supportedFormats.includes(format));
        return formats.length > 0
            ? new window.BarcodeDetector({ formats })
            : new window.BarcodeDetector();
    }

    async function start(videoId, dotNetReference, callbackName) {
        await stop(videoId);

        if (!window.navigator.mediaDevices || typeof window.navigator.mediaDevices.getUserMedia !== "function") {
            throw new Error("Camera access is not available in this browser.");
        }

        if (!window.isSecureContext && location.hostname !== "localhost" && location.hostname !== "127.0.0.1") {
            throw new Error("Camera scanning requires HTTPS. Localhost is allowed for testing.");
        }

        const video = document.getElementById(videoId);
        if (!video) {
            throw new Error("Scanner video element was not found.");
        }

        if ("BarcodeDetector" in window) {
            await startNativeScanner(videoId, video, dotNetReference, callbackName);
            return;
        }

        if ("Html5Qrcode" in window) {
            await startLibraryScanner(videoId, video, dotNetReference, callbackName);
            return;
        }

        throw new Error("Barcode scanning support was not loaded. Refresh the page and use current Edge or Chrome.");
    }

    async function startNativeScanner(videoId, video, dotNetReference, callbackName) {
        const detector = await createDetector();
        const stream = await window.navigator.mediaDevices.getUserMedia({
            audio: false,
            video: {
                facingMode: { ideal: "environment" },
                width: { ideal: 1280 },
                height: { ideal: 720 }
            }
        });

        video.srcObject = stream;
        video.muted = true;
        video.playsInline = true;
        video.setAttribute("playsinline", "true");
        await video.play();

        scanners.set(videoId, {
            active: true,
            callbackName: callbackName || "OnBarcodeDetected",
            detector,
            dotNetReference,
            mode: "native",
            stream,
            timeoutId: 0,
            video
        });

        scan(videoId);
    }

    async function startLibraryScanner(videoId, video, dotNetReference, callbackName) {
        const host = ensureLibraryHost(videoId, video);
        host.innerHTML = "";

        const supported = window.Html5QrcodeSupportedFormats || {};
        const formatsToSupport = libraryFormatNames
            .map(name => supported[name])
            .filter(format => typeof format === "number");

        const scanner = formatsToSupport.length > 0
            ? new window.Html5Qrcode(host.id, { formatsToSupport })
            : new window.Html5Qrcode(host.id);

        scanners.set(videoId, {
            active: true,
            callbackName: callbackName || "OnBarcodeDetected",
            dotNetReference,
            host,
            html5Scanner: scanner,
            mode: "library",
            video
        });

        await scanner.start(
            { facingMode: "environment" },
            { fps: 12, qrbox: { width: 220, height: 220 }, aspectRatio: 1.7777778 },
            async decodedText => {
                const state = scanners.get(videoId);
                if (!state || !state.active) {
                    return;
                }

                const value = (decodedText || "").trim();
                if (!value) {
                    return;
                }

                state.active = false;
                await state.dotNetReference.invokeMethodAsync(state.callbackName, value);
                await stop(videoId);
            },
            () => {
                // Decode misses are normal while the camera is moving.
            });
    }

    async function scan(videoId) {
        const state = scanners.get(videoId);
        if (!state || !state.active) {
            return;
        }

        try {
            if (state.video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA) {
                const codes = await state.detector.detect(state.video);
                const value = codes && codes.length > 0 ? (codes[0].rawValue || "").trim() : "";

                if (value) {
                    state.active = false;
                    await state.dotNetReference.invokeMethodAsync(state.callbackName, value);
                    await stop(videoId);
                    return;
                }
            }
        } catch {
            // Keep scanning. Some browsers briefly throw while the video stream initializes.
        }

        state.timeoutId = window.setTimeout(() => scan(videoId), 280);
    }

    async function stop(videoId) {
        if (!videoId) {
            await Promise.all(Array.from(scanners.keys()).map(id => stop(id)));
            return;
        }

        const state = scanners.get(videoId);
        if (!state) {
            return;
        }

        state.active = false;
        if (state.timeoutId) {
            window.clearTimeout(state.timeoutId);
        }

        if (state.html5Scanner) {
            try {
                if (state.html5Scanner.isScanning) {
                    await state.html5Scanner.stop();
                }
            } catch {
                // The library may already have stopped the camera after a decode.
            }

            try {
                state.html5Scanner.clear();
            } catch {
                // Clearing is best-effort because the element can be removed during navigation.
            }
        }

        if (state.host) {
            state.host.innerHTML = "";
        }

        if (state.video) {
            state.video.pause();
            state.video.removeAttribute("src");
            state.video.srcObject = null;
        }

        if (state.stream) {
            state.stream.getTracks().forEach(track => track.stop());
        }

        scanners.delete(videoId);
    }

    function ensureLibraryHost(videoId, video) {
        const hostId = `${videoId}LibraryHost`;
        let host = document.getElementById(hostId);
        if (host) {
            return host;
        }

        host = document.createElement("div");
        host.id = hostId;
        host.className = "scanner-library-host";
        const frame = video.closest(".scanner-frame, .phone-scan") || video.parentElement;
        frame.appendChild(host);
        return host;
    }

    window.reconIqBarcodeScanner = {
        start,
        stop,
        isSupported: () =>
            !!window.navigator.mediaDevices?.getUserMedia &&
            (("BarcodeDetector" in window) || ("Html5Qrcode" in window))
    };
})();
