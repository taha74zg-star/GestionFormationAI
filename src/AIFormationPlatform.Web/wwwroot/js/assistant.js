(function () {
    'use strict';

    const ANAM_SDK_URL = 'https://esm.sh/@anam-ai/js-sdk@4.8.0';

    const S = {
        sessionId: null,
        anamClient: null,
        anamReady: false,
        isRecording: false,
        isProcessing: false,
        isSpeaking: false,
        isConversationActive: false,
        recognition: null,
        speechSupported: false,
        drawerOpen: false
    };

    const $ = (id) => document.getElementById(id);

    function getModuleId() {
        const configuredId = window.avatarModuleId ?? new URLSearchParams(window.location.search).get('moduleId');
        const moduleId = Number(configuredId);
        return Number.isInteger(moduleId) && moduleId > 0 ? moduleId : null;
    }

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

        if (ring) { ring.className = 'avatar-ring'; if (mode) ring.classList.add(mode); }
        if (glow) { glow.className = 'avatar-glow'; if (mode) glow.classList.add(mode); }
        if (bars) {
            bars.className = 'wave-bars';
            if (mode) bars.classList.add('visible');
            if (mode === 'speaking') bars.classList.add('speaking');
        }
    }

    function setTranscript(text, role) {
        const el = $('transcript');
        if (el) {
            el.textContent = text;
            el.className = 'transcript ' + (role || '');
        }
    }

    function addChatMessage(role, content) {
        const container = $('chat-messages');
        if (!container) return;
        const msg = document.createElement('div');
        msg.className = 'chat-msg chat-msg-' + role;
        msg.innerHTML =
            '<div class="chat-msg-label">' + (role === 'user' ? 'Vous' : 'Assistant') + '</div>' +
            '<div class="chat-msg-bubble">' + escapeHtml(content) + '</div>';
        container.appendChild(msg);
        container.scrollTop = container.scrollHeight;
    }

    function escapeHtml(s) { const d = document.createElement('div'); d.textContent = s; return d.innerHTML; }

    function updateButtons() {
        const start = $('btn-start');
        const mic = $('btn-mic');
        const stop = $('btn-stop');

        if (start) {
            start.disabled = S.isConversationActive;
            start.style.display = S.isConversationActive ? 'none' : '';
        }
        if (mic) {
            mic.disabled = !S.isConversationActive || S.isProcessing || S.isSpeaking;
        }
        if (stop) {
            stop.disabled = !S.isConversationActive;
            stop.style.display = S.isConversationActive ? '' : 'none';
        }
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
                if (S.isConversationActive) {
                    setStatus('Appuyez sur le micro', 'connected');
                    setVisualState('');
                    updateButtons();
                }
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
            const btn = $('btn-mic');
            if (btn) btn.classList.add('recording');
            setStatus('Je vous écoute...', 'listening');
            setVisualState('listening');
            setTranscript('...', 'user');
        } catch (e) {
            S.isRecording = false;
        }
    }

    function stopRecording() {
        S.isRecording = false;
        if (S.recognition) try { S.recognition.stop(); } catch (e) { }
        const btn = $('btn-mic');
        if (btn) btn.classList.remove('recording');
    }

    // ─── Chat API ─────────────────────────────────────────────
    async function processUserMessage(text) {
        if (!text || S.isProcessing) return;
        S.isProcessing = true;
        updateButtons();

        addChatMessage('user', text);
        setTranscript(text, 'user');
        setStatus('L\'assistant réfléchit...', 'thinking');
        setVisualState('thinking');

        try {
            if (!S.sessionId) S.sessionId = crypto.randomUUID ? crypto.randomUUID() : Date.now().toString(36);

            const resp = await fetch('/api/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    sessionId: S.sessionId,
                    message: text,
                    moduleId: getModuleId()
                })
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
                setStatus('Appuyez sur le micro', 'connected');
                setVisualState('');
                updateButtons();
            }

        } catch (err) {
            console.error('Chat error:', err);
            await respond('Erreur de connexion. Veuillez réessayer.');
        }

        S.isProcessing = false;
        updateButtons();
    }

    async function respond(text) {
        addChatMessage('ai', text);
        setTranscript(text, 'ai');
        setStatus('L\'avatar répond...', 'speaking');
        setVisualState('speaking');
        S.isSpeaking = true;
        updateButtons();

        await speakViaAnam(text);

        S.isSpeaking = false;
        if (S.isConversationActive) {
            setStatus('Appuyez sur le micro', 'connected');
            setVisualState('');
            setTranscript('', '');
        }
        updateButtons();
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

    // ─── Conversation Start / Stop ────────────────────────────
    async function startConversation() {
        if (S.isConversationActive) return;
        S.isConversationActive = true;
        updateButtons();
        setStatus('Connexion à l\'avatar...', 'loading');
        await connectAnam();
    }

    function stopConversation() {
        S.isConversationActive = false;
        stopRecording();
        stopAnam();

        S.sessionId = null;
        S.isProcessing = false;
        S.isSpeaking = false;
        if ('speechSynthesis' in window) speechSynthesis.cancel();

        if (S.sessionId) {
            fetch('/api/clear-session', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ sessionId: S.sessionId })
            }).catch(() => { });
        }

        const msgs = $('chat-messages');
        if (msgs) msgs.innerHTML = '';

        setVisualState('');
        setTranscript('', '');
        setStatus('Conversation terminée', '');
        updateButtons();
    }

    // ─── Anam Avatar ──────────────────────────────────────────
    async function connectAnam() {
        setStatus('Connexion à l\'avatar...', 'loading');

        try {
            const moduleId = getModuleId();
            const sessionRequest = moduleId !== null
                ? {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ moduleId })
                }
                : { method: 'POST' };
            const resp = await fetch('/api/session-token', sessionRequest);
            const data = await resp.json().catch(function () { return {}; });
            if (!resp.ok || !data.sessionToken) {
                throw new Error(data.error || 'Impossible de créer la session avatar.');
            }

            setStatus('Chargement de l\'avatar...', 'loading');

            const { createClient, AnamEvent } = await import(ANAM_SDK_URL);
            S.anamClient = createClient(data.sessionToken);

            S.anamClient.addListener(AnamEvent.SESSION_READY, function () {
                S.anamReady = true;
                setStatus('Avatar connecté — Appuyez sur le micro', 'connected');
                setVisualState('');
                updateButtons();
            });

            S.anamClient.addListener(AnamEvent.VIDEO_PLAY_STARTED, function () {
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
                    setStatus('Je vous écoute...', 'listening');
                    setVisualState('listening');
                } else if (evt.role === 'persona') {
                    setStatus('L\'avatar répond...', 'speaking');
                    setVisualState('speaking');
                }
            });

            S.anamClient.addListener(AnamEvent.TALK_STREAM_INTERRUPTED, function () {
                if (S.anamReady && !S.isProcessing && S.isConversationActive) {
                    setStatus('Avatar connecté — Appuyez sur le micro', 'connected');
                    setVisualState('');
                }
            });

            S.anamClient.addListener(AnamEvent.CONNECTION_CLOSED, function () {
                S.anamReady = false;
                if (S.isConversationActive) {
                    setStatus('Avatar déconnecté — reconnexion...', 'error');
                    updateButtons();
                    const v = $('avatar-video');
                    const p = $('avatar-placeholder');
                    if (v) { v.classList.remove('visible'); v.srcObject = null; }
                    if (p) p.classList.remove('hidden');
                    setTimeout(function () {
                        if (S.isConversationActive) connectAnam();
                    }, 3000);
                }
            });

            S.anamClient.addListener(AnamEvent.INPUT_AUDIO_STREAM_STARTED, function () {
                setStatus('Micro actif — Parlez...', 'listening');
            });

            await S.anamClient.streamToVideoElement('avatar-video');

        } catch (err) {
            console.error('Anam error:', err);
            if (S.isConversationActive) {
                const detail = err instanceof Error ? err.message : 'Erreur de connexion Anam.';
                setStatus('Avatar indisponible : ' + detail, 'error');
                updateButtons();
            }
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

    // ─── Drawer ───────────────────────────────────────────────
    function toggleDrawer() {
        S.drawerOpen = !S.drawerOpen;
        const c = $('drawer-content');
        if (c) c.classList.toggle('open', S.drawerOpen);
    }

    // ─── Init ─────────────────────────────────────────────────
    function init() {
        initSpeech();
        updateButtons();

        $('btn-start')?.addEventListener('click', startConversation);
        $('btn-stop')?.addEventListener('click', stopConversation);

        $('btn-mic')?.addEventListener('click', function () {
            if (S.isRecording) stopRecording();
            else startRecording();
        });

        $('drawer-toggle')?.addEventListener('click', toggleDrawer);
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init);
    else init();
})();
