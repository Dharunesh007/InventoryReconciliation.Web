window.reconIqMobileEvidence = (() => {
    let recorder;
    let mediaStream;
    let chunks = [];
    let startedAt = 0;

    const supported = () => !!(navigator.mediaDevices && navigator.mediaDevices.getUserMedia && window.MediaRecorder);

    const cleanup = () => {
        if (mediaStream) {
            for (const track of mediaStream.getTracks()) {
                track.stop();
            }
        }

        recorder = undefined;
        mediaStream = undefined;
        chunks = [];
        startedAt = 0;
    };

    const startVoiceRecording = async () => {
        if (!supported()) {
            throw new Error("Voice recording is not supported in this browser.");
        }

        if (recorder && recorder.state === "recording") {
            return true;
        }

        mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        chunks = [];
        startedAt = Date.now();
        recorder = new MediaRecorder(mediaStream);

        recorder.ondataavailable = event => {
            if (event.data && event.data.size > 0) {
                chunks.push(event.data);
            }
        };

        recorder.start();
        return true;
    };

    const stopVoiceRecording = async () => {
        if (!recorder || recorder.state !== "recording") {
            cleanup();
            return null;
        }

        return await new Promise((resolve, reject) => {
            recorder.onerror = event => {
                cleanup();
                reject(event.error || new Error("Voice recording failed."));
            };

            recorder.onstop = () => {
                const mimeType = recorder.mimeType || "audio/webm";
                const blob = new Blob(chunks, { type: mimeType });
                const durationSeconds = Math.max(1, Math.round((Date.now() - startedAt) / 1000));
                const reader = new FileReader();

                reader.onloadend = () => {
                    const result = {
                        dataUrl: reader.result,
                        mimeType,
                        size: blob.size,
                        durationSeconds
                    };
                    cleanup();
                    resolve(result);
                };

                reader.onerror = () => {
                    cleanup();
                    reject(new Error("Could not read recorded voice note."));
                };

                reader.readAsDataURL(blob);
            };

            recorder.stop();
        });
    };

    const cancelVoiceRecording = () => {
        if (recorder && recorder.state === "recording") {
            recorder.stop();
        }

        cleanup();
    };

    const isRecording = () => !!(recorder && recorder.state === "recording");

    return {
        startVoiceRecording,
        stopVoiceRecording,
        cancelVoiceRecording,
        isRecording
    };
})();
