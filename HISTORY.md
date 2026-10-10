# HISTORY.md — история изменений SporeMaster 3

Формат: сверху — свежие изменения. Версии условные, отражают крупные этапы.

## [3.0.0] — первый релиз переработки (2026-10-10)

### Добавлено
- Новое решение **.NET 9**: `SporeMaster.Core`, `SporeMaster.Cli`, `SporeMaster.Tests`.
- Ядро ядра (`SporeMaster.Core`):
  - чтение DBPF/DBBF (версии 1.x/2.x/3.x, включая **бета-версию Spore**);
  - запись архивов (версии 2.0 и 3.0, RefPack-сжатие, «большой» формат);
  - полный порт **RefPack/QFS** (компрессор и декомпрессор), байт-совместимый со SporeModder FX;
  - 32-битный **FNV-хеш** именования (совместим со SporeMaster 2 и SporeModder FX);
  - реестры имён `reg_type`, `reg_file`, `reg_property`, `reg_simulator` — встроены в сборку;
  - каркас локализации (`Localizer`, `lang/en.lang`).
- CLI-утилита `spore`: `info`, `list`, `extract`, `unpack`, `pack`, `verify`, `hash`, `types`.
- Headless-тесты: собственный раннер + 20 тестов, включая проверку на реальных бета- и
  релизных архивах.
- Документация: `README.md`, `AGENT.md`, `HISTORY.md`, `MEMORY.md`, `PROMPT.md`,
  `docs/ARCHITECTURE.md`, `docs/DBPF_FORMAT.md` (на русском).
- Инициализирован git-репозиторий, добавлен `.gitignore`.

### Исправлено (в процессе разработки)
- `indexSize` в заголовке вычислялся по устаревшей позиции потока и записывался как `0`.
- Конфликт глобального флага `--version` с параметром команды `pack --version`.

### Известные ограничения
- Версия формата DBPF 1.x читается лишь частично.
- Нет GUI, текстового формата `.prop`, текстур DDS, моделей RW4, локалей — см. `MEMORY.md`.

### Основа
- Портированы алгоритмы из `SporeMaster 2.0` (Gibbed.Spore) и `SporeModder FX`.
- Проверка форматов — по `refs/EAPackageBrowser-main`, `refs/CheckAll`, `refs/SporeEdit`.
