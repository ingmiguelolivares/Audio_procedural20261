mergeInto(LibraryManager.library, {
  PSW_UnlockAudio: function() {
    if (!globalThis.ProceduralSynthState) {
      globalThis.ProceduralSynthState = {
        ctx: null,
        node: null,
        master: null,
        driver: null,
        didLogAudioProcess: false,
        callbackCount: 0,
        debugCallbacksRemaining: 0,
        listenersInstalled: false
      };
    }

    var state = globalThis.ProceduralSynthState;
    var AudioCtx = globalThis.AudioContext || globalThis.webkitAudioContext;
    if (!AudioCtx) return;

    if (!globalThis.ProceduralSynthEnsureAudioUnlocked) {
      globalThis.ProceduralSynthEnsureAudioUnlocked = function() {
        if (!globalThis.ProceduralSynthState) return;
        var innerState = globalThis.ProceduralSynthState;
        var InnerAudioCtx = globalThis.AudioContext || globalThis.webkitAudioContext;
        if (!InnerAudioCtx) return;

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

            if (!innerState.didLogAudioProcess) {
              innerState.didLogAudioProcess = true;
              console.log("[ProceduralSynth] ScriptProcessor active");
            }

            var voices = globalThis.ProceduralSynthVoices || {};
            var handles = Object.keys(voices);
            var activeVoiceCount = 0;
            var peak = 0;

            function sine(v, f, t) { return Math.sin(2 * Math.PI * f * t / v.sampleRate); }
            function adsr(v, t) {
              if (v.useAudioClipADSR && v.adsrData.length > 0) return (t >= 0 && t < v.adsrData.length) ? Math.max(0, Math.min(1, v.adsrData[t])) : 0;
              var attack = Math.max(1, Math.round((v.attackMs / 1000) * v.sampleRate));
              var decay = attack + Math.round((v.decayMs / 1000) * v.sampleRate);
              var sustain = decay + Math.round((v.sustainMs / 1000) * v.sampleRate);
              var release = sustain + Math.round((v.releaseMs / 1000) * v.sampleRate);
              if (t < attack) return t / attack;
              if (t < decay) return 1 + ((v.sustainLevel - 1) * ((t - attack) / Math.max(1, decay - attack)));
              if (t < sustain) return v.sustainLevel;
              if (t < release) return v.sustainLevel + ((0.0000001 - v.sustainLevel) * ((t - sustain) / Math.max(1, release - sustain)));
              return 0.0000001;
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

            for (var sampleIndex = 0; sampleIndex < left.length; sampleIndex++) {
              var mixed = 0;
              for (var handleIndex = 0; handleIndex < handles.length; handleIndex++) {
                var v = voices[handles[handleIndex]];
                if (!v || !v.active || v.frequency <= 0 || v.level <= 0) continue;
                activeVoiceCount++;

                var t = v.timeIndex;
                var env = adsr(v, t);
                var trem = v.tremLfoFrequency > 0 ? 0.5 + 0.5 * sine(v, v.tremLfoFrequency, t) : 1;
                var vib = v.vibLfoFrequency > 0 ? v.vibratoDepth * sine(v, v.vibLfoFrequency, t) : 0;
                var currentFreq = Math.max(0, v.frequency + vib);
                var detunedFreq = currentFreq * Math.pow(2, v.detuneCents / 1200);
                mixed += v.level * 0.5 * (gen(v, currentFreq, t, true) + gen(v, detunedFreq, t, false)) * env * trem;
                v.timeIndex++;
              }
              left[sampleIndex] = mixed;
              if (right !== left) right[sampleIndex] = mixed;
              var absMixed = Math.abs(mixed);
              if (absMixed > peak) peak = absMixed;
            }

            innerState.callbackCount++;
            if (innerState.debugCallbacksRemaining > 0) {
              innerState.debugCallbacksRemaining--;
              var snapshot = [];
              for (var debugIndex = 0; debugIndex < handles.length; debugIndex++) {
                var debugVoice = voices[handles[debugIndex]];
                if (!debugVoice) continue;
                snapshot.push(
                  handles[debugIndex] +
                  "{active=" + !!debugVoice.active +
                  ",freq=" + debugVoice.frequency +
                  ",level=" + debugVoice.level +
                  ",time=" + debugVoice.timeIndex + "}"
                );
              }
              console.log("[ProceduralSynth] DebugVoices activeVoices=" + activeVoiceCount + " peak=" + peak.toFixed(6) + " " + snapshot.join(" "));
            }

            if ((innerState.callbackCount % 60) === 0) {
              console.log("[ProceduralSynth] MixStats activeVoices=" + activeVoiceCount + " peak=" + peak.toFixed(6));
            }
          };

          if (innerState.ctx.createConstantSource) {
            innerState.driver = innerState.ctx.createConstantSource();
            innerState.driver.offset.value = 0;
            innerState.driver.connect(innerState.node);
            innerState.driver.start();
          }

          innerState.node.connect(innerState.master);
          innerState.master.connect(innerState.ctx.destination);
          console.log("[ProceduralSynth] WebAudio graph created");
        }

        if (innerState.ctx.state === "suspended") {
          innerState.ctx.resume();
        }
      };
    }

    if (!state.listenersInstalled) {
      state.listenersInstalled = true;
      var unlockHandler = function() {
        if (globalThis.ProceduralSynthEnsureAudioUnlocked) {
          globalThis.ProceduralSynthEnsureAudioUnlocked();
        }
      };
      globalThis.addEventListener("pointerdown", unlockHandler, { passive: true });
      globalThis.addEventListener("touchend", unlockHandler, { passive: true });
      globalThis.addEventListener("keydown", unlockHandler, { passive: true });
      globalThis.addEventListener("click", unlockHandler, { passive: true });
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
      amplitudes: [],
      wavetable: [],
      samplingData: [],
      adsrData: []
    };
    console.log("[ProceduralSynth] CreateVoice", handle, "sr=", sampleRate, "ch=", channels);
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
          didLogAudioProcess: false,
          callbackCount: 0,
          debugCallbacksRemaining: 0,
          listenersInstalled: false
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
    console.log("[ProceduralSynth] NoteOn", handle, frequency, "level=", v.level, "wave=", v.waveform);
    if (globalThis.ProceduralSynthState) {
      globalThis.ProceduralSynthState.debugCallbacksRemaining = 8;
    }
  },
  PSW_NoteOff: function(handle) {
    var v = globalThis.ProceduralSynthVoices[handle];
    if (!v) return;
    v.active = false;
    v.frequency = 0;
    v.timeIndex = 0;
    v.samplingReadPosition = v.samplingStartFrame;
    console.log("[ProceduralSynth] NoteOff", handle);
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
    function adsr(t) {
      if (v.useAudioClipADSR && v.adsrData.length > 0) return (t >= 0 && t < v.adsrData.length) ? Math.max(0, Math.min(1, v.adsrData[t])) : 0;
      var attack = Math.max(1, Math.round((v.attackMs / 1000) * v.sampleRate));
      var decay = attack + Math.round((v.decayMs / 1000) * v.sampleRate);
      var sustain = decay + Math.round((v.sustainMs / 1000) * v.sampleRate);
      var release = sustain + Math.round((v.releaseMs / 1000) * v.sampleRate);
      if (t < attack) return t / attack;
      if (t < decay) return 1 + ((v.sustainLevel - 1) * ((t - attack) / Math.max(1, decay - attack)));
      if (t < sustain) return v.sustainLevel;
      if (t < release) return v.sustainLevel + ((0.0000001 - v.sustainLevel) * ((t - sustain) / Math.max(1, release - sustain)));
      return 0.0000001;
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
    }
  }
});
