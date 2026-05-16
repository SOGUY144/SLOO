mergeInto(LibraryManager.library, {
    
    // --- MICROPHONE VOLUME ---
    InitWebMic: function () {
        if (window.webMicInitialized) return;
        
        navigator.mediaDevices.getUserMedia({ audio: true, video: false })
        .then(function(stream) {
            window.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            window.analyser = window.audioContext.createAnalyser();
            window.microphone = window.audioContext.createMediaStreamSource(stream);
            window.microphone.connect(window.analyser);
            window.analyser.fftSize = 256;
            window.dataArray = new Uint8Array(window.analyser.frequencyBinCount);
            window.webMicInitialized = true;
            console.log("Web Microphone Initialized!");
        })
        .catch(function(err) {
            console.log("Web Mic error: " + err);
        });
    },
    
    GetWebMicVolume: function () {
        if (!window.webMicInitialized || !window.analyser) return 0.0;
        
        window.analyser.getByteTimeDomainData(window.dataArray);
        var max = 0;
        for (var i = 0; i < window.dataArray.length; i++) {
            var val = (window.dataArray[i] - 128) / 128.0;
            var peak = val * val;
            if (peak > max) max = peak;
        }
        return Math.sqrt(max);
    },
    
    StopWebMic: function() {
        if (window.audioContext && window.audioContext.state !== 'closed') {
            window.audioContext.close();
            window.webMicInitialized = false;
        }
    },

    // --- SPEECH RECOGNITION ---
    InitWebSpeech: function(objectNamePtr) {
        if (window.webSpeechRecognition) return;

        var gameObjectName = UTF8ToString(objectNamePtr);

        if (!('webkitSpeechRecognition' in window)) {
            console.log("Web Speech API is not supported in this browser.");
            return;
        }

        var recognition = new webkitSpeechRecognition();
        recognition.continuous = true;
        recognition.interimResults = false;
        // ให้รองรับเสียงภาษาอังกฤษเป็นหลัก (เพราะคำร่ายคือ Fire, Push, Boom)
        recognition.lang = 'en-US'; 

        recognition.onresult = function (event) {
            var lastResult = event.results[event.results.length - 1];
            if (lastResult.isFinal) {
                var transcript = lastResult[0].transcript.trim().toLowerCase();
                console.log("Web Speech Recognized: " + transcript);
                // ส่งคำที่ฟังได้ กลับเข้าไปหาฟังก์ชันใน Unity
                SendMessage(gameObjectName, "OnWebSpeechRecognized", transcript);
            }
        };

        recognition.onerror = function(event) {
            console.log("Web Speech Error: " + event.error);
        };

        recognition.onend = function() {
            // บังคับให้มันเปิดฟังใหม่เรื่อยๆ ถ้าระบบยัง Active อยู่
            if (window.webSpeechActive) {
                recognition.start();
            }
        };

        window.webSpeechRecognition = recognition;
    },

    StartWebSpeech: function() {
        if (window.webSpeechRecognition && !window.webSpeechActive) {
            window.webSpeechActive = true;
            window.webSpeechRecognition.start();
            console.log("Web Speech Started");
        }
    },

    StopWebSpeech: function() {
        if (window.webSpeechRecognition && window.webSpeechActive) {
            window.webSpeechActive = false;
            window.webSpeechRecognition.stop();
            console.log("Web Speech Stopped");
        }
    }
});
