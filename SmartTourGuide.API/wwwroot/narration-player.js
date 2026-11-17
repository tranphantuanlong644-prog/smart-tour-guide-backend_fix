const measuredNarrationRates = new Map();

function setupNarrationPlayer(text, lang, autoPlay) {
 const labels = {
  vi: ['Tốc độ đọc', 'Tiến độ thuyết minh', 'Tạm dừng', 'Nghe lại từ đầu'],
  en: ['Playback speed', 'Narration progress', 'Pause', 'Replay from start'],
  'zh-CN': ['播放速度', '讲解进度', '暂停', '从头播放'],
  'zh-HK': ['播放速度', '導覽進度', '暫停', '從頭播放'],
  es: ['Velocidad', 'Progreso de la narración', 'Pausar', 'Repetir desde el inicio']
 }[lang] || ['Playback speed', 'Narration progress', 'Pause', 'Replay from start'];
 const play = document.querySelector('#replay-narration');
 const status = document.querySelector('#detail-voice-status');
 let audio = document.querySelector('#location-audio');
 status.insertAdjacentHTML('beforebegin', `
  <div id="speech-timeline">
   <label for="speech-position">${labels[1]}</label>
   <input id="speech-position" type="range" min="0" step="0.01" value="0">
   
  </div>
  <span id="speech-time"></span><label for="narration-rate">${labels[0]}</label>
  <select id="narration-rate">${[0.75,1,1.25,2].map(n=>`<option value="${n}" ${n===1?'selected':''}>${n}×</option>`).join('')}</select>`);
 const slider = document.querySelector('#speech-position');
 const time = document.querySelector('#speech-time');
 const rate = document.querySelector('#narration-rate');
 // Native audio has its own accurate, seekable timeline. Device speech exposes text offsets.
 const words = typeof Intl.Segmenter === 'function'
  ? Array.from(new Intl.Segmenter(lang,{granularity:'word'}).segment(text)).filter(part=>part.isWordLike)
  : Array.from(text.matchAll(/[\u3400-\u9fff]|[^\s\u3400-\u9fff]+/gu));
 const offsets = words.map(word=>word.index);
 if (!offsets.length) offsets.push(0);
 slider.max = offsets.length;
 let position = 0, frame = 0, utterance = null, disposed = false;
 let confirmedWord = null;
 let listened = 0, startedAt = null;
 const listenedLabel = {vi:'Đã nghe',en:'Listening time','zh-CN':'已收听','zh-HK':'已收聽',es:'Tiempo escuchado'}[lang] || 'Listening time';
 const format = seconds => Math.floor(seconds/60)+':'+String(Math.floor(seconds%60)).padStart(2,'0');
 function updateTime() {
  if (audio) {
   if (!Number.isFinite(audio.duration) || audio.duration <= 0) {
    slider.disabled = true;
    time.textContent = '0:00 / —';
    return;
   }
   slider.disabled = false;
   slider.max = audio.duration;
   slider.value = audio.ended ? audio.duration : audio.currentTime;
   const label = format(Number(slider.value))+' / '+format(audio.duration);
   if (time.textContent !== label) time.textContent = label;
   slider.setAttribute('aria-valuetext',label);
  } else {
   const elapsed = listened + (startedAt === null ? 0 : (performance.now()-startedAt)/1000);
   const label = listenedLabel+' '+format(elapsed);
   if (time.textContent !== label) time.textContent = label;
  }
 }
 function finishClock(segmentSeconds) {
  if (startedAt !== null) listened += segmentSeconds ?? (performance.now()-startedAt)/1000;
  startedAt = null;
  cancelAnimationFrame(frame);
 }
 function display() {
  if (audio) {
   updateTime();
   play.textContent = audio.ended ? labels[3] : audio.paused ? ui(lang,'speak') : labels[2];
   return;
  }
  slider.value = position;
  updateTime();
  slider.setAttribute('aria-valuetext',Math.round(100*position/offsets.length)+'%');
  play.textContent = audio || position >= offsets.length ? labels[3] : utterance ? labels[2] : ui(lang,'speak');
 }
 function stopSpeech() {
  const wasSpeaking = !!utterance;
  utterance = null;
  finishClock();
  if (wasSpeaking) window.speechSynthesis.cancel();
 }
 function startSpeech() {
  stopSpeech();
  if (position >= offsets.length) { position = 0; listened = 0; }
  const first = Math.min(offsets.length-1, Math.floor(position));
  const start = first === 0 ? 0 : offsets[first];
  const speed = Number(rate.value);
  const speech = speak(text.slice(start),lang,speed);
  if (!speech) { display(); return; }
  utterance = speech;
  confirmedWord = null;
  let anchor = first, anchorAt, previousFrame;
  const rateKey = JSON.stringify([lang,speech.voice?.voiceURI,text,speed]);
  let wordsPerSecond = measuredNarrationRates.get(rateKey) ?? 2.6*speed;
  const synthesis = window.speechSynthesis;
  let observedSpeaking = false;
  function completeSpeech(event) {
   if (utterance !== speech) return;
   const wallSeconds = startedAt === null ? 0 : (performance.now()-startedAt)/1000;
   // An end event can be delivered after the engine's recorded end time.
   const engineSeconds = event?.elapsedTime;
   const seconds = engineSeconds > 0 && engineSeconds <= wallSeconds + 0.1
    ? engineSeconds : wallSeconds;
   if (first === 0 && seconds > 0) measuredNarrationRates.set(rateKey,offsets.length/seconds);
   finishClock(seconds);
   utterance = null;
   position = offsets.length;
   display();
  }
  function animate(now) {
   if (utterance !== speech) return;
   if (synthesis.speaking) observedSpeaking = true;
   else if (observedSpeaking && !synthesis.pending && !synthesis.paused) {
    completeSpeech();
    return;
   }
   const limit = confirmedWord === null ? offsets.length-0.01 : confirmedWord+0.99;
   const target = Math.min(limit,anchor+Math.max(0,now-anchorAt)*wordsPerSecond/1000);
   const blend = 1-Math.exp(-Math.max(0,now-previousFrame)/65);
   position += Math.max(0,target-position)*blend;
   previousFrame = now;
   slider.value = position;
   const progress = Math.floor(100*position/offsets.length)+'%';
   if (slider.getAttribute('aria-valuetext') !== progress) slider.setAttribute('aria-valuetext',progress);
   updateTime();
   frame = requestAnimationFrame(animate);
  }
  speech.onboundary = event => {
   if (utterance !== speech || event.name !== 'word') return;
   const index = offsets.indexOf(start + event.charIndex);
   if (index < anchor) return;
   const now = performance.now();
   if (index > anchor && now-anchorAt > 30) {
    const observed = (index-anchor)*1000/(now-anchorAt);
    wordsPerSecond = 0.65*wordsPerSecond + 0.35*Math.min(10*speed,observed);
   }
   confirmedWord = anchor = index;
   anchorAt = now;
  };
  speech.addEventListener('start',()=>{
   if (utterance !== speech) return;
   observedSpeaking = synthesis.speaking;
   previousFrame = anchorAt = startedAt = performance.now();
   frame = requestAnimationFrame(animate);
  });
  speech.addEventListener('end',completeSpeech);
  speech.addEventListener('error',()=>{
   if (utterance !== speech) return;
   finishClock();
   utterance = null;
   display();
  });
  display();
 }
 let resumeAfterSeek = false;
 slider.onkeydown = event => {
  const direction = {ArrowRight:1,ArrowUp:1,ArrowLeft:-1,ArrowDown:-1}[event.key];
  if (!direction) return;
  event.preventDefault();
  if (audio) {
   audio.currentTime = Math.max(0,Math.min(audio.duration,audio.currentTime+direction));
   updateTime();
   return;
  }
  slider.value = Math.max(0,Math.min(offsets.length,Math.round(position)+direction));
  slider.oninput();
  slider.onchange();
 };
 slider.oninput = () => {
  if (audio) { audio.currentTime = Number(slider.value); updateTime(); return; }
  resumeAfterSeek ||= !!utterance;
  stopSpeech();
  position = Math.round(Number(slider.value));
  if (!position) listened = 0;
  display();
 };
 slider.onchange = () => {
  if (resumeAfterSeek && position < offsets.length) startSpeech();
  resumeAfterSeek = false;
 };
 rate.onchange = () => {
  if (audio) { audio.playbackRate = Number(rate.value); updateTime(); }
  else if (utterance) {
   position = confirmedWord ?? Math.floor(position);
   startSpeech();
  }
 };
 function useSpeech() {
  if (!audio || disposed) return;
  audio.onerror = null;
  audio.pause();
  audio.remove();
  audio = null;
  cancelAnimationFrame(frame);
  slider.disabled = false;
  slider.step = '0.01';
  slider.max = offsets.length;
  status.textContent = ui(lang,'audioFallback');
  display();
 }
 function playAudio() {
  if (!audio.paused && !audio.ended) { audio.pause(); return; }
  if (audio.ended) audio.currentTime = 0;
  audio.playbackRate = Number(rate.value);
  audio.play().catch(error=>{
   if (disposed) return;
   if (error.name === 'NotAllowedError') status.textContent = ui(lang,'pressPlay');
   else if (error.name !== 'AbortError') { useSpeech(); if (!utterance) startSpeech(); }
  });
 }
 if (audio) {
  audio.hidden = true;
  slider.step = 'any';
  function trackRecording() {
   cancelAnimationFrame(frame);
   if (!audio || disposed) return;
   display();
   if (!audio.paused && !audio.ended) frame = requestAnimationFrame(trackRecording);
  }
  audio.onerror = useSpeech;
  audio.onplaying = () => {status.textContent = ui(lang,'playingAudio'); trackRecording();};
  audio.onpause = audio.onended = trackRecording;
  audio.onratechange = () => { if (audio) rate.value = String(audio.playbackRate); };
  for (const event of ['loadedmetadata','durationchange','timeupdate','seeked','ratechange']) audio.addEventListener(event,updateTime);
  if (audio.error) useSpeech();
 }
 play.onclick = () => {
  if (audio) playAudio();
  else if (utterance) { stopSpeech(); display(); }
  else startSpeech();
 };
 display();
 if (autoPlay) play.click();
 return () => {
  disposed = true;
  stopSpeech();
  if (audio) { audio.onerror = null; audio.pause(); }
 };
}
