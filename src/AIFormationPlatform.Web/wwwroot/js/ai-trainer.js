(function () {
    'use strict';

    const ANAM_SDK_URL = 'https://esm.sh/@anam-ai/js-sdk@latest';
    const state = {
        anamClient: null,
        lessonId: 0,
        conversationId: null,
        token: '',
        isStreaming: false,
        anamAvailable: false
    };

    function getToken() {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    }

    function addMessage(role, content) {
        const messagesDiv = document.getElementById('at-messages');
        if (!messagesDiv) return;

        const msg = document.createElement('div');
        msg.className = 'at-msg at-msg-' + (role === 'user' ? 'user' : 'ai');
        msg.innerHTML = '<div class="at-msg-label">' + (role === 'user' ? 'Vous' : 'Formateur IA') + '</div>'
            + '<div class="at-msg-bubble">' + escapeHtml(content) + '</div>';
        messagesDiv.appendChild(msg);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
        return msg;
    }

    function addLoading() {
        const messagesDiv = document.getElementById('at-messages');
        if (!messagesDiv) return null;
        const el = document.createElement('div');
        el.className = 'at-loading';
        el.id = 'at-loading';
        el.textContent = 'Le Formateur IA réfléchit';
        messagesDiv.appendChild(el);
        messagesDiv.scrollTop = messagesDiv.scrollHeight;
        return el;
    }

    function removeLoading() {
        document.getElementById('at-loading')?.remove();
    }

    function setStatus(text, mode) {
        const dot = document.getElementById('at-status-dot');
        const label = document.getElementById('at-status-label');
        if (dot) {
            dot.className = 'at-status-dot';
            if (mode) dot.classList.add(mode);
        }
        if (label) label.textContent = text;
    }

    function escapeHtml(str) {
        const div = document.createElement('div');
        div.textContent = str;
        return div.innerHTML;
    }

    async function sendTextMessage(question) {
        if (!question || state.isStreaming) return;
        state.isStreaming = true;

        const input = document.getElementById('at-text-input');
        const sendBtn = document.getElementById('at-send-btn');
        if (input) input.value = '';
        if (sendBtn) sendBtn.disabled = true;

        addMessage('user', question);
        addLoading();
        setStatus('En réflexion...', 'thinking');

        try {
            const response = await fetch('/AITrainer/Stream', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': state.token
                },
                body: JSON.stringify({
                    question: question,
                    lessonId: state.lessonId,
                    conversationId: state.conversationId
                })
            });

            if (!response.ok) throw new Error('Stream error');

            removeLoading();
            const aiMsg = addMessage('ai', '');
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
                            const bubble = aiMsg.querySelector('.at-msg-bubble');
                            if (bubble) bubble.textContent = fullText;
                            const messagesDiv = document.getElementById('at-messages');
                            if (messagesDiv) messagesDiv.scrollTop = messagesDiv.scrollHeight;
                        }
                        if (data.conversationId) {
                            state.conversationId = data.conversationId;
                        }
                    } catch (e) { }
                }
            }

            if (state.anamClient && state.anamClient.isStreaming && state.anamClient.isStreaming()) {
                try {
                    const talkStream = state.anamClient.createTalkMessageStream();
                    const chunks = fullText.match(/.{1,30}/g) || [fullText];
                    for (let i = 0; i < chunks.length; i++) {
                        if (talkStream.isActive()) {
                            await talkStream.streamMessageChunk(chunks[i], i === chunks.length - 1);
                        }
                    }
                } catch (e) {
                    console.warn('Anam talk failed:', e);
                }
            }

        } catch (err) {
            removeLoading();
            addMessage('ai', 'Erreur de connexion. Veuillez réessayer.');
        }

        state.isStreaming = false;
        if (sendBtn) sendBtn.disabled = false;
    }

    async function startAnamSession() {
        const startBtn = document.getElementById('at-btn-start');
        const stopBtn = document.getElementById('at-btn-stop');
        if (startBtn) startBtn.disabled = true;

        setStatus('Création de session...', '');

        try {
            const tokenResp = await fetch('/AITrainer/SessionToken', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': state.token
                },
                body: JSON.stringify({ lessonId: state.lessonId })
            });

            const tokenData = await tokenResp.json();
            if (!tokenData.success) throw new Error(tokenData.message);

            setStatus('Connexion en cours...', '');

            const { createClient, AnamEvent } = await import(ANAM_SDK_URL);

            state.anamClient = createClient(tokenData.sessionToken);

            state.anamClient.addListener(AnamEvent.SESSION_READY, function () {
                setStatus('Connecté — Parlez librement', 'connected');
                if (stopBtn) stopBtn.disabled = false;
                state.anamAvailable = true;
            });

            state.anamClient.addListener(AnamEvent.MESSAGE_HISTORY_UPDATED, function (messages) {
                if (!messages || messages.length === 0) return;
                const last = messages[messages.length - 1];
                if (last.role === 'user' && last.content) {
                    setStatus('En réflexion...', 'thinking');
                    sendTextMessage(last.content);
                }
            });

            state.anamClient.addListener(AnamEvent.MESSAGE_STREAM_EVENT_RECEIVED, function (evt) {
                if (evt.role === 'user') {
                    setStatus('Vous parlez...', 'listening');
                } else if (evt.role === 'persona') {
                    setStatus('Le formateur parle...', 'speaking');
                }
            });

            state.anamClient.addListener(AnamEvent.TALK_STREAM_INTERRUPTED, function () {
                setStatus('Connecté — Parlez librement', 'connected');
            });

            state.anamClient.addListener(AnamEvent.CONNECTION_CLOSED, function () {
                setStatus('Déconnecté', '');
                state.anamAvailable = false;
                if (startBtn) startBtn.disabled = false;
                if (stopBtn) stopBtn.disabled = true;
            });

            state.anamClient.addListener(AnamEvent.INPUT_AUDIO_STREAM_STARTED, function () {
                setStatus('Micro actif — Parlez...', 'listening');
            });

            await state.anamClient.streamToVideoElement('at-avatar-video');

        } catch (err) {
            console.error('Anam session error:', err);
            setStatus('Avatar indisponible — utilisez le texte', '');
            if (startBtn) startBtn.disabled = false;
            showTextFallback();
        }
    }

    function stopAnamSession() {
        if (state.anamClient) {
            state.anamClient.stopStreaming();
            state.anamClient = null;
        }
        state.anamAvailable = false;
        const video = document.getElementById('at-avatar-video');
        if (video) video.srcObject = null;

        const startBtn = document.getElementById('at-btn-start');
        const stopBtn = document.getElementById('at-btn-stop');
        if (startBtn) startBtn.disabled = false;
        if (stopBtn) stopBtn.disabled = true;
        setStatus('Session terminée', '');
    }

    function showTextFallback() {
        const fallback = document.getElementById('at-fallback');
        if (fallback) fallback.style.display = 'block';
    }

    function init() {
        const el = document.getElementById('ai-trainer-data');
        if (!el) return;

        state.lessonId = parseInt(el.dataset.lessonId) || 0;
        state.conversationId = null;
        state.token = getToken();

        const startBtn = document.getElementById('at-btn-start');
        const stopBtn = document.getElementById('at-btn-stop');
        const textForm = document.getElementById('at-text-form');

        if (startBtn) startBtn.addEventListener('click', startAnamSession);
        if (stopBtn) stopBtn.addEventListener('click', stopAnamSession);

        if (textForm) {
            textForm.addEventListener('submit', function (e) {
                e.preventDefault();
                const input = document.getElementById('at-text-input');
                const q = input ? input.value.trim() : '';
                if (q) sendTextMessage(q);
            });
        }

        showTextFallback();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
