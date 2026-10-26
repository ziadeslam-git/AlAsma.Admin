/* AlAsma.Admin — front-end behaviour (vanilla JS, no framework)
   Features: theme, sidebar rail, sheets/drawers/modals, export menus, toasts,
   password toggle, live totals, row -> edit form filling, contract countdown. */
(function () {
  'use strict';

  var doc = document;
  var root = doc.documentElement;

  function qs(sel, ctx) { return (ctx || doc).querySelector(sel); }
  function qsa(sel, ctx) { return Array.prototype.slice.call((ctx || doc).querySelectorAll(sel)); }
  function store(key, value) {
    try { if (value === undefined) { return localStorage.getItem(key); } localStorage.setItem(key, value); } catch (e) { /* storage blocked */ }
    return null;
  }

  /* ---------- Theme ---------- */
  function applyTheme(theme) {
    if (theme === 'dark') { root.setAttribute('data-theme', 'dark'); } else { root.removeAttribute('data-theme'); }
    var meta = qs('meta[name="theme-color"]');
    if (meta) { meta.setAttribute('content', theme === 'dark' ? '#00201e' : '#004743'); }
    qsa('[data-theme-toggle]').forEach(function (btn) { btn.setAttribute('aria-pressed', theme === 'dark' ? 'true' : 'false'); });
  }
  doc.addEventListener('click', function (e) {
    var t = e.target.closest('[data-theme-toggle]');
    if (!t) { return; }
    var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
    store('theme', next);
    applyTheme(next);
  });
  applyTheme(root.getAttribute('data-theme') === 'dark' ? 'dark' : 'light');

  /* ---------- Scrim + overlays ---------- */
  var scrim = qs('.scrim');
  var openStack = [];

  function openOverlay(el) {
    if (!el) { return; }
    el.classList.add('is-open');
    el.removeAttribute('aria-hidden');
    openStack.push(el);
    if (scrim) { scrim.classList.add('is-open'); }
    var focusable = qs('input:not([type=hidden]):not([readonly]), select, textarea, button, a[href]', el);
    if (focusable && el.matches('.drawer, .modal')) { setTimeout(function () { focusable.focus(); }, 60); }
  }
  function closeOverlay(el) {
    if (!el) { return; }
    el.classList.remove('is-open');
    el.setAttribute('aria-hidden', 'true');
    openStack = openStack.filter(function (x) { return x !== el; });
    if (!openStack.length && scrim) { scrim.classList.remove('is-open'); }
  }
  function closeAll() { openStack.slice().forEach(closeOverlay); closeSidebar(); }

  doc.addEventListener('click', function (e) {
    var opener = e.target.closest('[data-open]');
    if (opener) {
      var target = doc.getElementById(opener.getAttribute('data-open'));
      if (target) {
        e.preventDefault();
        closeAll();
        if (opener.hasAttribute('data-reset')) { resetForm(target); }
        openOverlay(target);
      }
      return;
    }
    if (e.target.closest('[data-close]')) {
      var el = e.target.closest('.drawer, .modal, .sheet');
      closeOverlay(el);
      return;
    }
    if (e.target === scrim) { closeAll(); }
    if (e.target.classList && e.target.classList.contains('modal')) { closeOverlay(e.target); }
  });
  doc.addEventListener('keydown', function (e) { if (e.key === 'Escape') { closeAll(); closeMenus(); } });

  function resetForm(container) {
    var form = qs('form', container);
    if (form) { form.reset(); updateTotals(form); }
  }

  /* ---------- Sidebar (rail expand on tablet, collapse on desktop) ---------- */
  var sidebar = qs('.sidebar');
  function closeSidebar() { if (sidebar && sidebar.classList.contains('is-open')) { sidebar.classList.remove('is-open'); if (scrim && !openStack.length) { scrim.classList.remove('is-open'); } } }
  var brand = qs('[data-sidebar-brand]');
  if (brand && sidebar) {
    brand.addEventListener('click', function () {
      if (window.matchMedia('(min-width: 640px) and (max-width: 1023px)').matches) {
        var open = sidebar.classList.toggle('is-open');
        if (scrim) { scrim.classList.toggle('is-open', open); }
      }
    });
  }
  var collapse = qs('[data-sidebar-collapse]');
  if (collapse) {
    collapse.addEventListener('click', function () {
      var rail = root.getAttribute('data-sidebar') === 'rail';
      if (rail) { root.removeAttribute('data-sidebar'); store('sidebar', 'full'); } else { root.setAttribute('data-sidebar', 'rail'); store('sidebar', 'rail'); }
    });
  }

  /* ---------- Menus (export dropdown) ---------- */
  function closeMenus(except) { qsa('.menu.is-open').forEach(function (m) { if (m !== except) { m.classList.remove('is-open'); } }); }
  doc.addEventListener('click', function (e) {
    var trigger = e.target.closest('[data-menu]');
    if (trigger) {
      var menu = trigger.closest('.menu');
      closeMenus(menu);
      menu.classList.toggle('is-open');
      trigger.setAttribute('aria-expanded', menu.classList.contains('is-open') ? 'true' : 'false');
      return;
    }
    if (!e.target.closest('.menu')) { closeMenus(); }
  });

  /* ---------- Toasts ---------- */
  function dismissToast(t) { t.classList.add('is-leaving'); setTimeout(function () { t.remove(); }, 260); }
  qsa('.toast').forEach(function (t) { setTimeout(function () { dismissToast(t); }, t.classList.contains('toast--error') ? 9000 : 5500); });
  doc.addEventListener('click', function (e) { var c = e.target.closest('.toast__close'); if (c) { dismissToast(c.closest('.toast')); } });

  /* ---------- Password show/hide ---------- */
  doc.addEventListener('click', function (e) {
    var b = e.target.closest('[data-toggle-password]');
    if (!b) { return; }
    var input = qs('input', b.closest('.input-wrap'));
    if (!input) { return; }
    var show = input.type === 'password';
    input.type = show ? 'text' : 'password';
    b.setAttribute('aria-label', show ? 'إخفاء كلمة المرور' : 'إظهار كلمة المرور');
    var shown = qs('[data-icon-show]', b), hidden = qs('[data-icon-hide]', b);
    if (shown && hidden) { shown.hidden = show; hidden.hidden = !show; }
  });

  /* ---------- Live totals (sale: price*qty - expenses*qty, operation: cost*qty) ---------- */
  function num(v) { var n = parseFloat(String(v).replace(/,/g, '')); return isNaN(n) ? 0 : n; }
  function fmt(n) { return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
  function updateTotals(form) {
    qsa('[data-total]', form).forEach(function (out) {
      var mode = out.getAttribute('data-total');
      var q = num((qs('[name="Quantity"]', form) || {}).value);
      var total = 0;
      if (mode === 'sale') {
        var price = num((qs('[name="SalePrice"]', form) || {}).value);
        var exp = num((qs('[name="BasicExpenses"]', form) || {}).value);
        total = Math.max(0, price * q - exp * q);
      } else {
        total = num((qs('[name="ExpenseAmount"]', form) || {}).value) * q;
      }
      out.textContent = fmt(total);
    });
  }
  doc.addEventListener('input', function (e) { var f = e.target.closest('form'); if (f && qs('[data-total]', f)) { updateTotals(f); } });
  qsa('form').forEach(function (f) { if (qs('[data-total]', f)) { updateTotals(f); } });

  /* ---------- Fill an edit form from a row (data-edit holds JSON) ---------- */
  doc.addEventListener('click', function (e) {
    var b = e.target.closest('[data-edit]');
    if (!b) { return; }
    e.preventDefault();
    var targetId = b.getAttribute('data-edit-target');
    var target = doc.getElementById(targetId);
    if (!target) { return; }
    var data = {};
    try { data = JSON.parse(b.getAttribute('data-edit')); } catch (err) { return; }
    var form = qs('form', target);
    Object.keys(data).forEach(function (k) {
      var field = qs('[name="' + k + '"]', form);
      if (field) { field.value = data[k] === null || data[k] === undefined ? '' : data[k]; }
    });
    updateTotals(form);
    closeAll();
    openOverlay(target);
  });

  /* ---------- Delete modal: fill facts + form id ---------- */
  doc.addEventListener('click', function (e) {
    var b = e.target.closest('[data-delete]');
    if (!b) { return; }
    e.preventDefault();
    var modal = doc.getElementById(b.getAttribute('data-delete-target'));
    if (!modal) { return; }
    var data = {};
    try { data = JSON.parse(b.getAttribute('data-delete')); } catch (err) { return; }
    var idField = qs('input[name="id"]', modal);
    if (idField) { idField.value = data.id; }
    qsa('[data-fact]', modal).forEach(function (el) { var k = el.getAttribute('data-fact'); el.textContent = data[k] === undefined ? '' : data[k]; });
    closeAll();
    openOverlay(modal);
  });


  /* ---------- Client-side filter for the rows of the current page (authors list) ---------- */
  qsa('[data-filter-scope]').forEach(function (scope) {
    var input = qs('[data-filter-input]', scope);
    var chips = qsa('[data-filter-chip]', scope);
    var rows = qsa('[data-filter-row]', scope);
    var empty = qs('[data-filter-empty]', scope);
    var status = '';
    function apply() {
      var term = input ? input.value.trim().toLowerCase() : '';
      var visible = 0;
      rows.forEach(function (row) {
        var ok = (!term || row.getAttribute('data-search').toLowerCase().indexOf(term) !== -1) &&
                 (!status || row.getAttribute('data-status') === status);
        row.hidden = !ok;
        if (ok) { visible++; }
      });
      if (empty) { empty.hidden = visible !== 0; }
    }
    if (input) { input.addEventListener('input', apply); }
    chips.forEach(function (chip) {
      chip.addEventListener('click', function () {
        status = chip.getAttribute('data-filter-chip');
        chips.forEach(function (c) { c.classList.toggle('chip--active', c === chip); });
        apply();
      });
    });
  });

  /* ---------- Search clear + auto submit when filter chip changes ---------- */
  doc.addEventListener('click', function (e) {
    var c = e.target.closest('[data-clear-search]');
    if (c) { var f = c.closest('form'); qs('input[name="q"]', f).value = ''; f.submit(); }
  });

  /* ---------- Submit guard (prevents double submit) ---------- */
  doc.addEventListener('submit', function (e) {
    var f = e.target;
    if (f.hasAttribute('data-once')) {
      var btn = qs('[type="submit"]', f);
      if (btn) { setTimeout(function () { btn.disabled = true; }, 0); }
    }
  });

  /* ---------- Contract countdown ---------- */
  qsa('.countdown[data-deadline]').forEach(function (el) {
    var deadline = new Date(el.getAttribute('data-deadline'));
    if (isNaN(deadline.getTime())) { return; }
    function tick() {
      var diff = deadline - new Date();
      if (diff <= 0) { el.textContent = 'منتهي'; clearInterval(timer); return; }
      var d = Math.floor(diff / 86400000), h = Math.floor(diff % 86400000 / 3600000), m = Math.floor(diff % 3600000 / 60000), s = Math.floor(diff % 60000 / 1000);
      el.textContent = d + 'ي ' + h + 'س ' + m + 'د ' + s + 'ث';
    }
    var timer = setInterval(tick, 1000);
    tick();
  });

  /* ---------- Open a drawer/modal automatically (e.g. after a failed submit) ---------- */
  qsa('[data-autoopen]').forEach(function (el) { openOverlay(el); });
})();
