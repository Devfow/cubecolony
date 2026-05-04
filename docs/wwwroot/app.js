let state = null;
let selectedBotId = 1;
let selectedCell = { x: 2, y: 2 };
let audio = null;

const qs = (id) => document.getElementById(id);

async function fetchState() {
  const res = await fetch('/api/state');
  state = await res.json();
  render();
}

async function send(kind, extra = {}) {
  ensureAudio();
  const botId = selectedBotId || 0;
  const body = JSON.stringify({ kind, botId, x: selectedCell.x, y: selectedCell.y, topicId: 12, ...extra });
  const res = await fetch('/api/command', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body });
  state = await res.json();
  beep(kind);
  render();
}

function render() {
  qs('tick').textContent = state.tick;
  const grid = qs('grid');
  grid.style.setProperty('--cols', state.width);
  grid.innerHTML = '';

  for (let y = 0; y < state.height; y++) {
    for (let x = 0; x < state.width; x++) {
      const cubeInfo = state.open.find(c => c.x === x && c.y === y);
      const bot = state.bots.find(b => b.x === x && b.y === y && b.alive);
      const cube = document.createElement('button');
      cube.className = `cube ${cubeInfo.open ? '' : 'closed'} ${selectedCell.x === x && selectedCell.y === y ? 'selected' : ''}`;
      cube.onclick = () => {
        selectedCell = { x, y };
        if (bot) selectedBotId = bot.id;
        render();
      };
      if (bot) {
        const node = document.createElement('div');
        node.className = `bot g${bot.groupId} ${bot.alive ? '' : 'dead'}`;
        node.textContent = bot.name.split('-')[0][0] + bot.id;
        cube.appendChild(node);
      }
      const item = state.items.find(i => i.x === x && i.y === y);
      if (item) {
        const itemNode = document.createElement('div');
        itemNode.className = 'item';
        itemNode.title = item.name;
        itemNode.textContent = item.icon;
        cube.appendChild(itemNode);
      }
      grid.appendChild(cube);
    }
  }

  const selected = state.bots.find(b => b.id === selectedBotId) || state.bots.find(b => b.alive);
  if (selected) {
    selectedBotId = selected.id;
    qs('botName').textContent = selected.name;
    qs('botEvent').textContent = selected.event;
    qs('goal').textContent = selected.goal;
    qs('emotion').textContent = selected.emotion;
    qs('personality').textContent = selected.personality;
    setMeter('energy', selected.energy);
    setMeter('nutrition', selected.nutrition);
    setMeter('integrity', selected.integrity);
    setMeter('social', selected.social);
    setMeter('craving', selected.craving);
    setMeter('trauma', selected.trauma);
    setMeter('dopamine', selected.dopamine);
    setMeter('cortisol', selected.cortisol);
    updateFace(selected);
  }

  renderNews();
}

function setMeter(id, value) {
  qs(id).value = Math.max(0, Math.min(1, value));
}

function updateFace(bot) {
  const mouth = qs('mouth');
  mouth.className = 'mouth';
  if (['Afraid', 'Lonely', 'Hurt', 'Exhausted'].includes(bot.emotion)) mouth.classList.add('sad');
  if (['Craving', 'Angry'].includes(bot.emotion)) mouth.classList.add('flat');
}

function ensureAudio() {
  if (!audio) audio = new (window.AudioContext || window.webkitAudioContext)();
}

function beep(kind) {
  if (!audio) return;
  const osc = audio.createOscillator();
  const gain = audio.createGain();
  const tones = { feed: 330, pet: 440, caffeine: 720, poppy: 220, teach: 520, add: 280, toggle: 180 };
  osc.frequency.value = tones[kind] || 360;
  gain.gain.value = 0.05;
  osc.connect(gain);
  gain.connect(audio.destination);
  osc.start();
  osc.stop(audio.currentTime + 0.08);
}

function renderNews() {
  const news = qs('news');
  const atBottom = news.scrollTop + news.clientHeight >= news.scrollHeight - 12;
  news.innerHTML = '';
  [...state.news].reverse().forEach(line => {
    const row = document.createElement('div');
    row.className = 'news-line';
    row.innerHTML = `<strong>${line.tick}</strong><span>${escapeHtml(line.text)}</span>`;
    news.appendChild(row);
  });
  if (atBottom || news.scrollTop === 0) news.scrollTop = 0;
}

function escapeHtml(text) {
  return String(text).replace(/[&<>"']/g, ch => ({
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#039;'
  })[ch]);
}

document.querySelectorAll('[data-command]').forEach(btn => {
  btn.addEventListener('click', () => send(btn.dataset.command));
});
qs('addBot').addEventListener('click', () => send('add'));
qs('toggleCube').addEventListener('click', () => send('toggle'));

fetchState();
setInterval(fetchState, 900);
