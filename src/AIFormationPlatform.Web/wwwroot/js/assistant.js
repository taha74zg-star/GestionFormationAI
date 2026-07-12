(function () {
    'use strict';

    const ANAM_SDK_URL = 'https://esm.sh/@anam-ai/js-sdk@latest';

    const state = {
        sessionId: null,
        anamClient: null,
        anamReady: false,
        isRecording: false,
        isProcessing: false,
        recognition: null,
        speechSupported: false
    };

    const $ = (id) => document.getElementById(id);

    function setStatus(text, mode) {
        const dot = $('status-dot');
        const txt = $('status-text');
        if (dot) {
            dot.className = 'status-dot';
            if (mode) dot.classList.add(mode);
        }
        if (txt) txt.textContent = text;
    }

    function setButtonStates({ connect, mic, stop }) {
        const c = $('btn-connect');
        const m = $('btn-mic');
        const s = $('btn-stop');
        if (c) c.disabled = !connect;
        if (m) m.disabled = !mic;
        if (s) s.disabled = !stop;
    }

    function addMessage(role, content) {
        const container = $('chat-messages');
        if (!container) return;

        const welcome = $('chat-welcome');
        if (welcome) welcome.remove();

        const msg = document.createElement('div');
        msg.className = 'chat-msg chat-msg-' + (role === 'user' ? 'user' : 'ai');
        msg.innerHTML =
            '<div class="chat-msg-label">' + (role === 'user' ? 'Vous' : 'Assistant IA') + '</div>' +
            '<div class="chat-msg-bubble">' + escapeHtml(content) + '</div>';
        container.appendChild(msg);
        container.scrollTop = container.scrollHeight;
        return msg;
    }

    function addLoading() {
        const container = $('chat-messages');
        if (!container) return null;
        const el = document.createElement('div');
        el.className = 'chat-loading';
        el.id = 'chat-loading';
        el.innerHTML = '<span>&#9679;</span><span>&#9679;</span><span>&#9679;</span>';
        container.appendChild(el);
        container.scrollTop = container.scrollHeight;
        return el;
    }

    function removeLoading() {
        const el = $('chat-loading');
        if (el) el.remove();
    }

    function escapeHtml(str) {
        const d = document.createElement('div');
        d.textContent = str;
        return d.innerHTML;
    }

    function setWaves(visible) {
        const w = $('wave-container');
        if (w) w.classList.toggle('visible', visible);
    }

    function setAvatarActive(active) {
        const c = $('avatar-container');
        if (c) c.classList.toggle('active', active);
    }

    function initSpeechRecognition() {
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!SpeechRecognition) {
            state.speechSupported = false;
            return;
        }

        state.speechSupported = true;
        state.recognition = new SpeechRecognition();
        state.recognition.continuous = false;
        state.recognition.interimResults = false;

        state.recognition.onresult = function (event) {
            const transcript = event.results[0][0].transcript;
            stopRecording();
            sendMessage(transcript);
        };

        state.recognition.onerror = function (event) {
            console.warn('Speech error:', event.error);
            stopRecording();
            if (event.error !== 'no-speech' && event.error !== 'aborted') {
                addMessage('ai', 'Je n\'ai pas compris. Pourriez-vous répéter ?');
                speakText('Je n\'ai pas compris. Pourriez-vous répéter ?');
            }
        };

        state.recognition.onend = function () {
            if (state.isRecording) {
                stopRecording();
            }
        };
    }

    function startRecording() {
        if (!state.speechSupported || !state.recognition || state.isRecording || state.isProcessing) return;

        try {
            state.isRecording = true;
            state.recognition.start();
            const btn = $('btn-mic');
            if (btn) btn.classList.add('recording');
            setStatus('Écoute en cours...', 'listening');
            setWaves(true);
        } catch (e) {
            console.warn('Recognition start error:', e);
            state.isRecording = false;
        }
    }

    function stopRecording() {
        state.isRecording = false;
        if (state.recognition) {
            try { state.recognition.stop(); } catch (e) { }
        }
        const btn = $('btn-mic');
        if (btn) btn.classList.remove('recording');
        setWaves(false);
    }

    async function sendMessage(text) {
        if (!text || state.isProcessing) return;
        state.isProcessing = true;

        addMessage('user', text);
        addLoading();
        setStatus('Réflexion...', 'thinking');
        setButtonStates({ connect: false, mic: false, stop: false });

        try {
            if (!state.sessionId) {
                state.sessionId = crypto.randomUUID ? crypto.randomUUID() : Date.now().toString(36);
            }

            const response = await fetch('/api/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ sessionId: state.sessionId, message: text })
            });

            if (!response.ok) throw new Error('Chat error');

            removeLoading();
            const aiMsg = addMessage('ai', '');
            const bubble = aiMsg ? aiMsg.querySelector('.chat-msg-bubble') : null;
            let fullText = '';

            const reader = response.body.getReader();
            const decoder = new TextDecoder();

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                const text = decoder.decode(value);
                const lines = text.split('\n');

                for (const line of lines) {
                    if (!line.startsWith('data: ')) continue;
                    try {
                        const data = JSON.parse(line.slice(6));
                        if (data.content) {
                            fullText += data.content;
                            if (bubble) bubble.textContent = fullText;
                            const container = $('chat-messages');
                            if (container) container.scrollTop = container.scrollHeight;
                        }
                        if (data.sessionId) {
                            state.sessionId = data.sessionId;
                        }
                    } catch (e) { }
                }
            }

            if (fullText) {
                setStatus('L\'assistant parle...', 'speaking');
                setAvatarActive(true);
                await speakText(fullText);
                setAvatarActive(false);
            }

            if (state.anamReady) {
                setStatus('Connecté — Parlez librement', 'connected');
            } else {
                setStatus('Prêt', 'connected');
            }

            if (state.speechSupported) {
                setButtonStates({ connect: !state.anamReady, mic: true, stop: state.anamReady });
            }

        } catch (err) {
            console.error('Chat error:', err);
            removeLoading();
            addMessage('ai', 'Erreur de connexion. Veuillez réessayer.');
            setStatus('Erreur', 'error');
        }

        state.isProcessing = false;
    }

    async function speakText(text) {
        if (state.anamClient && state.anamReady) {
            try {
                const stream = state.anamClient.createTalkMessageStream();
                const chunks = text.match(/.{1,40}/g) || [text];
                for (let i = 0; i < chunks.length; i++) {
                    if (stream.isActive()) {
                        await stream.streamMessageChunk(chunks[i], i === chunks.length - 1);
                    }
                }
                await new Promise(resolve => setTimeout(resolve, 1500));
                return;
            } catch (e) {
                console.warn('Anam TTS failed, falling back to browser:', e);
            }
        }

        if ('speechSynthesis' in window) {
            return new Promise((resolve) => {
                const utterance = new SpeechSynthesisUtterance(text);
                utterance.lang = detectLanguage(text);
                utterance.rate = 1;
                utterance.onend = resolve;
                utterance.onerror = resolve;
                speechSynthesis.speak(utterance);
            });
        }

        return new Promise(resolve => setTimeout(resolve, 1000));
    }

    function detectLanguage(text) {
        const arabic = /[\u0600-\u06FF]/;
        if (arabic.test(text)) return 'ar-MA';
        if (/\b(what|how|why|when|where|who|can|do|is|are|the|a|an|this|that|i|you|we|they)\b/i.test(text)) return 'en-US';
        return 'fr-FR';
    }

    async function connectAnam() {
        const btn = $('btn-connect');
        if (btn) btn.disabled = true;
        setStatus('Connexion avatar...', '');

        try {
            const resp = await fetch('/api/session-token', { method: 'POST' });
            const data = await resp.json();
            if (data.error) throw new Error(data.error);

            setStatus('Chargement avatar...', '');

            const { createClient, AnamEvent } = await import(ANAM_SDK_URL);
            state.anamClient = createClient(data.sessionToken);

            state.anamClient.addListener(AnamEvent.SESSION_READY, function () {
                state.anamReady = true;
                setStatus('Connecté — Parlez librement', 'connected');
                setButtonStates({ connect: false, mic: true, stop: true });
                const video = $('avatar-video');
                const placeholder = $('avatar-placeholder');
                if (video) video.classList.add('visible');
                if (placeholder) placeholder.classList.add('hidden');
            });

            state.anamClient.addListener(AnamEvent.MESSAGE_HISTORY_UPDATED, function (messages) {
                if (!messages || messages.length === 0) return;
                const last = messages[messages.length - 1];
                if (last.role === 'user' && last.content) {
                    sendMessage(last.content);
                }
            });

            state.anamClient.addListener(AnamEvent.MESSAGE_STREAM_EVENT_RECEIVED, function (evt) {
                if (evt.role === 'user') {
                    setStatus('Vous parlez...', 'listening');
                } else if (evt.role === 'persona') {
                    setStatus('L\'assistant parle...', 'speaking');
                    setAvatarActive(true);
                }
            });

            state.anamClient.addListener(AnamEvent.TALK_STREAM_INTERRUPTED, function () {
                setAvatarActive(false);
                if (state.anamReady) {
                    setStatus('Connecté — Parlez librement', 'connected');
                }
            });

            state.anamClient.addListener(AnamEvent.CONNECTION_CLOSED, function () {
                state.anamReady = false;
                setStatus('Avatar déconnecté', 'error');
                setButtonStates({ connect: true, mic: state.speechSupported, stop: false });
                const video = $('avatar-video');
                const placeholder = $('avatar-placeholder');
                if (video) { video.classList.remove('visible'); video.srcObject = null; }
                if (placeholder) placeholder.classList.remove('hidden');
            });

            state.anamClient.addListener(AnamEvent.INPUT_AUDIO_STREAM_STARTED, function () {
                setStatus('Micro actif — Parlez...', 'listening');
            });

            await state.anamClient.streamToVideoElement('avatar-video');

        } catch (err) {
            console.error('Anam connection error:', err);
            setStatus('Avatar indisponible — mode texte', 'connected');
            setButtonStates({ connect: false, mic: state.speechSupported, stop: false });
        }
    }

    function stopAnam() {
        if (state.anamClient) {
            state.anamClient.stopStreaming();
            state.anamClient = null;
        }
        state.anamReady = false;
        const video = $('avatar-video');
        const placeholder = $('avatar-placeholder');
        if (video) { video.classList.remove('visible'); video.srcObject = null; }
        if (placeholder) placeholder.classList.remove('hidden');
        setAvatarActive(false);
        setButtonStates({ connect: true, mic: state.speechSupported, stop: false });
        setStatus('Déconnecté', '');
    }

    async function restart() {
        stopAnam();
        stopRecording();
        state.sessionId = null;
        state.isProcessing = false;

        const container = $('chat-messages');
        if (container) {
            container.innerHTML = '<div class="chat-welcome" id="chat-welcome"><p>Bonjour ! Je suis votre assistant IA.</p><p>Connectez-vous puis posez-moi vos questions par la voix ou le texte.</p></div>';
        }

        if (state.speechSupported) {
            setButtonStates({ connect: true, mic: false, stop: false });
        }
        setStatus('Prêt', '');
    }

    function init() {
        initSpeechRecognition();
        setButtonStates({ connect: true, mic: false, stop: false });

        $('btn-connect')?.addEventListener('click', connectAnam);
        $('btn-stop')?.addEventListener('click', stopAnam);
        $('btn-restart')?.addEventListener('click', restart);

        $('btn-mic')?.addEventListener('click', function () {
            if (state.isRecording) {
                stopRecording();
            } else {
                startRecording();
            }
        });

        $('chat-form')?.addEventListener('submit', function (e) {
            e.preventDefault();
            const input = $('chat-input');
            const text = input ? input.value.trim() : '';
            if (text) {
                input.value = '';
                sendMessage(text);
            }
        });

        if (state.speechSupported) {
            setStatus('Prêt — Connectez l\'avatar ou parlez', '');
        } else {
            setStatus('Mode texte uniquement', '');
            setButtonStates({ connect: true, mic: false, stop: false });
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
