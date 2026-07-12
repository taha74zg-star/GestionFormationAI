(function () {
    'use strict';

    const ANAM_SDK_URL = 'https://esm.sh/@anam-ai/js-sdk@latest';

    const S = {
        sessionId: null,
        anamClient: null,
        anamReady: false,
        isRecording: false,
        isProcessing: false,
        isSpeaking: false,
        recognition: null,
        speechSupported: false,
        drawerOpen: false
    };

    const $ = (id) => document.getElementById(id);

    function setStatus(text, mode) {
        const dot = $('status-dot');
        const txt = $('status-text');
        if (dot) { dot.className = 'status-dot'; if (mode) dot.classList.add(mode); }
        if (txt) txt.textContent = text;
    }

    function setVisualState(mode) {
        const ring = $('avatar-ring');
        const glow = $('avatar-glow');
        const bars = $('wave-bars');
        const transcript = $('transcript');

        if (ring) { ring.className = 'avatar-ring'; if (mode) ring.classList.add(mode); }
        if (glow) { glow.className = 'avatar-glow'; if (mode) glow.classList.add(mode); }
        if (bars) { bars.className = 'wave-bars'; if (mode) bars.classList.add('visible'); if (mode === 'speaking') bars.classList.add('speaking'); }
        if (transcript) transcript.className = 'transcript';
    }

    function setTranscript(text, role) {
        const el = $('transcript');
        if (el) {
            el.textContent = text;
            el.className = 'transcript ' + role;
        }
    }

    function addChatMessage(role, content) {
        const container = $('chat-messages');
        if (!container) return;
        const msg = document.createElement('div');
        msg.className = 'chat-msg chat-msg-' + (role === 'user' ? 'user' : 'ai');
        msg.innerHTML =
            '<div class="chat-msg-label">' + (role === 'user' ? 'Vous' : 'Assistant') + '</div>' +
            '<div class="chat-msg-bubble">' + escapeHtml(content) + '</div>';
        container.appendChild(msg);
        container.scrollTop = container.scrollHeight;
    }

    function escapeHtml(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

    function setMicEnabled(on) {
        const btn = $('btn-mic');
        if (btn) btn.disabled = !on;
    }

    function setMicRecording(on) {
        const btn = $('btn-mic');
        if (btn) btn.classList.toggle('recording', on);
    }

    // ─── Speech Recognition ───────────────────────────────────
    function initSpeech() {
        const SR = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SR) { S.speechSupported = false; return; }

        S.speechSupported = true;
        S.recognition = new SR();
        S.recognition.continuous = false;
        S.recognition.interimResults = true;
        S.recognition.lang = '';

        S.recognition.onresult = function (e) {
            let final = '';
            let interim = '';
            for (let i = e.resultIndex; i < e.results.length; i++) {
                if (e.results[i].isFinal) final += e.results[i][0].transcript;
                else interim += e.results[i][0].transcript;
            }
            if (interim) setTranscript(interim, 'user');
            if (final) {
                stopRecording();
                processUserMessage(final);
            }
        };

        S.recognition.onerror = function (e) {
            console.warn('STT error:', e.error);
            stopRecording();
            if (e.error === 'no-speech' || e.error === 'aborted') {
                setStatus('Appuyez pour parler', 'connected');
                setVisualState('');
                enableMicIfReady();
            } else {
                respond('Je n\'ai pas bien entendu. Pourriez-vous répéter ?');
            }
        };

        S.recognition.onend = function () {
            if (S.isRecording) stopRecording();
        };
    }

    function startRecording() {
        if (!S.speechSupported || !S.recognition || S.isRecording || S.isProcessing || S.isSpeaking) return;
        try {
            S.isRecording = true;
            S.recognition.start();
            setMicRecording(true);
            setStatus('Écoute...', 'listening');
            setVisualState('listening');
            setTranscript('...', 'user');
        } catch (e) {
            S.isRecording = false;
        }
    }

    function stopRecording() {
        S.isRecording = false;
        if (S.recognition) try { S.recognition.stop(); } catch (e) { }
        setMicRecording(false);
    }

    // ─── Chat API ─────────────────────────────────────────────
    async function processUserMessage(text) {
        if (!text || S.isProcessing) return;
        S.isProcessing = true;
        setMicEnabled(false);

        addChatMessage('user', text);
        setTranscript(text, 'user');
        setStatus('Réflexion...', 'thinking');
        setVisualState('thinking');

        try {
            if (!S.sessionId) S.sessionId = crypto.randomUUID ? crypto.randomUUID() : Date.now().toString(36);

            const resp = await fetch('/api/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ sessionId: S.sessionId, message: text })
            });

            if (!resp.ok) throw new Error('API error');

            let fullText = '';
            const reader = resp.body.getReader();
            const decoder = new TextDecoder();

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;
                const raw = decoder.decode(value);
                for (const line of raw.split('\n')) {
                    if (!line.startsWith('data: ')) continue;
                    try {
                        const d = JSON.parse(line.slice(6));
                        if (d.content) fullText += d.content;
                        if (d.sessionId) S.sessionId = d.sessionId;
                    } catch (e) { }
                }
            }

            if (fullText) {
                await respond(fullText);
            } else {
                setStatus('Appuyez pour parler', 'connected');
                setVisualState('');
                enableMicIfReady();
            }

        } catch (err) {
            console.error('Chat error:', err);
            await respond('Erreur de connexion. Veuillez réessayer.');
        }

        S.isProcessing = false;
    }

    async function respond(text) {
        addChatMessage('ai', text);
        setTranscript(text, 'ai');
        setStatus('L\'assistant parle...', 'speaking');
        setVisualState('speaking');
        S.isSpeaking = true;
        setMicEnabled(false);

        await speakViaAnam(text);

        S.isSpeaking = false;
        setStatus('Appuyez pour parler', 'connected');
        setVisualState('');
        setTranscript('', '');
        enableMicIfReady();
    }

    async function speakViaAnam(text) {
        if (S.anamClient && S.anamReady) {
            try {
                const stream = S.anamClient.createTalkMessageStream();
                const chunks = text.match(/.{1,35}/g) || [text];
                for (let i = 0; i < chunks.length; i++) {
                    if (stream.isActive()) {
                        await stream.streamMessageChunk(chunks[i], i === chunks.length - 1);
                    }
                }
                await new Promise(r => setTimeout(r, 1200));
                return;
            } catch (e) {
                console.warn('Anam TTS failed:', e);
            }
        }

        if ('speechSynthesis' in window) {
            return new Promise(r => {
                const u = new SpeechSynthesisUtterance(text);
                u.lang = detectLang(text);
                u.rate = 1;
                u.onend = r;
                u.onerror = r;
                speechSynthesis.speak(u);
            });
        }

        await new Promise(r => setTimeout(r, 1000));
    }

    function detectLang(t) {
        if (/[\u0600-\u06FF]/.test(t)) return 'ar-MA';
        if (/\b(what|how|why|when|where|who|can|do|is|are|the|a|an|this|that|i|you|we|they)\b/i.test(t)) return 'en-US';
        return 'fr-FR';
    }

    function enableMicIfReady() {
        if (S.anamReady && S.speechSupported && !S.isProcessing && !S.isSpeaking) {
            setMicEnabled(true);
        }
    }

    // ─── Anam Avatar ──────────────────────────────────────────
    async function connectAnam() {
        setStatus('Connexion avatar...', 'loading');

        try {
            const resp = await fetch('/api/session-token', { method: 'POST' });
            const data = await resp.json();
            if (data.error) throw new Error(data.error);

            setStatus('Chargement avatar...', 'loading');

            const { createClient, AnamEvent } = await import(ANAM_SDK_URL);
            S.anamClient = createClient(data.sessionToken);

            S.anamClient.addListener(AnamEvent.SESSION_READY, function () {
                S.anamReady = true;
                setStatus('Appuyez pour parler', 'connected');
                setVisualState('');
                setMicEnabled(true);
                const v = $('avatar-video');
                const p = $('avatar-placeholder');
                if (v) v.classList.add('visible');
                if (p) p.classList.add('hidden');
            });

            S.anamClient.addListener(AnamEvent.MESSAGE_HISTORY_UPDATED, function (messages) {
                if (!messages || !messages.length) return;
                const last = messages[messages.length - 1];
                if (last.role === 'user' && last.content) {
                    processUserMessage(last.content);
                }
            });

            S.anamClient.addListener(AnamEvent.MESSAGE_STREAM_EVENT_RECEIVED, function (evt) {
                if (evt.role === 'user') {
                    setStatus('Vous parlez...', 'listening');
                    setVisualState('listening');
                } else if (evt.role === 'persona') {
                    setStatus('L\'assistant parle...', 'speaking');
                    setVisualState('speaking');
                }
            });

            S.anamClient.addListener(AnamEvent.TALK_STREAM_INTERRUPTED, function () {
                if (S.anamReady && !S.isProcessing) {
                    setStatus('Appuyez pour parler', 'connected');
                    setVisualState('');
                }
            });

            S.anamClient.addListener(AnamEvent.CONNECTION_CLOSED, function () {
                S.anamReady = false;
                setStatus('Avatar déconnecté — reconnexion...', 'error');
                setMicEnabled(false);
                const v = $('avatar-video');
                const p = $('avatar-placeholder');
                if (v) { v.classList.remove('visible'); v.srcObject = null; }
                if (p) p.classList.remove('hidden');
                setTimeout(connectAnam, 3000);
            });

            S.anamClient.addListener(AnamEvent.INPUT_AUDIO_STREAM_STARTED, function () {
                setStatus('Micro actif — Parlez...', 'listening');
            });

            await S.anamClient.streamToVideoElement('avatar-video');

        } catch (err) {
            console.error('Anam error:', err);
            setStatus('Avatar indisponible — mode texte', 'connected');
            setMicEnabled(S.speechSupported);
        }
    }

    function stopAnam() {
        if (S.anamClient) { S.anamClient.stopStreaming(); S.anamClient = null; }
        S.anamReady = false;
        const v = $('avatar-video');
        const p = $('avatar-placeholder');
        if (v) { v.classList.remove('visible'); v.srcObject = null; }
        if (p) p.classList.remove('hidden');
    }

    async function restart() {
        stopAnam();
        stopRecording();
        S.sessionId = null;
        S.isProcessing = false;
        S.isSpeaking = false;
        if ('speechSynthesis' in window) speechSynthesis.cancel();

        fetch('/api/clear-session', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ sessionId: S.sessionId })
        }).catch(() => { });

        const msgs = $('chat-messages');
        if (msgs) msgs.innerHTML = '';

        setVisualState('');
        setTranscript('', '');
        setMicEnabled(false);
        setStatus('Reconnexion...', 'loading');
        await connectAnam();
    }

    function toggleDrawer() {
        S.drawerOpen = !S.drawerOpen;
        const c = $('drawer-content');
        if (c) c.classList.toggle('open', S.drawerOpen);
    }

    // ─── Init ─────────────────────────────────────────────────
    function init() {
        initSpeech();
        setMicEnabled(false);

        $('btn-mic')?.addEventListener('click', function () {
            if (S.isRecording) stopRecording();
            else startRecording();
        });

        $('btn-restart')?.addEventListener('click', restart);
        $('drawer-toggle')?.addEventListener('click', toggleDrawer);

        connectAnam();
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
