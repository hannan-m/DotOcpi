/* ═══════════════════════════════════════════════════════════
   DotOcpi Dashboard — SSE + HTMX Bridge
   ═══════════════════════════════════════════════════════════ */

(function () {
  'use strict';

  // ── SSE Connection ─────────────────────────────────────────

  let evtSource = null;
  let reconnectTimer = null;
  const SSE_URL = '/api/events';
  const RECONNECT_DELAY = 3000;

  function connectSSE() {
    if (evtSource) evtSource.close();

    evtSource = new EventSource(SSE_URL);
    const dot = document.getElementById('sse-dot');
    const label = document.getElementById('sse-label');

    evtSource.onopen = () => {
      if (dot) { dot.classList.add('connected'); }
      if (label) { label.textContent = 'Live'; }
    };

    evtSource.onerror = () => {
      if (dot) { dot.classList.remove('connected'); }
      if (label) { label.textContent = 'Offline'; }
      evtSource.close();
      clearTimeout(reconnectTimer);
      reconnectTimer = setTimeout(connectSSE, RECONNECT_DELAY);
    };

    // ── Event Handlers ─────────────────────────────────────

    evtSource.addEventListener('sync-completed', (e) => {
      const d = JSON.parse(e.data);
      addActivity('sync', `Synced <strong>${d.items}</strong> ${d.module} from <strong>${d.cpoId}</strong>`, d.duration);
      showToast('Sync Complete', `${d.cpoId}/${d.module}: ${d.items} items`, 'success');
      refreshStats();
      triggerSessionRefresh();
    });

    evtSource.addEventListener('command-result', (e) => {
      const d = JSON.parse(e.data);
      addActivity('command', `Command <strong>${d.command}</strong> to <strong>${d.cpoId}</strong>: ${d.result}`);
      showToast('Command Result', `${d.command}: ${d.result}`, d.result === 'ACCEPTED' ? 'success' : 'warning');
    });

    evtSource.addEventListener('session-update', (e) => {
      const d = JSON.parse(e.data);
      addActivity('sync', `Session <strong>${d.sessionId}</strong> → ${d.status} (${d.kwh} kWh)`);
      triggerSessionRefresh();
    });

    evtSource.addEventListener('health-change', (e) => {
      const d = JSON.parse(e.data);
      addActivity('health', `CPO <strong>${d.cpoId}</strong> status: ${d.status}`);
    });

    evtSource.addEventListener('data-update', (e) => {
      const d = JSON.parse(e.data);
      updateStatCard('stat-locations', d.locations);
      updateStatCard('stat-sessions', d.sessions);
      updateStatCard('stat-tariffs', d.tariffs);
      updateStatCard('stat-cdrs', d.cdrs);
    });
  }

  // ── Session/Dashboard Refresh ──────────────────────────────

  function triggerSessionRefresh() {
    ['sessions-table', 'dashboard-sessions'].forEach(id => {
      const el = document.getElementById(id);
      if (el) htmx.trigger(el, 'htmx:load');
    });
  }

  // ── Activity Feed ──────────────────────────────────────────

  function addActivity(type, html, meta) {
    const feed = document.getElementById('activity-feed');
    if (!feed) return;

    const iconMap = { sync: '⟳', command: '⚡', error: '✕', health: '♥' };
    const now = new Date().toLocaleTimeString();

    const li = document.createElement('li');
    li.className = 'activity-item';
    li.innerHTML = `
      <div class="activity-icon ${type}">${iconMap[type] || '●'}</div>
      <div>
        <div class="activity-text">${html}</div>
        <div class="activity-time">${now}${meta ? ' · ' + meta : ''}</div>
      </div>`;

    feed.prepend(li);

    // Keep only last 15 items
    while (feed.children.length > 15) {
      feed.removeChild(feed.lastChild);
    }
  }

  // ── Stat Cards ─────────────────────────────────────────────

  function updateStatCard(id, value) {
    const el = document.getElementById(id);
    if (el) el.textContent = value;
  }

  function refreshStats() {
    fetch('/api/metrics')
      .then(r => r.json())
      .then(d => {
        updateStatCard('stat-cpos', d.cpoCount);
        updateStatCard('stat-locations', d.dataStore?.locations ?? 0);
        updateStatCard('stat-sessions', d.dataStore?.sessions ?? 0);
        updateStatCard('stat-cdrs', d.dataStore?.cdrs ?? 0);
      })
      .catch(() => {});
  }

  // ── Toasts ─────────────────────────────────────────────────

  function showToast(title, body, type) {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast ${type || ''}`;
    toast.innerHTML = `<div class="toast-title">${title}</div><div class="toast-body">${body}</div>`;
    container.appendChild(toast);

    setTimeout(() => {
      toast.classList.add('toast-exit');
      setTimeout(() => toast.remove(), 300);
    }, 4000);
  }

  // Make showToast globally available for HTMX response handlers
  window.showToast = showToast;

  // ── Expandable Rows ────────────────────────────────────────

  document.addEventListener('click', (e) => {
    const toggle = e.target.closest('.expand-toggle');
    if (!toggle) return;

    toggle.classList.toggle('open');
    const targetId = toggle.dataset.target;
    if (targetId) {
      const content = document.getElementById(targetId);
      if (content) content.classList.toggle('open');
    }
  });

  // ── Mobile Sidebar Toggle ──────────────────────────────────

  document.addEventListener('click', (e) => {
    if (e.target.closest('.mobile-toggle')) {
      document.querySelector('.sidebar')?.classList.toggle('open');
    }
  });

  // ── Collapsible Sections ───────────────────────────────────

  document.addEventListener('click', (e) => {
    const trigger = e.target.closest('.collapsible-trigger');
    if (!trigger) return;
    const targetId = trigger.dataset.target;
    if (targetId) {
      document.getElementById(targetId)?.classList.toggle('open');
    }
  });

  // ── HTMX Events ───────────────────────────────────────────

  document.addEventListener('htmx:afterRequest', (e) => {
    if (e.detail.successful && e.detail.elt.dataset.successMessage) {
      showToast('Success', e.detail.elt.dataset.successMessage, 'success');
    }
  });

  document.addEventListener('htmx:responseError', (e) => {
    showToast('Error', 'Request failed: ' + (e.detail.xhr?.status || 'Network error'), 'error');
  });

  // ── Init ──────────────────────────────────────────────────

  document.addEventListener('DOMContentLoaded', () => {
    connectSSE();
    refreshStats();
  });

})();
