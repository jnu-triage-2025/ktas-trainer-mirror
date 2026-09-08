const $ = id => document.getElementById(id);
const token = location.hash.slice(1) || sessionStorage.getItem('e2e-token');
if (location.hash) { sessionStorage.setItem('e2e-token', token); history.replaceState(null, '', '/'); }
let epoch = 0, generation = 0, instances = [], operationId = null;
let selected = '', human = false, recording = null, runId = null, cursor = 0, imageWidth = 0, imageHeight = 0, imageFrame = 0;
let capturing = false, selectionVersion = 0, captureStarted = 0, captureTimes = [];
const held = new Set();
async function call(tool, args = {}) {
  if (!token) throw new Error('인증 정보가 없습니다. 콘솔 시작 시 제공된 인증 링크로 다시 여세요.');
  const response = await fetch('/api', { method: 'POST', headers: { Authorization: `Bearer ${token}`, 'Content-Type':'application/json' },
    body: JSON.stringify({ tool, args: { instanceId: selected, controlEpoch: epoch, ...args } }) });
  if (response.status === 401) throw new Error('인증 정보가 만료됐거나 올바르지 않습니다. 새 인증 링크로 다시 여세요.');
  if (response.status === 403) throw new Error('이 요청을 수행할 권한이 없거나 허용되지 않은 콘솔 주소입니다. 관찰용 자격으로는 게임을 제어할 수 없습니다.');
  const value = await response.json().catch(() => undefined);
  if (!response.ok || !value?.ok) throw new Error(value?.error?.code ?? `콘솔 요청에 실패했습니다. HTTP ${response.status}`);
  return value.result;
}
const logEntries = [];
function renderLogs() {
  const filter = $('log-filter').value, severity = $('severity').value;
  $('logs').replaceChildren();
  for (const text of logEntries) {
    if ((filter && !text.includes(filter)) || (severity && !text.includes(severity))) continue;
    const p = document.createElement('pre'); p.textContent = text; $('logs').append(p);
  }
  $('logs').scrollTop = $('logs').scrollHeight;
}
function log(text) { logEntries.push(text); if (logEntries.length > 300) logEntries.shift(); renderLogs(); }
$('log-filter').oninput = renderLogs; $('severity').onchange = renderLogs;
let inputChain = Promise.resolve();
function input(tool, args = {}) {
  const captured = { instanceId: selected, controlEpoch: epoch, ...args };
  const issuedGeneration = generation;
  const task = inputChain.then(() => issuedGeneration === generation ? call(tool, captured) : undefined);
  inputChain = task.catch(() => {});
  return task;
}
function action(id, fn) { $(id).onclick = () => Promise.resolve().then(fn).catch(error => { log(String(error)); $('status').textContent = String(error); }); }
let assistanceRenderKey = '';
async function refreshAssistance() {
  const id = selected, processRun = instances.find(instance => instance.instanceId === id)?.runId;
  const requests = await call('assistance.list', processRun ? {runId:processRun} : {});
  if (selected !== id) return;
  const renderKey = JSON.stringify([id,requests]);
  if (renderKey === assistanceRenderKey) return;
  assistanceRenderKey = renderKey;
  $('assistance-list').replaceChildren();
  const labels = {queued:'AI 수신 대기',running:'AI 처리 중',completed:'AI 완료 보고',failed:'실패',cancelled:'요청 취소됨'};
  for (const request of requests.slice(-20).reverse()) {
    const entry = document.createElement('pre');
    entry.textContent = `${labels[request.state] ?? request.state} · ${request.instanceId.slice(-7)}\n${request.prompt}${request.result ? '\n'+request.result : ''}\n실행: ${request.runId}`;
    $('assistance-list').append(entry);
    if (request.state === 'queued' || request.state === 'running') {
      const cancel = document.createElement('button');cancel.type = 'button';cancel.textContent = '요청 취소';
      cancel.onclick = async () => {
        cancel.disabled = true;
        try {
          await call('assistance.update',{instanceId:request.instanceId,requestId:request.id,revision:request.revision,state:'cancelled'});
          $('assistance-status').textContent = '요청을 취소했습니다. 연결된 E2E 실행에도 중단을 요청했습니다.';
          await refreshAssistance();
        } catch (error) { $('assistance-status').textContent = String(error);log(String(error));await refreshAssistance(); }
        finally { cancel.disabled = false; }
      };
      $('assistance-list').append(cancel);
    }
  }
}
action('assistance-refresh', refreshAssistance);
action('assistance-submit', async () => {
  if (!selected) throw new Error('먼저 실행 중인 플레이어를 선택하세요.');
  const prompt = $('assistance-prompt').value.trim();
  if (!prompt) throw new Error('수행할 작업을 입력하세요.');
  $('assistance-submit').disabled = true;
  try {
    await call('assistance.submit', {prompt});
    $('assistance-prompt').value = '';
    $('assistance-status').textContent = '요청을 등록했습니다. 연결된 AI가 받을 때까지 대기합니다.';
    await refreshAssistance();
  } finally { $('assistance-submit').disabled = false; }
});
async function refresh() {
  instances = await call('instances.list'); $('instances').replaceChildren();
  for (const i of instances) { const option = document.createElement('option'); option.value = i.instanceId; option.textContent = `${i.role} · ${i.instanceId.slice(-7)} · ${i.state}`; $('instances').append(option); }
  const active = instances.filter(i => !['EXITED','CRASHED','START_FAILED'].includes(i.state));
  if (!active.some(i => i.instanceId === selected)) {
    selectionVersion++; captureTimes = []; imageWidth = imageHeight = imageFrame = 0;
    selected = active[0]?.instanceId ?? ''; human = false; cursor = 0;
    $('screen').removeAttribute('src'); $('empty').hidden = false;
    $('stream-status').textContent = selected ? '화면 수신 대기 중 · 목표 12 FPS' : '실행 중인 화면이 없습니다.';
    $('raw-state').textContent = ''; $('state').textContent = selected ? '관측 대기 중' : '실행 중인 인스턴스가 없습니다.';
  }
  $('instances').value = selected;
  $('status').textContent = `${instances.length}개 인스턴스`;
  $('thumbnails').replaceChildren();
  for (const i of instances.filter(i => i.role !== 'dedicated' && !['EXITED','CRASHED','START_FAILED'].includes(i.state))) {
    const button = document.createElement('button'), img = document.createElement('img'), label = document.createElement('span');
    img.alt = `${i.role} ${i.instanceId.slice(-7)} 화면`; img.dataset.instance = i.instanceId;
    label.textContent = `${i.role} · ${i.instanceId.slice(-7)}`; button.append(img,label);
    button.onclick = () => select(i.instanceId).catch(error => log(String(error))); $('thumbnails').append(button);
  }
}
async function capture() {
  if (!selected || capturing) return;
  capturing = true; captureStarted = Date.now();
  const id = selected, version = selectionVersion;
  try {
    const shot = await call('game.screenshot',{instanceId:id,ttlMs:1500});
    if (id !== selected || version !== selectionVersion || shot.frame <= imageFrame) return;
    const decoded = document.createElement('img'); decoded.src = `data:${shot.mimeType};base64,${shot.data}`;
    await decoded.decode();
    if (id !== selected || version !== selectionVersion) return;
    $('screen').src = decoded.src; imageWidth = shot.width; imageHeight = shot.height; imageFrame = shot.frame;
    $('empty').hidden = true;
    const now = Date.now(); captureTimes = captureTimes.filter(time => now - time < 3000); captureTimes.push(now);
    const fps = captureTimes.length > 1 ? (captureTimes.length - 1) * 1000 / (now - captureTimes[0]) : 0;
    $('stream-status').textContent = `화면 ${fps.toFixed(1)} FPS · 수신·해독 ${now-captureStarted}ms · 목표 12 FPS`;
  } finally { capturing = false; }
}
async function release() { generation++; held.clear(); lookX = lookY = 0; pendingPointer = null; await inputChain; if (human && selected) await call('input.release_all'); }
async function select(id) { await release().catch(error => log(String(error))); selectionVersion++; captureTimes=[]; imageWidth=imageHeight=imageFrame=0; selected = id; $('screen').removeAttribute('src'); $('empty').hidden=false; $('stream-status').textContent='화면 수신 대기 중 · 목표 12 FPS'; $('instances').value = id; human = false; cursor = 0; await capture(); }
$('instances').onchange = () => select($('instances').value).catch(error => log(String(error)));
action('refresh', refresh); action('capture', capture);
action('acquire', async () => { const issuedGeneration = generation; const control = await call('control.acquire'); if (issuedGeneration !== generation) return; epoch = control.controlEpoch; human = true; $('screen').focus(); });
action('return', async () => { await release(); await call('control.handoff', { owner: 'Automation' }); human = false; });
action('release', async () => { generation++; held.clear(); lookX=lookY=0; pendingPointer=null; human=false; await call('control.emergency_stop'); });
function cameraError(error) {
  const reason = error?.name ?? 'PointerLockError';
  $('camera-status').textContent = reason === 'WrongDocumentError'
    ? '카메라 잠금을 시작하지 못했습니다. 콘솔 브라우저 창과 탭을 앞으로 가져온 뒤 카메라 조작을 다시 누르세요.'
    : `카메라 잠금을 시작하지 못했습니다 (${reason}). 브라우저의 포인터 잠금 허용 상태를 확인하세요.`;
  log(JSON.stringify({eventType:'console.pointer_lock_failed',reason,documentFocused:document.hasFocus(),visibility:document.visibilityState}));
}
$('lock').onclick = () => {
  if (!human) { $('camera-status').textContent = '먼저 제어권을 받으세요.'; return; }
  $('camera-status').textContent = '카메라 잠금을 요청하고 있습니다.';
  try {
    // Invoke during the user gesture; older browsers report failure only by event.
    const pending = $('screen').requestPointerLock();
    if (pending?.catch) pending.catch(cameraError);
  } catch (error) { cameraError(error); }
};
action('launch', async () => { log(JSON.stringify(await call('instances.launch', { buildId: $('build').value, topology: $('topology').value }))); await refresh(); });
action('join', async () => {
  const instance = instances.find(i => i.instanceId === selected); if (!instance) throw new Error('인스턴스를 선택하세요.');
  operationId = (await call('instances.join', { runId:instance.runId })).operationId;
  $('preparation').textContent = '타이틀 UI로 방을 만들고 플레이어를 접속시키고 있습니다.';
});
action('artifacts', async () => { const artifactRun = $('artifact-run').value.trim() || runId;
  if (!artifactRun) throw new Error('저장된 실행 ID를 입력하거나 테스트를 실행하세요.'); $('artifact-list').replaceChildren();
  for (const artifact of await call('artifacts.list',{runId:artifactRun})) {
    const button=document.createElement('button');button.textContent=artifact.name;
    button.onclick=async()=>{
      try {
        const result=await call('artifacts.read',{runId:artifactRun,name:artifact.name});
        if(result.content?.mimeType?.startsWith('image/')&&result.content?.data) {
          $('artifact-content').textContent='';const image=document.createElement('img');image.style.maxWidth='100%';image.alt=artifact.name;
          image.src=`data:${result.content.mimeType};base64,${result.content.data}`;$('artifact-content').append(image);
        } else $('artifact-content').textContent=JSON.stringify(result.content,null,2);
      }catch(error){log(String(error));}
    };
    $('artifact-list').append(button);
  } });
action('stop', async () => { await call('instances.stop'); human = false; await refresh(); });
action('sendtext', () => call('ui.text', { payload: { text: $('text').value, mode:'input_adapter' } }));
let recordingDraft = null;
action('record', async () => {
  $('record').disabled = true;
  try {
    if (recording) {
      const saved = await call('recording.stop', { recordingId: recording });
      recording = null; $('record').textContent = '기록 시작';
      $('recording-review').open = true;
      $('recording-status').textContent = '기록을 저장했습니다. 초안과 판정 의도를 검토한 뒤 사용하세요.';
      $('recording-detail').textContent = JSON.stringify(saved, null, 2);
      log(JSON.stringify(saved));
      if (saved.runId && saved.draftName) {
        $('artifact-run').value = saved.runId;
        const artifact = await call('artifacts.read', {runId:saved.runId,name:saved.draftName});
        recordingDraft = artifact.content;
        $('recording-detail').textContent = JSON.stringify(recordingDraft, null, 2);
        $('load-draft').disabled = !recordingDraft?.definition || recordingDraft.blockers?.length > 0;
        if (recordingDraft.blockers?.length) $('recording-status').textContent = '기록을 저장했지만 자동 변환할 수 없는 조작이 있습니다. 아래 사유를 확인하세요.';
      }
    } else {
      recording = (await call('recording.start')).recordingId;
      recordingDraft = null; $('load-draft').disabled = true;
      $('recording-detail').textContent = '';
      $('recording-status').textContent = `선택한 인스턴스의 조작을 기록하고 있습니다. 기록 ID: ${recording}`;
      $('record').textContent = '기록 종료';
    }
  } finally { $('record').disabled = false; }
});
action('load-draft', () => {
  if (!recordingDraft?.definition || recordingDraft.blockers?.length) throw new Error('사용 가능한 초안이 없습니다.');
  $('scenario').value = JSON.stringify(recordingDraft.definition, null, 2);
  $('scenario').focus();
  $('result').textContent = '검토용 초안을 가져왔습니다. 판정 조건과 참여자 연결을 확인하고 검증하세요.';
});
action('validate', async () => { $('result').textContent = JSON.stringify(await call('scenario.validate', { definition: JSON.parse($('scenario').value) }), null, 2); });
action('run', async () => { const value = await call('scenario.start', { definition: JSON.parse($('scenario').value), actors: JSON.parse($('actors').value) }); runId = value.runId; });
action('cancel', () => call('scenario.cancel', { runId }));
const keyMap = { Digit0:'Alpha0',Digit1:'Alpha1',Digit2:'Alpha2',Digit3:'Alpha3',Digit4:'Alpha4',Digit5:'Alpha5',Digit6:'Alpha6',Digit7:'Alpha7',Digit8:'Alpha8',Digit9:'Alpha9',AltLeft:'LeftAlt',AltRight:'RightAlt',ShiftRight:'RightShift',ControlRight:'RightControl', KeyT:'T',KeyJ:'J',KeyK:'K',Slash:'Slash',NumpadEnter:'KeypadEnter',Minus:'Minus',Equal:'Equals', KeyW:'W', KeyA:'A', KeyS:'S', KeyD:'D', KeyF:'F', KeyE:'E', KeyI:'I', KeyQ:'Q', KeyY:'Y', Space:'Space', Enter:'Return', Escape:'Escape', Tab:'Tab', ShiftLeft:'LeftShift', ControlLeft:'LeftControl', ArrowUp:'UpArrow', ArrowDown:'DownArrow', ArrowLeft:'LeftArrow', ArrowRight:'RightArrow' };
async function key(event, down) {
  if (!human || !keyMap[event.code] || ['INPUT','TEXTAREA','SELECT'].includes(document.activeElement.tagName)) return;
  event.preventDefault(); const key = keyMap[event.code]; if (down && held.has(key)) return;
  down ? held.add(key) : held.delete(key);
  try { await input('input.execute', { payload: { sequence: [{ operation: down ? 'press' : 'release', key }] } }); } catch (error) { log(String(error)); }
}
document.addEventListener('keydown', e => key(e, true)); document.addEventListener('keyup', e => key(e, false));
window.addEventListener('blur', () => release().catch(() => {})); document.addEventListener('visibilitychange', () => { if (document.hidden) release().catch(() => {}); });
let lookX = 0, lookY = 0, pendingPointer = null;
let cameraMouseEvents = 0, cameraMovingEvents = 0;
$('screen').addEventListener('mousemove', e => {
  if (!human || document.pointerLockElement !== $('screen')) return;
  cameraMouseEvents++;
  const x = e.movementX, y = e.movementY;
  if (!Number.isFinite(x) || !Number.isFinite(y)) {
    $('camera-input-status').textContent = '마우스 이동량을 읽지 못했습니다.';
    return;
  }
  if (x || y) cameraMovingEvents++;
  lookX += x; lookY -= y;
  $('camera-input-status').textContent = `브라우저 입력 ${cameraMouseEvents}회 · 이동 감지 ${cameraMovingEvents}회 · 최근 이동 (${x}, ${y})`;
});
async function pointer(e, pressed) {
  if (!human) return;
  if (document.pointerLockElement === $('screen')) {
    const key = {0:'Mouse0',1:'Mouse2',2:'Mouse1'}[e.button];
    if (!key) return;
    e.preventDefault();
    pressed ? held.add(key) : held.delete(key);
    try { await input('input.execute', {payload:{sequence:[{operation:pressed?'press':'release',key}]}}); }
    catch(error) { log(String(error)); }
    return;
  }
  if (!imageWidth || e.button !== 0) return;
  const rect = $('screen').getBoundingClientRect();
  const x = (e.clientX - rect.left) / rect.width * imageWidth, y = (1 - (e.clientY - rect.top) / rect.height) * imageHeight;
  const target = {instanceId:selected,controlEpoch:epoch};
  try { await input('ui.pointer', { ...target, payload: { x, y, pressed, screenWidth:imageWidth,screenHeight:imageHeight,frame:imageFrame } }); }
  catch (error) {
    log(String(error));
    // A stale frame may reject pointerup. Release without replaying its click position.
    if (!pressed) {
      try { await input('input.release_all', target); } catch (releaseError) { log(String(releaseError)); }
    }
  }
}
$('screen').addEventListener('pointerdown', e => { $('screen').setPointerCapture(e.pointerId); pointer(e, true); });
$('screen').addEventListener('pointermove', e => {
  if (human && !document.pointerLockElement && (e.buttons & 1)) {
    pendingPointer = {clientX:e.clientX,clientY:e.clientY,button:0};
  }
});
$('screen').addEventListener('pointerup', e => { pendingPointer = null; pointer(e, false); });
$('screen').addEventListener('dragstart', e => e.preventDefault());
$('screen').addEventListener('pointercancel', () => release().catch(() => {}));
$('screen').addEventListener('contextmenu', e => { if(human) e.preventDefault(); });
document.addEventListener('pointerlockchange', () => {
  if (document.pointerLockElement === $('screen')) {
    cameraMouseEvents = cameraMovingEvents = 0;
    $('camera-input-status').textContent = '마우스 입력 수신 대기 중입니다.';
  }
  $('camera-status').textContent = document.pointerLockElement === $('screen')
    ? '카메라 조작 중입니다. Esc 키로 마우스 잠금을 해제합니다.'
    : '카메라 잠금이 해제됐습니다.';
  if (document.pointerLockElement !== $('screen')) release().catch(error => log(String(error)));
});
document.addEventListener('pointerlockerror', () => {
  if ($('camera-status').textContent === '카메라 잠금을 요청하고 있습니다.') cameraError();
});
$('screen').addEventListener('wheel', async e => {
  if (!human || !e.deltaY) return;
  e.preventDefault();
  const target = {instanceId:selected,controlEpoch:epoch};
  try {
    if (document.pointerLockElement !== $('screen')) {
      if (!imageWidth || !imageHeight) return;
      const rect = $('screen').getBoundingClientRect();
      await input('ui.pointer', {...target, payload:{
        x:(e.clientX-rect.left)/rect.width*imageWidth,
        y:(1-(e.clientY-rect.top)/rect.height)*imageHeight,
        pressed:false,screenWidth:imageWidth,screenHeight:imageHeight,frame:imageFrame
      }});
    }
    await input('input.execute', {...target,payload:{sequence:[{operation:'scroll',y:-Math.sign(e.deltaY)}]}});
  } catch(error) { log(String(error)); }
}, {passive:false});
let renewing = false;
setInterval(async () => {
  if (renewing || !human || !selected || document.hidden) return;
  renewing = true;
  const id = selected, controlEpoch = epoch;
  try { await call('control.heartbeat', {instanceId:id,controlEpoch,ttlMs:1000}); }
  catch (error) {
    if (selected === id && epoch === controlEpoch) {
      human = false; generation++; held.clear(); lookX = lookY = 0; pendingPointer = null;
      $('status').textContent = String(error);
    }
  } finally { renewing = false; }
}, 500);
let polling = false;
setInterval(async () => {
  if (polling || !selected) return; polling = true;
  const currentId = selected;
  try {
    const state = await call('game.observe',{instanceId:currentId}); if (currentId !== selected) return; $('raw-state').textContent = JSON.stringify(state, null, 2);
    $('state').textContent = `씬: ${state.scene}\n제어: ${state.control.owner} · 세대 ${state.control.controlEpoch}\n플레이어: ${state.client.players.length}명 · 서버 틱 ${state.server.tick}\n입력 문맥: ${state.inputContext ?? '확인 중'}`;
    $('status').textContent = `${state.scene} · 연결됨 · ${state.control.owner}`;
    epoch = state.control.controlEpoch; human = state.control.owner === 'RemoteHuman';
    if (human && !document.hidden) {
      if (held.size) await input('input.execute', { payload: { sequence: [...held].map(key => ({ operation: 'press', key })) } });
    }
    const events = await call('events.read', {instanceId:currentId, payload: { cursor } }); if (currentId !== selected) return; cursor = events.cursor;
    for (const event of events.events) log(JSON.stringify(event));
    if (runId) $('result').textContent = JSON.stringify(await call('scenario.status', { runId }), null, 2);
  } catch (error) { $('status').textContent = String(error); }
  finally { polling = false; }
}, 600);
let looking = false;
let pointing = false;
setInterval(async () => {
  if (pointing || !pendingPointer) return;
  const event = pendingPointer; pendingPointer = null;
  if (!human || document.hidden || document.pointerLockElement) return;
  pointing = true;
  try { await pointer(event, true); } finally { pointing = false; }
}, 50);
setInterval(async () => {
  if (looking || !human || (!lookX && !lookY)) return; looking = true;
  const x = lookX * .1, y = lookY * .1; lookX = lookY = 0;
  try { await input('input.execute', { payload: { sequence: [{ operation: 'lookDelta', x, y }] } }); }
  catch (error) { log(String(error)); } finally { looking = false; }
}, 50);
let background = false;
setInterval(async () => {
  if (background || document.hidden) return; background = true;
  try {
    if (operationId) {
      const operation = await call('operations.status',{operationId});
      $('preparation').textContent = operation.error ?? (operation.state === 'completed' ? '모든 플레이어의 접속과 스폰을 확인했습니다.' : `접속 준비: ${operation.state}`);
      if (operation.state !== 'running') {
        operationId = null;
        if (operation.result?.actors) $('actors').value = JSON.stringify(operation.result.actors);
        await refresh();
      }
    }
    await refreshAssistance().catch(error => { $('assistance-status').textContent = String(error); });
    for (const img of $('thumbnails').querySelectorAll('img')) {
      const id = img.dataset.instance;
      if (id === selected && $('screen').src) { img.src = $('screen').src; continue; }
      try { const shot = await call('game.screenshot',{instanceId:id,ttlMs:1000}); img.src = `data:${shot.mimeType};base64,${shot.data}`; }
      catch { img.alt = `${id.slice(-7)} · 화면 대기`; }
    }
  } catch (error) { $('preparation').textContent = String(error); }
  finally { background = false; }
}, 1000);
setInterval(async () => {
  if (document.hidden || !selected || capturing || Date.now()-captureStarted < 1000/12) return;
  try { await capture(); }
  catch (error) { $('stream-status').textContent = `화면 수신 대기: ${String(error)}`; }
}, 10);
refresh().catch(error => $('status').textContent = String(error));
// Console shell: panel tabs, instance highlight and log decoration.
// The headless DOM used by the console tests lacks these APIs, so the shell stays inert there.
if (typeof MutationObserver === 'function' && typeof document.querySelectorAll === 'function') shell();
function shell() {
  const store = { get(key) { try { return sessionStorage.getItem(key); } catch { return null; } },
    set(key, value) { try { sessionStorage.setItem(key, value); } catch {} } };
  const tabs = [...document.querySelectorAll('[data-panel]')];
  const show = name => {
    for (const tab of tabs) {
      const chosen = tab.dataset.panel === name;
      tab.setAttribute('aria-selected', String(chosen));
      document.getElementById(tab.dataset.panel)?.classList.toggle('is-active', chosen);
    }
    store.set('e2e-panel', name);
  };
  for (const tab of tabs) tab.onclick = () => show(tab.dataset.panel);
  const opened = store.get('e2e-panel');
  if (tabs.some(tab => tab.dataset.panel === opened)) show(opened);
  const openPanel = name => { if (!document.getElementById(name)?.classList.contains('is-active')) show(name); };
  $('run')?.addEventListener('click', () => openPanel('panel-scenario'));
  $('artifacts')?.addEventListener('click', () => openPanel('panel-artifacts'));
  $('assistance-submit')?.addEventListener('click', () => openPanel('panel-assistance'));

  const tones = { control:'tag-blue', console:'tag-blue', input:'tag-violet', ui:'tag-violet', pointer:'tag-violet',
    scenario:'tag-green', fixture:'tag-green', quest:'tag-green', signal:'tag-green',
    protocol:'tag-amber', network:'tag-amber', performance:'tag-amber', Error:'tag-red', Warning:'tag-amber' };
  const parsed = new Map();
  function describe(text) {
    if (parsed.has(text)) return parsed.get(text);
    let event = { label:'', tone:'', time:'', body:text, detail:'', severity:'' };
    if (text.startsWith('{')) try {
      const { eventSequence, eventType, instanceId, runId, side, frame, wallClockTimestamp, monotonicTimestamp, payload, ...rest } = JSON.parse(text);
      if (typeof eventType === 'string') {
        const severity = typeof payload?.severity === 'string' ? payload.severity : '';
        const stamp = wallClockTimestamp ? new Date(wallClockTimestamp) : null;
        const label = eventType === 'log' ? (severity || 'log') : eventType;
        event = { label, severity, tone: tones[label] ?? tones[eventType.split('.')[0]] ?? '',
          time: stamp && !Number.isNaN(stamp.getTime())
            ? `${String(stamp.getHours()).padStart(2,'0')}:${String(stamp.getMinutes()).padStart(2,'0')}:${String(stamp.getSeconds()).padStart(2,'0')}.${String(stamp.getMilliseconds()).padStart(3,'0')}`
            : '',
          body: eventType === 'log' && typeof payload?.message === 'string'
            ? payload.message
            : JSON.stringify(Object.keys(rest).length ? { ...rest, payload } : payload ?? {}),
          detail: [eventSequence !== undefined ? `#${eventSequence}` : '', side ?? '', frame !== undefined ? `frame ${frame}` : '',
            monotonicTimestamp !== undefined ? `${Number(monotonicTimestamp).toFixed(3)}s` : '', instanceId ? instanceId.slice(-7) : '', runId ? `실행 ${runId.slice(-7)}` : '']
            .filter(Boolean).join(' · ') };
      }
    } catch { /* An unparsed line stays readable as its original text. */ }
    if (parsed.size > 600) parsed.clear();
    parsed.set(text, event);
    return event;
  }
  function decorate(entry) {
    if (entry.dataset.shell) return;
    entry.dataset.shell = '1';
    const text = entry.textContent, event = describe(text);
    if (event.severity === 'Error' || (!event.label && text.includes('Error'))) entry.classList.add('is-error');
    else if (event.severity === 'Warning' || (!event.label && text.includes('Warning'))) entry.classList.add('is-warn');
    if (!event.label) return;
    entry.textContent = event.body;
    if (event.detail) entry.title = event.detail;
    const tag = document.createElement('span');
    tag.className = `tag ${event.tone}`.trim(); tag.textContent = event.label;
    const nodes = [tag];
    if (event.time) {
      const time = document.createElement('span');
      time.className = 'logtime'; time.textContent = event.time; nodes.unshift(time);
    }
    entry.prepend(...nodes);
  }
  const logs = $('logs');
  if (logs) {
    new MutationObserver(records => {
      for (const record of records) for (const node of record.addedNodes) if (node.nodeName === 'PRE') decorate(node);
    }).observe(logs, { childList:true });
    for (const entry of logs.querySelectorAll('pre')) decorate(entry);
  }

  const badge = document.getElementById('control-badge'), owners = {
    RemoteHuman: ['제어: 사람', 'pill pill-green'], Automation: ['제어: AI', 'pill pill-blue'], None: ['제어: 없음', 'pill'] };
  setInterval(() => {
    const chosen = $('instances').value;
    let count = 0;
    for (const button of $('thumbnails').children) {
      count++;
      button.classList.toggle('is-selected', button.querySelector('img')?.dataset.instance === chosen);
    }
    const railEmpty = document.getElementById('rail-empty');
    if (railEmpty) railEmpty.hidden = count > 0;
    const owner = /제어: (\w+)/.exec($('state').textContent)?.[1] ?? '';
    const [text, className] = owners[owner] ?? ['제어: 확인 중', 'pill'];
    if (badge) { badge.textContent = text; badge.className = className; }
    const status = $('status').textContent;
    $('status').title = status;
    $('status').className = /실패|오류|없습니다|만료|권한|Error/.test(status) ? 'pill pill-red'
      : status.includes('연결됨') ? 'pill pill-green' : 'pill pill-blue';
  }, 400);
}
