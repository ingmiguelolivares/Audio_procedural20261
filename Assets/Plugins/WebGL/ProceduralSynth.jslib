mergeInto(LibraryManager.library, {
  PSW_UnlockAudio: function() {
    if (!globalThis.ProceduralSynthState) {
      globalThis.ProceduralSynthState = {
        ctx: null,
        node: null,
        master: null,
        driver: null,
        mediaDestination: null,
        mediaElement: null,
        unlockListenersInstalled: false,
        debugLoggedUnlock: false,
        debugLoggedProcess: false,
        debugProcessCounter: 0,
        debugLastActiveVoices: -1
      };
    }

    var state = globalThis.ProceduralSynthState;
    var AudioCtx = globalThis.AudioContext || globalThis.webkitAudioContext;
    if (!AudioCtx) return;

    if (!globalThis.ProceduralSynthEnsureAudioUnlocked) {
      globalThis.ProceduralSynthIsAppleMobileWeb = function() {
        var ua = globalThis.navigator && globalThis.navigator.userAgent ? globalThis.navigator.userAgent : "";
        var platform = globalThis.navigator && globalThis.navigator.platform ? globalThis.navigator.platform : "";
        var touchPoints = globalThis.navigator && typeof globalThis.navigator.maxTouchPoints === "number" ? globalThis.navigator.maxTouchPoints : 0;
        return /iPad|iPhone|iPod/.test(ua) || (platform === "MacIntel" && touchPoints > 1);
      };

      globalThis.ProceduralSynthWarmupIosOutput = function(ctx) {
        if (!ctx) return;
        try {
          var buffer = ctx.createBuffer(1, 1, Math.max(22050, ctx.sampleRate || 44100));
          var source = ctx.createBufferSource();
          var gain = ctx.createGain();
          gain.gain.value = 0.00001;
          source.buffer = buffer;
          source.connect(gain);
          gain.connect(ctx.destination);
          source.start(0);
          if (source.stop) source.stop(0.001);
        } catch (e) {
        }

        if (globalThis.ProceduralSynthIsAppleMobileWeb && globalThis.ProceduralSynthIsAppleMobileWeb()) {
          if (globalThis.primeHtmlMediaPlayback) {
            try {
              globalThis.primeHtmlMediaPlayback();
            } catch (mediaPrimeError) {
            }
          }

          try {
            var osc = ctx.createOscillator();
            var oscGain = ctx.createGain();
            osc.type = "sine";
            osc.frequency.value = 880;
            oscGain.gain.setValueAtTime(0.0001, ctx.currentTime);
            oscGain.gain.linearRampToValueAtTime(0.015, ctx.currentTime + 0.005);
            oscGain.gain.linearRampToValueAtTime(0.0001, ctx.currentTime + 0.03);
            osc.connect(oscGain);
            oscGain.connect(ctx.destination);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.03);
          } catch (e2) {
          }
        }
      };

      globalThis.ProceduralSynthEnsureAudioUnlocked = function() {
        if (!globalThis.ProceduralSynthState) return;
        var innerState = globalThis.ProceduralSynthState;
        var InnerAudioCtx = globalThis.AudioContext || globalThis.webkitAudioContext;
        if (!InnerAudioCtx) return;
        var preferMediaElementOutput = globalThis.ProceduralSynthIsAppleMobileWeb && globalThis.ProceduralSynthIsAppleMobileWeb();

        if (!innerState.ctx) {
          innerState.ctx = new InnerAudioCtx();
        }

        if (!innerState.node) {
          innerState.master = innerState.ctx.createGain();
          innerState.master.gain.value = 1.0;

          innerState.node = innerState.ctx.createScriptProcessor(1024, 1, 2);
          innerState.node.onaudioprocess = function(evt) {
            var left = evt.outputBuffer.getChannelData(0);
            var right = evt.outputBuffer.numberOfChannels > 1 ? evt.outputBuffer.getChannelData(1) : left;
            left.fill(0);
            if (right !== left) right.fill(0);

            var voices = globalThis.ProceduralSynthVoices || {};
            var handles = Object.keys(voices);
            var activeVoices = 0;
            var peak = 0;
            var invalidSamples = 0;

            function sine(v, f, t) { return Math.sin(2 * Math.PI * f * t / v.sampleRate); }
            function curve01(progress, useLogCurve) {
              progress = Math.max(0, Math.min(1, progress));
              if (!useLogCurve) return progress;
              return Math.log10(1 + (9 * progress));
            }
            function releaseFrames(v) {
              return Math.max(1, Math.round((v.releaseMs / 1000) * v.sampleRate));
            }
            function naturalReleaseEnd(v) {
              var attack = Math.max(1, Math.round((v.attackMs / 1000) * v.sampleRate));
              var decay = attack + Math.round((v.decayMs / 1000) * v.sampleRate);
              var sustain = decay + Math.round((v.sustainMs / 1000) * v.sampleRate);
              return sustain + releaseFrames(v);
            }
            function scheduledAdsr(v, t) {
              if (v.useAudioClipADSR && v.adsrData.length > 0) return (t >= 0 && t < v.adsrData.length) ? Math.max(0, Math.min(1, v.adsrData[t])) : 0;
              var attack = Math.max(1, Math.round((v.attackMs / 1000) * v.sampleRate));
              var decay = attack + Math.round((v.decayMs / 1000) * v.sampleRate);
              var sustain = decay + Math.round((v.sustainMs / 1000) * v.sampleRate);
              var release = sustain + Math.round((v.releaseMs / 1000) * v.sampleRate);
              if (t < attack) return curve01(t / attack, v.attackUsesLogCurve);
              if (t < decay) return 1 + ((v.sustainLevel - 1) * curve01((t - attack) / Math.max(1, decay - attack), v.decayUsesLogCurve));
              if (t < sustain) return v.sustainLevel;
              if (t < release) return v.sustainLevel + ((0.0000001 - v.sustainLevel) * curve01((t - sustain) / Math.max(1, release - sustain), v.releaseUsesLogCurve));
              return 0.0000001;
            }
            function adsr(v, t) {
              if (v.releaseTriggered && t >= v.releaseStartTimeIndex) {
                return v.releaseStartLevel + ((0.0000001 - v.releaseStartLevel) * curve01((t - v.releaseStartTimeIndex) / releaseFrames(v), v.releaseUsesLogCurve));
              }
              return scheduledAdsr(v, t);
            }
            function wavetable(v, time, freq) {
              if (!v.wavetable.length) return 0;
              var idx = Math.round((time * freq / v.sampleRate) * v.wavetable.length) % v.wavetable.length;
              if (idx < 0) idx += v.wavetable.length;
              return v.wavetable[idx];
            }
            function additive(v, freq, time) {
              if (v.harmonicCount <= 0 || !v.amplitudes.length) return 0;
              var limit = Math.min(v.harmonicCount, v.amplitudes.length), sum = 0;
              for (var h = 1; h <= limit; h++) sum += Math.sin(2 * Math.PI * h * freq * time / v.sampleRate) * v.amplitudes[h - 1];
              return sum / Math.max(1, limit);
            }
            function fm(v, freq, time) {
              var amount = Math.max(0, Math.min(1, v.fmMacroAmount));
              var ratio = v.fmMinRatio + (v.fmMaxRatio - v.fmMinRatio) * amount;
              var modIndex = v.fmMaxIndex * Math.max(0.05, v.fmModIndex) * amount;
              var modFreq = Math.min(freq * ratio, v.sampleRate * 0.45);
              var blend = v.fmHighHarmonicBlend * amount;
              var carrierPhase = 2 * Math.PI * freq * time / v.sampleRate;
              var modPhase = 2 * Math.PI * modFreq * time / v.sampleRate;
              var modulation = Math.sin(modPhase);
              var primary = Math.sin(carrierPhase + modIndex * modulation);
              var bright = Math.sin((carrierPhase * 2) + (modIndex * 0.5 * modulation));
              return primary + (0.18 * blend * (bright - primary));
            }
            function sampling(v, freq, advance) {
              if (!v.samplingData.length || v.samplingTotalFrames <= 0) return 0;
              var startFrame = Math.max(0, Math.min(v.samplingStartFrame, Math.max(0, v.samplingTotalFrames - 1)));
              var endFrame = Math.max(startFrame + 1, Math.min(v.samplingEndFrame, Math.max(0, v.samplingTotalFrames - 1)));
              if (endFrame <= startFrame) return 0;
              var pos = v.samplingReadPosition;
              if (pos >= endFrame) return 0;
              var baseFreq = Math.max(1, v.samplingBaseFrequency);
              var ratio = freq / baseFreq;
              var frame0 = Math.max(startFrame, Math.min(Math.floor(pos), endFrame));
              var frame1 = Math.max(startFrame, Math.min(frame0 + 1, endFrame));
              var frac = pos - frame0;
              function frame(frameIdx) {
                var channels = Math.max(1, v.samplingChannels);
                var base = frameIdx * channels;
                if (base < 0 || base >= v.samplingData.length) return 0;
                if (channels === 1) return v.samplingData[base];
                var sum = 0;
                for (var c = 0; c < channels; c++) sum += v.samplingData[base + c] || 0;
                return sum / channels;
              }
              var result = frame(frame0) + ((frame(frame1) - frame(frame0)) * frac);
              if (advance) v.samplingReadPosition += ratio;
              return result;
            }
            function gen(v, freq, time, advanceSampling) {
              if (v.waveform !== 7 && freq <= 0) return 0;
              if (v.useWavetable && v.wavetable.length && v.waveform !== 5) return wavetable(v, time, freq);
              switch (v.waveform) {
                case 0: return sine(v, freq, time);
                case 1: return sine(v, freq, time) >= 0 ? 1 : -1;
                case 2: return 1 + (-2 * ((time % (v.sampleRate / freq)) / Math.max(1, v.sampleRate / freq)));
                case 3: {
                  var period = v.sampleRate / freq, pos = time % period, q = period * 0.25, tq = period * 0.75;
                  if (pos <= q) return pos / Math.max(1, q);
                  if (pos <= tq) return 1 - 2 * ((pos - q) / Math.max(1, tq - q));
                  return -1 + ((pos - tq) / Math.max(1, period - tq));
                }
                case 4: return additive(v, freq, time);
                case 5: return fm(v, freq, time);
                case 6: return sampling(v, freq, advanceSampling);
                case 7: return (Math.random() * 2) - 1;
                default: return 0;
              }
            }

            for (var handleIndex = 0; handleIndex < handles.length; handleIndex++) {
              var activeVoice = voices[handles[handleIndex]];
              if (activeVoice && activeVoice.active && activeVoice.frequency > 0 && activeVoice.level > 0) {
                activeVoices++;
              }
            }

            for (var sampleIndex = 0; sampleIndex < left.length; sampleIndex++) {
              var mixed = 0;
              for (var handleIndex = 0; handleIndex < handles.length; handleIndex++) {
                var v = voices[handles[handleIndex]];
                if (!v || !v.active || v.frequency <= 0 || v.level <= 0) continue;

                var t = v.timeIndex;
                var env = adsr(v, t);
                var trem = v.tremLfoFrequency > 0 ? 0.5 + 0.5 * sine(v, v.tremLfoFrequency, t) : 1;
                var vib = v.vibLfoFrequency > 0 ? v.vibratoDepth * sine(v, v.vibLfoFrequency, t) : 0;
                var currentFreq = Math.max(0, v.frequency + vib);
                var detunedFreq = currentFreq * Math.pow(2, v.detuneCents / 1200);
                mixed += v.level * 0.5 * (gen(v, currentFreq, t, true) + gen(v, detunedFreq, t, false)) * env * trem;
                v.timeIndex++;

                var finished = v.releaseTriggered
                  ? (t - v.releaseStartTimeIndex) >= releaseFrames(v)
                  : t >= naturalReleaseEnd(v);

                if (finished) {
                  v.active = false;
                  v.frequency = 0;
                  v.samplingReadPosition = v.samplingStartFrame;
                  v.releaseTriggered = false;
                }
              }
              if (!isFinite(mixed)) {
                invalidSamples++;
                mixed = 0;
              }
              if (Math.abs(mixed) > peak) {
                peak = Math.abs(mixed);
              }
              mixed = Math.max(-1, Math.min(1, mixed));
              left[sampleIndex] = mixed;
              if (right !== left) right[sampleIndex] = mixed;
            }

            innerState.debugProcessCounter++;
            if (!innerState.debugLoggedProcess) {
              innerState.debugLoggedProcess = true;
              console.log("[ProceduralSynth] onaudioprocess started", "sampleRate=", innerState.ctx.sampleRate, "channels=", evt.outputBuffer.numberOfChannels);
            }

            if (activeVoices !== innerState.debugLastActiveVoices) {
              innerState.debugLastActiveVoices = activeVoices;
              console.log("[ProceduralSynth] activeVoices=", activeVoices);
              console.log("[ProceduralSynth] mixSnapshot", "activeVoices=", activeVoices, "peak=", peak, "invalidSamples=", invalidSamples);
            }

            if ((innerState.debugProcessCounter % 180) === 0) {
              console.log("[ProceduralSynth] processTick", "count=", innerState.debugProcessCounter, "activeVoices=", activeVoices, "peak=", peak, "invalidSamples=", invalidSamples);
            }
          };

          var preferOscillatorDriver = globalThis.ProceduralSynthIsAppleMobileWeb && globalThis.ProceduralSynthIsAppleMobileWeb();
          if (preferOscillatorDriver && innerState.ctx.createOscillator) {
            innerState.driver = innerState.ctx.createOscillator();
            var iosDriverGain = innerState.ctx.createGain();
            iosDriverGain.gain.value = 1.0;
            innerState.driver.frequency.value = 220;
            innerState.driver.connect(iosDriverGain);
            iosDriverGain.connect(innerState.node);
            innerState.driver.start();
          } else if (innerState.ctx.createConstantSource) {
            innerState.driver = innerState.ctx.createConstantSource();
            innerState.driver.offset.value = 0.000001;
            innerState.driver.connect(innerState.node);
            innerState.driver.start();
          } else if (innerState.ctx.createOscillator) {
            innerState.driver = innerState.ctx.createOscillator();
            var driverGain = innerState.ctx.createGain();
            driverGain.gain.value = 0.000001;
            innerState.driver.frequency.value = 20;
            innerState.driver.connect(driverGain);
            driverGain.connect(innerState.node);
            innerState.driver.start();
          }

          innerState.node.connect(innerState.master);

          if (preferMediaElementOutput && innerState.ctx.createMediaStreamDestination) {
            try {
              innerState.mediaDestination = innerState.ctx.createMediaStreamDestination();
              innerState.master.connect(innerState.mediaDestination);

              innerState.mediaElement = globalThis.document ? globalThis.document.createElement("audio") : null;
              if (innerState.mediaElement) {
                innerState.mediaElement.autoplay = true;
                innerState.mediaElement.playsInline = true;
                innerState.mediaElement.muted = false;
                innerState.mediaElement.volume = 1.0;
                innerState.mediaElement.srcObject = innerState.mediaDestination.stream;
              }
            } catch (mediaDestinationError) {
              innerState.mediaDestination = null;
              innerState.mediaElement = null;
            }
          }

          if (!innerState.mediaDestination) {
            innerState.master.connect(innerState.ctx.destination);
          }
        }

        if (globalThis.ProceduralSynthWarmupIosOutput) {
          globalThis.ProceduralSynthWarmupIosOutput(innerState.ctx);
        }

        if (preferMediaElementOutput && innerState.mediaElement && innerState.mediaElement.paused) {
          try {
            var mediaPlayPromise = innerState.mediaElement.play();
            if (mediaPlayPromise && typeof mediaPlayPromise.catch === "function") {
              mediaPlayPromise.catch(function() {
              });
            }
          } catch (mediaPlayError) {
          }
        }

        if (innerState.ctx.state === "suspended") {
          try {
            var resumePromise = innerState.ctx.resume();
            if (resumePromise && typeof resumePromise.then === "function") {
              resumePromise.then(function() {
                if (globalThis.ProceduralSynthWarmupIosOutput) {
                  globalThis.ProceduralSynthWarmupIosOutput(innerState.ctx);
                }
              });
            }
          } catch (e) {
          }
        }

        if (!innerState.debugLoggedUnlock && innerState.ctx) {
          innerState.debugLoggedUnlock = true;
          console.log("[ProceduralSynth] unlock state=", innerState.ctx.state, "sampleRate=", innerState.ctx.sampleRate);
        }
      };
    }

    if (!state.unlockListenersInstalled) {
      state.unlockListenersInstalled = true;
      var unlockHandler = function() {
        if (globalThis.ProceduralSynthEnsureAudioUnlocked) {
          globalThis.ProceduralSynthEnsureAudioUnlocked();
        }
      };
      var targets = [globalThis, globalThis.document, globalThis.document ? globalThis.document.body : null, globalThis.Module ? globalThis.Module.canvas : null];
      var events = ["pointerdown", "touchstart", "touchend", "mousedown", "click", "keydown"];
      for (var targetIndex = 0; targetIndex < targets.length; targetIndex++) {
        var target = targets[targetIndex];
        if (!target || !target.addEventListener) continue;
        for (var eventIndex = 0; eventIndex < events.length; eventIndex++) {
          target.addEventListener(events[eventIndex], unlockHandler, { passive: true, capture: true });
        }
      }
    }

    globalThis.ProceduralSynthEnsureAudioUnlocked();
  },
  PSW_IsAudioUnlocked: function() {
    if (!globalThis.ProceduralSynthState || !globalThis.ProceduralSynthState.ctx) return 0;
    return globalThis.ProceduralSynthState.ctx.state === "running" ? 1 : 0;
  },
  PSW_CreateVoice: function(sampleRate, channels) {
    if (!globalThis.ProceduralSynthVoices) {
      globalThis.ProceduralSynthVoices = {};
      globalThis.ProceduralSynthNextHandle = 1;
    }
    var handle = globalThis.ProceduralSynthNextHandle++;
    globalThis.ProceduralSynthVoices[handle] = {
      sampleRate: sampleRate,
      channels: channels,
      waveform: 0,
      harmonicCount: 0,
      useWavetable: false,
      useAudioClipADSR: false,
      active: false,
      level: 0.5,
      frequency: 0,
      detuneCents: 0,
      tremLfoFrequency: 0,
      vibLfoFrequency: 0,
      vibratoDepth: 5,
      attackMs: 5,
      decayMs: 5,
      sustainMs: 5,
      sustainLevel: 0.7,
      releaseMs: 5,
      attackUsesLogCurve: false,
      decayUsesLogCurve: false,
      sustainUsesLogCurve: false,
      releaseUsesLogCurve: false,
      fmMacroAmount: 0.35,
      fmMinRatio: 1,
      fmMaxRatio: 5,
      fmMaxIndex: 8,
      fmHighHarmonicBlend: 0.18,
      fmModFrequency: 220,
      fmModIndex: 1,
      samplingBaseFrequency: 440,
      samplingChannels: 1,
      samplingTotalFrames: 0,
      samplingStartFrame: 0,
      samplingEndFrame: 0,
      timeIndex: 0,
      samplingReadPosition: 0,
      releaseTriggered: false,
      releaseStartTimeIndex: 0,
      releaseStartLevel: 0,
      amplitudes: [],
      wavetable: [],
      samplingData: [],
      adsrData: []
    };
    if (globalThis.ProceduralSynthEnsureAudioUnlocked) {
      globalThis.ProceduralSynthEnsureAudioUnlocked();
    } else {
      var AudioCtx = globalThis.AudioContext || globalThis.webkitAudioContext;
      if (AudioCtx && !globalThis.ProceduralSynthState) {
        globalThis.ProceduralSynthState = {
          ctx: null,
          node: null,
          master: null,
          driver: null,
          unlockListenersInstalled: false
        };
      }
    }
    return handle;
  },
  PSW_DestroyVoice: function(handle) {
    if (globalThis.ProceduralSynthVoices) delete globalThis.ProceduralSynthVoices[handle];
  },
  PSW_SetCore: function(handle, waveform, level, detuneCents, tremLfoFrequency, vibLfoFrequency, vibratoDepth, attackMs, decayMs, sustainMs, sustainLevel, releaseMs) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.waveform = waveform; v.level = level; v.detuneCents = detuneCents;
    v.tremLfoFrequency = tremLfoFrequency; v.vibLfoFrequency = vibLfoFrequency; v.vibratoDepth = vibratoDepth;
    v.attackMs = attackMs; v.decayMs = decayMs; v.sustainMs = sustainMs; v.sustainLevel = sustainLevel; v.releaseMs = releaseMs;
  },
  PSW_SetFM: function(handle, fmMacroAmount, fmMinRatio, fmMaxRatio, fmMaxIndex, fmHighHarmonicBlend, fmModFrequency, fmModIndex) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.fmMacroAmount = fmMacroAmount; v.fmMinRatio = fmMinRatio; v.fmMaxRatio = fmMaxRatio; v.fmMaxIndex = fmMaxIndex;
    v.fmHighHarmonicBlend = fmHighHarmonicBlend; v.fmModFrequency = fmModFrequency; v.fmModIndex = fmModIndex;
  },
  PSW_SetFlags: function(handle, useWavetable, useAudioClipADSR) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.useWavetable = useWavetable !== 0;
    v.useAudioClipADSR = useAudioClipADSR !== 0;
  },
  PSW_SetAdsrCurveModes: function(handle, attackUsesLogCurve, decayUsesLogCurve, sustainUsesLogCurve, releaseUsesLogCurve) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.attackUsesLogCurve = attackUsesLogCurve !== 0;
    v.decayUsesLogCurve = decayUsesLogCurve !== 0;
    v.sustainUsesLogCurve = sustainUsesLogCurve !== 0;
    v.releaseUsesLogCurve = releaseUsesLogCurve !== 0;
  },
  PSW_SetHarmonics: function(handle, amplitudes, count) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.harmonicCount = count;
    v.amplitudes = Array.from(HEAPF32.subarray(amplitudes >> 2, (amplitudes >> 2) + count));
  },
  PSW_SetWavetable: function(handle, wavetable, count) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.wavetable = Array.from(HEAPF32.subarray(wavetable >> 2, (wavetable >> 2) + count));
  },
  PSW_SetSampling: function(handle, samplingData, sampleCount, channels, totalFrames, baseFrequency, startFrame, endFrame) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.samplingData = Array.from(HEAPF32.subarray(samplingData >> 2, (samplingData >> 2) + sampleCount));
    v.samplingChannels = channels;
    v.samplingTotalFrames = totalFrames;
    v.samplingBaseFrequency = baseFrequency;
    v.samplingStartFrame = startFrame;
    v.samplingEndFrame = endFrame;
  },
  PSW_SetADSRClip: function(handle, adsrData, count) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.adsrData = Array.from(HEAPF32.subarray(adsrData >> 2, (adsrData >> 2) + count));
  },
  PSW_NoteOn: function(handle, frequency) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.frequency = frequency;
    v.timeIndex = 0;
    v.samplingReadPosition = v.samplingStartFrame;
    v.active = true;
    v.releaseTriggered = false;
    v.releaseStartTimeIndex = 0;
    v.releaseStartLevel = 0;
    console.log("[ProceduralSynth] NoteOn", "handle=", handle, "frequency=", frequency);
  },
  PSW_NoteOff: function(handle) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    if (!v.active || v.releaseTriggered) return;
    function curve01(progress, useLogCurve) {
      progress = Math.max(0, Math.min(1, progress));
      if (!useLogCurve) return progress;
      return Math.log10(1 + (9 * progress));
    }
    function scheduledAdsr(voice, t) {
      if (voice.useAudioClipADSR && voice.adsrData.length > 0) return (t >= 0 && t < voice.adsrData.length) ? Math.max(0, Math.min(1, voice.adsrData[t])) : 0;
      var attack = Math.max(1, Math.round((voice.attackMs / 1000) * voice.sampleRate));
      var decay = attack + Math.round((voice.decayMs / 1000) * voice.sampleRate);
      var sustain = decay + Math.round((voice.sustainMs / 1000) * voice.sampleRate);
      var release = sustain + Math.round((voice.releaseMs / 1000) * voice.sampleRate);
      if (t < attack) return curve01(t / attack, voice.attackUsesLogCurve);
      if (t < decay) return 1 + ((voice.sustainLevel - 1) * curve01((t - attack) / Math.max(1, decay - attack), voice.decayUsesLogCurve));
      if (t < sustain) return voice.sustainLevel;
      if (t < release) return voice.sustainLevel + ((0.0000001 - voice.sustainLevel) * curve01((t - sustain) / Math.max(1, release - sustain), voice.releaseUsesLogCurve));
      return 0.0000001;
    }
    v.releaseStartTimeIndex = v.timeIndex;
    v.releaseStartLevel = scheduledAdsr(v, v.timeIndex);
    v.releaseTriggered = true;
    console.log("[ProceduralSynth] NoteOff", "handle=", handle);
  },
  PSW_Render: function(handle, dataPtr, sampleCount) {
    var v = globalThis.ProceduralSynthVoices[handle];
    var out = HEAPF32.subarray(dataPtr >> 2, (dataPtr >> 2) + sampleCount);
    if (globalThis.ProceduralSynthState && globalThis.ProceduralSynthState.ctx) {
      out.fill(0);
      return;
    }
    if (!v || !v.active || v.frequency <= 0) {
      out.fill(0);
      return;
    }
    var ch = Math.max(1, v.channels);
    function sine(f, t) { return Math.sin(2 * Math.PI * f * t / v.sampleRate); }
    function curve01(progress, useLogCurve) {
      progress = Math.max(0, Math.min(1, progress));
      if (!useLogCurve) return progress;
      return Math.log10(1 + (9 * progress));
    }
    function releaseFrames() {
      return Math.max(1, Math.round((v.releaseMs / 1000) * v.sampleRate));
    }
    function naturalReleaseEnd() {
      var attack = Math.max(1, Math.round((v.attackMs / 1000) * v.sampleRate));
      var decay = attack + Math.round((v.decayMs / 1000) * v.sampleRate);
      var sustain = decay + Math.round((v.sustainMs / 1000) * v.sampleRate);
      return sustain + releaseFrames();
    }
    function scheduledAdsr(voice, t) {
      if (voice.useAudioClipADSR && voice.adsrData.length > 0) return (t >= 0 && t < voice.adsrData.length) ? Math.max(0, Math.min(1, voice.adsrData[t])) : 0;
      var attack = Math.max(1, Math.round((voice.attackMs / 1000) * voice.sampleRate));
      var decay = attack + Math.round((voice.decayMs / 1000) * voice.sampleRate);
      var sustain = decay + Math.round((voice.sustainMs / 1000) * voice.sampleRate);
      var release = sustain + Math.round((voice.releaseMs / 1000) * voice.sampleRate);
      if (t < attack) return curve01(t / attack, voice.attackUsesLogCurve);
      if (t < decay) return 1 + ((voice.sustainLevel - 1) * curve01((t - attack) / Math.max(1, decay - attack), voice.decayUsesLogCurve));
      if (t < sustain) return voice.sustainLevel;
      if (t < release) return voice.sustainLevel + ((0.0000001 - voice.sustainLevel) * curve01((t - sustain) / Math.max(1, release - sustain), voice.releaseUsesLogCurve));
      return 0.0000001;
    }
    function adsr(t) {
      if (v.releaseTriggered && t >= v.releaseStartTimeIndex) {
        return v.releaseStartLevel + ((0.0000001 - v.releaseStartLevel) * curve01((t - v.releaseStartTimeIndex) / releaseFrames(), v.releaseUsesLogCurve));
      }
      return scheduledAdsr(v, t);
    }
    function wavetable(time, freq) {
      if (!v.wavetable.length) return 0;
      var idx = Math.round((time * freq / v.sampleRate) * v.wavetable.length) % v.wavetable.length;
      if (idx < 0) idx += v.wavetable.length;
      return v.wavetable[idx];
    }
    function additive(freq, time) {
      if (v.harmonicCount <= 0 || !v.amplitudes.length) return 0;
      var limit = Math.min(v.harmonicCount, v.amplitudes.length), sum = 0;
      for (var h = 1; h <= limit; h++) sum += Math.sin(2 * Math.PI * h * freq * time / v.sampleRate) * v.amplitudes[h - 1];
      return sum / Math.max(1, limit);
    }
    function fm(freq, time) {
      var amount = Math.max(0, Math.min(1, v.fmMacroAmount));
      var ratio = v.fmMinRatio + (v.fmMaxRatio - v.fmMinRatio) * amount;
      var modIndex = v.fmMaxIndex * Math.max(0.05, v.fmModIndex) * amount;
      var modFreq = Math.min(freq * ratio, v.sampleRate * 0.45);
      var blend = v.fmHighHarmonicBlend * amount;
      var carrierPhase = 2 * Math.PI * freq * time / v.sampleRate;
      var modPhase = 2 * Math.PI * modFreq * time / v.sampleRate;
      var modulation = Math.sin(modPhase);
      var primary = Math.sin(carrierPhase + modIndex * modulation);
      var bright = Math.sin((carrierPhase * 2) + (modIndex * 0.5 * modulation));
      return primary + (0.18 * blend * (bright - primary));
    }
    function sampling(freq, advance) {
      if (!v.samplingData.length || v.samplingTotalFrames <= 0) return 0;
      var startFrame = Math.max(0, Math.min(v.samplingStartFrame, Math.max(0, v.samplingTotalFrames - 1)));
      var endFrame = Math.max(startFrame + 1, Math.min(v.samplingEndFrame, Math.max(0, v.samplingTotalFrames - 1)));
      if (endFrame <= startFrame) return 0;
      var pos = v.samplingReadPosition;
      if (pos >= endFrame) return 0;
      var baseFreq = Math.max(1, v.samplingBaseFrequency);
      var ratio = freq / baseFreq;
      var frame0 = Math.max(startFrame, Math.min(Math.floor(pos), endFrame));
      var frame1 = Math.max(startFrame, Math.min(frame0 + 1, endFrame));
      var frac = pos - frame0;
      function frame(frame) {
        var channels = Math.max(1, v.samplingChannels);
        var base = frame * channels;
        if (base < 0 || base >= v.samplingData.length) return 0;
        if (channels === 1) return v.samplingData[base];
        var sum = 0;
        for (var c = 0; c < channels; c++) sum += v.samplingData[base + c] || 0;
        return sum / channels;
      }
      var result = frame(frame0) + ((frame(frame1) - frame(frame0)) * frac);
      if (advance) v.samplingReadPosition += ratio;
      return result;
    }
    function gen(freq, time, advanceSampling) {
      if (v.waveform !== 7 && freq <= 0) return 0;
      if (v.useWavetable && v.wavetable.length && v.waveform !== 5) return wavetable(time, freq);
      switch (v.waveform) {
        case 0: return sine(freq, time);
        case 1: return sine(freq, time) >= 0 ? 1 : -1;
        case 2: return 1 + (-2 * ((time % (v.sampleRate / freq)) / Math.max(1, v.sampleRate / freq)));
        case 3: {
          var period = v.sampleRate / freq, pos = time % period, q = period * 0.25, tq = period * 0.75;
          if (pos <= q) return pos / Math.max(1, q);
          if (pos <= tq) return 1 - 2 * ((pos - q) / Math.max(1, tq - q));
          return -1 + ((pos - tq) / Math.max(1, period - tq));
        }
        case 4: return additive(freq, time);
        case 5: return fm(freq, time);
        case 6: return sampling(freq, advanceSampling);
        case 7: return (Math.random() * 2) - 1;
        default: return 0;
      }
    }
    for (var i = 0; i < sampleCount; i += ch) {
      var t = v.timeIndex;
      var env = adsr(t);
      var trem = v.tremLfoFrequency > 0 ? 0.5 + 0.5 * sine(v.tremLfoFrequency, t) : 1;
      var vib = v.vibLfoFrequency > 0 ? v.vibratoDepth * sine(v.vibLfoFrequency, t) : 0;
      var currentFreq = Math.max(0, v.frequency + vib);
      var detunedFreq = currentFreq * Math.pow(2, v.detuneCents / 1200);
      var sample = v.level * 0.5 * (gen(currentFreq, t, true) + gen(detunedFreq, t, false)) * env * trem;
      out[i] = sample;
      for (var c = 1; c < ch; c++) out[i + c] = sample;
      v.timeIndex++;
      var finished = v.releaseTriggered
        ? (t - v.releaseStartTimeIndex) >= releaseFrames()
        : t >= naturalReleaseEnd();
      if (finished) {
        v.active = false;
        v.frequency = 0;
        v.samplingReadPosition = v.samplingStartFrame;
        v.releaseTriggered = false;
        out.fill(0, i + ch);
        break;
      }
    }
  }
});
