let currentDb = null;
let currentTable = null;
let selectedRowIndex = -1;
let currentDisplayedRows = [];

const DataTypeNames = ["Integer", "Real", "Char", "String", "ComplexInteger", "ComplexReal"];

async function loadDb() {
  const res = await fetch('/api/database');
  currentDb = await res.json();
  renderSidebar();
  if (currentDb.tables.length > 0) {
    if (!currentTable || !currentDb.tables.some(t => t.name === currentTable.name)) {
      selectTable(currentDb.tables[0].name);
    } else {
      selectTable(currentTable.name);
    }
  } else {
    currentTable = null;
    renderTable();
  }
}

function renderSidebar() {
  const list = document.getElementById('tableList');
  list.innerHTML = '';
  currentDb.tables.forEach(t => {
    const item = document.createElement('div');
    item.className = 'table-item' + (currentTable && currentTable.name === t.name ? ' active' : '');
    item.textContent = t.name;
    item.onclick = () => selectTable(t.name);
    list.appendChild(item);
  });
}

function selectTable(tableName) {
  currentTable = currentDb.tables.find(t => t.name === tableName);
  selectedRowIndex = -1;
  currentDisplayedRows = currentTable ? currentTable.rows : [];
  renderSidebar();
  populateSearchColumns();
  renderTable();
}

function populateSearchColumns() {
  const sel = document.getElementById('searchColumnSelect');
  sel.innerHTML = '<option value="(Всі колонки)">(Всі колонки)</option>';
  if (!currentTable) return;
  currentTable.columns.forEach(c => {
    const opt = document.createElement('option');
    opt.value = c.name;
    opt.textContent = c.name;
    sel.appendChild(opt);
  });
}

function renderTable() {
  const thead = document.getElementById('dataThead');
  const tbody = document.getElementById('dataTbody');
  thead.innerHTML = '';
  tbody.innerHTML = '';

  if (!currentTable) return;

  const headerRow = document.createElement('tr');
  currentTable.columns.forEach(c => {
    const th = document.createElement('th');
    th.textContent = `${c.name} (${DataTypeNames[c.type]})`;
    headerRow.appendChild(th);
  });
  thead.appendChild(headerRow);

  currentDisplayedRows.forEach((row, idx) => {
    const tr = document.createElement('tr');
    if (idx === selectedRowIndex) tr.classList.add('selected');
    tr.onclick = () => {
      selectedRowIndex = idx;
      renderTable();
    };

    row.cells.forEach(cell => {
      const td = document.createElement('td');
      td.textContent = cell.rawValue;
      tr.appendChild(td);
    });
    tbody.appendChild(tr);
  });
}

async function performSearch() {
  if (!currentTable) return;
  const col = document.getElementById('searchColumnSelect').value;
  const pat = document.getElementById('searchPatternInput').value;
  const res = await fetch(`/api/database/tables/${encodeURIComponent(currentTable.name)}/search?column=${encodeURIComponent(col)}&pattern=${encodeURIComponent(pat)}`);
  currentDisplayedRows = await res.json();
  selectedRowIndex = -1;
  renderTable();
}

function resetSearch() {
  document.getElementById('searchPatternInput').value = '';
  if (currentTable) currentDisplayedRows = currentTable.rows;
  selectedRowIndex = -1;
  renderTable();
}

function showModal(html) {
  const container = document.getElementById('modalContainer');
  container.innerHTML = html;
  document.getElementById('modalOverlay').style.display = 'flex';
}
function closeModal() {
  document.getElementById('modalOverlay').style.display = 'none';
}

function openCreateTableModal() {
  showModal(`
    <h3>Створити нову таблицю</h3>
    <div class="form-group">
      <label>Назва таблиці:</label>
      <input type="text" id="newTableName">
    </div>
    <div class="form-group">
      <label>Перша колонка:</label>
      <input type="text" id="firstColName" value="ID">
    </div>
    <div class="form-group">
      <label>Тип колонки:</label>
      <select id="firstColType">${DataTypeNames.map((n, i) => `<option value="${i}">${n}</option>`).join('')}</select>
    </div>
    <div id="modalErr" class="error-msg"></div>
    <div class="modal-actions">
      <button onclick="closeModal()">Скасувати</button>
      <button onclick="submitCreateTable()">Створити</button>
    </div>
  `);
}

async function submitCreateTable() {
  const name = document.getElementById('newTableName').value.trim();
  const colName = document.getElementById('firstColName').value.trim();
  if (!colName) {
    document.getElementById('modalErr').textContent = "Назва колонки не може бути порожньою!";
    return;}  
  const colType = parseInt(document.getElementById('firstColType').value);

  const payload = {
    name: name,
    columns: [{ name: colName, type: colType }],
    rows: []
  };

  const res = await fetch('/api/database/tables', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(payload)
  });

  if (!res.ok) {
    const data = await res.json();
    document.getElementById('modalErr').textContent = data.error;
    return;
  }
  closeModal();
  await loadDb();
  selectTable(name);
}

async function deleteCurrentTable() {
  if (!currentTable) return;
  if (!confirm(`Ви дійсно бажаєте видалити таблицю '${currentTable.name}'?`)) return;
  await fetch(`/api/database/tables/${encodeURIComponent(currentTable.name)}`, { method: 'DELETE' });
  await loadDb();
}

function openAddColumnModal() {
  if (!currentTable) return;
  showModal(`
    <h3>Додати колонку</h3>
    <div class="form-group">
      <label>Назва колонки:</label>
      <input type="text" id="addColName">
    </div>
    <div class="form-group">
      <label>Тип колонки:</label>
      <select id="addColType">${DataTypeNames.map((n, i) => `<option value="${i}">${n}</option>`).join('')}</select>
    </div>
    <div id="modalErr" class="error-msg"></div>
    <div class="modal-actions">
      <button onclick="closeModal()">Скасувати</button>
      <button onclick="submitAddColumn()">Додати</button>
    </div>
  `);
}

async function submitAddColumn() {
  const name = document.getElementById('addColName').value.trim();
  const type = parseInt(document.getElementById('addColType').value);
  const res = await fetch(`/api/database/tables/${encodeURIComponent(currentTable.name)}/columns`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name, type })
  });
  if (!res.ok) {
    const err = await res.json();
    document.getElementById('modalErr').textContent = err.error;
    return;
  }
  closeModal();
  await loadDb();
}

function openEditColumnModal() {
  if (!currentTable) return;
  showModal(`
    <h3>Редагувати колонку</h3>
    <div class="form-group">
      <label>Оберіть колонку:</label>
      <select id="editColSelect">${currentTable.columns.map(c => `<option value="${c.name}">${c.name}</option>`).join('')}</select>
    </div>
    <div class="form-group">
      <label>Нова назва:</label>
      <input type="text" id="editColNewName" value="${currentTable.columns[0]?.name || ''}">
    </div>
    <div class="form-group">
      <label>Новий тип:</label>
      <select id="editColNewType">${DataTypeNames.map((n, i) => `<option value="${i}">${n}</option>`).join('')}</select>
    </div>
    <div id="modalErr" class="error-msg"></div>
    <div class="modal-actions">
      <button onclick="closeModal()">Скасувати</button>
      <button onclick="submitEditColumn()">Зберегти</button>
    </div>
  `);

  document.getElementById('editColSelect').onchange = (e) => {
    const col = currentTable.columns.find(c => c.name === e.target.value);
    if (col) {
      document.getElementById('editColNewName').value = col.name;
      document.getElementById('editColNewType').value = col.type;
    }
  };
}

async function submitEditColumn() {
  const oldName = document.getElementById('editColSelect').value;
  const newName = document.getElementById('editColNewName').value.trim();
  const newType = parseInt(document.getElementById('editColNewType').value);

  const res = await fetch(`/api/database/tables/${encodeURIComponent(currentTable.name)}/columns/${encodeURIComponent(oldName)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: newName, type: newType })
  });

  if (!res.ok) {
    const err = await res.json();
    document.getElementById('modalErr').textContent = err.error;
    return;
  }
  closeModal();
  await loadDb();
}

async function deleteColumnAction() {
  if (!currentTable) return;
  const colName = prompt("Введіть назву колонки для видалення:", currentTable.columns[currentTable.columns.length - 1]?.name);
  if (!colName) return;

  const res = await fetch(`/api/database/tables/${encodeURIComponent(currentTable.name)}/columns/${encodeURIComponent(colName)}`, {
    method: 'DELETE'
  });
  if (!res.ok) {
    const err = await res.json();
    alert(err.error);
    return;
  }
  await loadDb();
}

function openRowModal(isEdit) {
  if (!currentTable) return;
  if (isEdit && selectedRowIndex === -1) {
    alert("Оберіть рядок для редагування!");
    return;
  }

  const row = isEdit ? currentDisplayedRows[selectedRowIndex] : null;

  let fieldsHtml = currentTable.columns.map((col, i) => `
    <div class="form-group">
      <label>${col.name} (${DataTypeNames[col.type]}):</label>
      <input type="text" id="rowCell_${i}" value="${row ? row.cells[i]?.rawValue : ''}">
    </div>
  `).join('');

  showModal(`
    <h3>${isEdit ? 'Редагувати рядок' : 'Новий рядок'}</h3>
    ${fieldsHtml}
    <div id="modalErr" class="error-msg"></div>
    <div class="modal-actions">
      <button onclick="closeModal()">Скасувати</button>
      <button onclick="submitRow(${isEdit})">Зберегти</button>
    </div>
  `);
}

async function submitRow(isEdit) {
  const values = currentTable.columns.map((_, i) => document.getElementById(`rowCell_${i}`).value);
  const url = isEdit 
    ? `/api/database/tables/${encodeURIComponent(currentTable.name)}/rows/${selectedRowIndex}`
    : `/api/database/tables/${encodeURIComponent(currentTable.name)}/rows`;

  const res = await fetch(url, {
    method: isEdit ? 'PUT' : 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(values)
  });

  if (!res.ok) {
    const err = await res.json();
    document.getElementById('modalErr').textContent = err.error;
    return;
  }
  closeModal();
  await loadDb();
}

async function deleteRowAction() {
  if (!currentTable || selectedRowIndex === -1) {
    alert("Оберіть рядок для видалення!");
    return;
  }
  if (!confirm("Видалити обраний рядок?")) return;
  await fetch(`/api/database/tables/${encodeURIComponent(currentTable.name)}/rows/${selectedRowIndex}`, { method: 'DELETE' });
  await loadDb();
}

let currentFileHandle = null;

async function importDatabase(event) {
  if (window.showOpenFilePicker) {
    try {
      const [handle] = await window.showOpenFilePicker({
        types: [{
          description: 'База даних JSON (*.json)',
          accept: { 'application/json': ['.json'] }
        }],
        multiple: false
      });
      currentFileHandle = handle;
      const file = await handle.getFile();
      const text = await file.text();
      await sendImportRequest(text);
      return;
    } catch (err) {
      if (err.name === 'AbortError') return; 
      console.warn("Фоллбек до стандартного інпуту:", err);
    }
  }

  const file = event.target.files[0];
  if (!file) return;
  const text = await file.text();
  await sendImportRequest(text);
  event.target.value = ''; 
}

async function sendImportRequest(jsonText) {
  const res = await fetch('/api/database/import', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(jsonText)
  });
  if (!res.ok) {
    const err = await res.json();
    alert(err.error || "Помилка імпорту");
    return;
  }
  await loadDb();
}

async function exportDatabase() {
  const res = await fetch('/api/database/export', { method: 'POST' });
  const jsonContent = await res.text();

  if (currentFileHandle && window.showSaveFilePicker) {
    try {
      const writable = await currentFileHandle.createWritable();
      await writable.write(jsonContent);
      await writable.close();
      alert("Зміни успішно збережено в поточному файлі!");
      return;
    } catch (err) {
      console.warn("Не вдалося оновити відкритий файл, створюємо діалог:", err);
    }
  }

  if (window.showSaveFilePicker) {
    try {
      const handle = await window.showSaveFilePicker({
        suggestedName: `${currentDb?.name || 'database'}.json`,
        types: [{
          description: 'База даних JSON (*.json)',
          accept: { 'application/json': ['.json'] }
        }]
      });
      currentFileHandle = handle;
      const writable = await handle.createWritable();
      await writable.write(jsonContent);
      await writable.close();
      alert("Базу даних збережено!");
      return;
    } catch (err) {
      if (err.name === 'AbortError') return;
    }
  }

  const blob = new Blob([jsonContent], { type: 'application/json' });
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `${currentDb?.name || 'database'}.json`;
  a.click();
}

async function newDatabasePrompt() {
  const name = prompt("Введіть назву нової бази даних:", "NewDatabase");
  if (!name) return;
  await fetch('/api/database/new', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(name)
  });
  await loadDb();
}

window.onload = loadDb;