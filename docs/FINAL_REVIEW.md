# Финальная ревизия (чек-лист)

Документ — заготовка **финальной ревизии** проекта (делаем в самом конце, см. `MEMORY.md` §3 п.7).
Цель: пройтись по всему, перепроверить, найти что улучшить/доделать/починить, причесать UI/UX,
закрыть «хвосты».

## Журнал ревизии

- **2026-10-11:** `verify` на всех 15 архивах → 0 проблем (39817 ресурсов); `roundtrip` lossless на
  всех релизных и бета-архивах; инвентарь форматов и приоритеты (таблица типов); подписи форматов
  (`HTRA`, XML `.crt`, Pure Data `.pfx`, бета `.cell`/`.structure` ≠ SporeModder); тест-наборы
  зафиксированы. **Найден и исправлен баг:** `list --format csv/json` игнорировал фильтры.
  Просмотрены `refs/SporeModder-FX` (форматы, конвертеры) и «внутренности» Leftovers.

## 1. Ядро (Core)

- [ ] DBPF: чтение/запись 2.0/3.0, DBBF, «загадочные» поля, устойчивость к битым файлам.
- [ ] RefPack: байт-точность против SporeModder на «сложных» данных.
- [ ] `.prop` (binary) + `.prop.xml` + `.prop_t`: round-trip на всём корпусе; авто-локали.
- [ ] `.locale`: чтение/запись, генерация.
- [ ] `.raster` ⇄ DDS, bitmap ⇄ PNG: round-trip на всём корпусе.
- [ ] RenderWare4: контейнер, меши, геометрия, скелеты, скины, анимации, материалы, морфы, растры,
      OBJ+MTL, Ogre XML; **скиннинг меша** (деформация) — не реализован.
- [ ] `.anim`: чтение (заголовок/каналы/кадры/интерполяторы) + текст; **запись** — не реализована.
- [ ] Конвертеры: `.pctp`, `.tlsa`, `.gait`, `.look_table`, `.loottable`, `.cnv` (только дамп).
- [ ] KeyMap/DDFList/расширения; hash guesser; поиск (маски/regex/содержимое).
- [ ] Диагностика (`DiagnosticSink`) и «мягкая деградация» везде.

## 2. CLI

- [ ] Пройти `--help` и каждый раздел `docs/*`; сверить usage/поведение.
- [ ] `info list extract unpack unpackdata pack prop propt locale texture bitmap guess search pctp
      tlsa gait cnv loottable looktable keymap rw4 export-models verify roundtrip diff strings pdb
      anim hash types`.
- [ ] Единый вывод: заголовки, таблицы, коды выхода, `--quiet/--no-color/--lang`.
- [ ] Локализация RU/EN (нет ли незаполненных ключей — см. тест).

## 3. GUI

- [ ] Проводник: дерево+флажки, список, превью, навигация, крошки, адресная строка, фильтр,
      сортировка, плоский режим, контекстные меню, мультивыбор, drag-in/out.
- [ ] Превью: props(XML), texture/bitmap/PNG, **3D RW4** (текстура, скелет, слайдер анимации),
      `.anim`, `trigger`-текст, hex.
- [ ] Панели: сведения (снизу), свойства (справа), экспорт всех моделей, сравнение архивов,
      загрузка names/KeyMap, переключение языка.
- [ ] Устойчивость: глобальный обработчик, `Guarded`, `%TEMP%\sporemaster-gui-crash.log`.
- [ ] `--selftest` покрывает ключевые пути; нет аварий и «пустых» превью.

## 4. Тесты

- [ ] Все зелёные (сейчас 148). Есть ли непокрытые ветки (ошибки/граничные случаи)?
- [ ] Тесты на реальных данных **пропускаются корректно**, если данных нет.

## 5. Документация

- [ ] `README.md` (обзор, команды, возможности) — актуальность.
- [ ] `AGENT.md`, `RESUME.md`, `MEMORY.md`, `HISTORY.md` — синхронность.
- [ ] `docs/*` — формат-доки и заметки; нет устаревших «не реализовано».
- [ ] Все «TODO/в планах» из `MEMORY.md` и `RESUME.md` — либо сделано, либо осознанно отложено.

## 5a. Просмотр `refs/` (источники)

- [ ] **`refs/SporeModder-FX-master/`** — пройти по исходникам (форматы, конвертеры, RW4, cell,
      `argscript`, `filestructures`, `shaders`, `simulator`, `locale`, `bitmap`/`raster`/`dds`):
      сверить, что реализовано, что нет и что можно доделать; найти недостающие детали форматов.
- [ ] **Прочие источники** — `refs/SporeEdit`, `refs/CheckAll`, `refs/EAPackageBrowser-main`,
      `refs/_AlsoOnlyIfNeed`: сверить подходы/фичи, перенять утилиты/идеи.
- [ ] **`refs/SporeMaster/`** (легаси SporeMaster 2) — проверить, не упущено ли что-то полезное
      (suffix-array поиск, обработка ошибок, UI-логика).

## 5b. Внутренности Leftovers (распакованные данные)

- [ ] **`refs/SporeUnpackedData/SporeBeta2008/Data/`** — просмотреть **содержимое** файлов (не только
      структуру): какие форматы/типы реально лежат, как отличаются от тестовых архивов.
- [ ] **`refs/SporeUnpackedData/SporeHeroWiiPCLeftOvers/Data/`** (`Data/` + `UserData/`) — то же:
      типы файлов, `DDFLists/`, `Config/`, `Locale/`, `PreBakedKeys.txt`, `README.txt`.
- [ ] Сверить реальные «внутренности» с нашими парсерами (форматы, расширения, поля); найти
      несоответствия/непокрытые типы.

### 5b.1 Что реально лежит в Leftovers (первый обзор)

- `SporeBeta2008/Data`: в основном **`.txt`** (8775 — это те самые текстовые `trigger`), далее
  `.prop`, `.css`, `.png`, `.ttf`, `.html`, `.js`, `.vp6`/`.avi` (видео), `.db`, `.pkp`, `.dat`, `.bat`.
- `SporeHeroWiiPCLeftOvers/Data`: `.prop` (978), `.txt`, **`.crt`** (54, тип `crt` = `0x2b978c46`),
  `.py`, `.bat`.
- Выводы: текст (`trigger`→`.txt`) и `.prop` доминируют; есть UI-веб (`.css/.html/.js`), шрифты (`.ttf`),
  видео (`.vp6/.avi`), и `.crt`. Полезно проверить `crt`/`db`/`pkp`/`dat` на реальных данных.
- **Найдено:** `.crt` (тип `0x2b978c46`) — это **XML** (`<?xml ...?><sporemodel>…`), т.е. текст
  (превью уже покажет как текст).
- **Найдено:** `arth`/`htra` (`0x0472329b`, в игре пути `SimulatorScripts\*.txt`) — **бинарный**
  формат с магией **`HTRA`** (проверено: `CreatureTuning.htra`); это скрипты симулятора
  (SporeModder: `ArithmeticaFile` + ANTLR-грамматика). Не текст.

### 5c. Инвентарь форматов (Core vs. SporeModder FX на 2026-10-11)
Реализовано (Core): `dbpf`, `prop`, `locale`, `raster`/`dds`, `bitmaps`, `rw4`, `anim`, `pctp`,
`tlsa`, `gait`, `cnv` (дамп), `cell` (`loottable`, `look_table`), `argscript`.

**Проверка типов (сверено с конвертерами SporeModder FX): совпадают** —
`pctp` `0x7c19aa7a`, `gait` `0x25df0112`, `tlsa` `0x4aeb6bc6`, `cnv` `0x55ada24`,
`loottable` `0xd92af091`, `look_table` `0x8c042499`, `anim` `0xee17c6ad`, `prop` `0xb1b104`,
`locale` `0x2fac0b6`, `raster` `0x2f4e681c`, `rw4` `0x2f4e681b`. (`scn` и `lvl` — один тип
`0x47b8300`.)

**Ещё не реализовано** (пакеты SporeModder FX, сверить на реальных данных перед реализацией!):
- `arth` (`0x0472329b`; **бинарный `HTRA`**, не текст!) — скрипты симулятора (`SimulatorScripts`,
  в релизе `.htra`), сплошь в `Spore_Game`/`CSA_Main` (десятки файлов);
- `creaturedata` (`0x0f43029a`, 260) — данные редактора существ;
- `scn` (`0x047b8300`) — уровни/архетипы (в тестовых архивах пока не встретился);
- `spui` (`0x250fe9a2`) — layout'ы интерфейса (есть в `CSA_Graphics`/`CSA_Main`);
- `otdb`, `gmdl` (`0x00e6bce5`), `effects`, `shaders`, `simulator`;
- под-форматы `cell` (напр. `.structure`) — референс SporeModder **не всегда совпадает** с данными.

Приоритет — по частоте/пользе; каждый **сначала проверять на реальных файлах** (правило проекта).

### 5c.1 Подписи форматов (по первым байтам, проверено)

**Приоритет по частоте** (все 15 тестовых архивов, топ типов, 2026-10-11):

| Тип | Имя | Кол-во | Статус |
|-----|-----|-------:|--------|
| `00b1b104` | `prop` | 15720 | ✅ |
| `01a527db` | `snr` | 6784 | ⬜ аудио (вне области) |
| `2f7d0004` | `png` | 3489 | ✅ (готовый PNG) |
| `5caa5e28` | `blockdata` | 3239 | ⬜ |
| `2f4e681b` | `rw4` | 2883 | ✅ |
| `ee17c6ad` | `animation` | 2436 | ✅ (чтение) |
| `02b9f662` | `audioProp` | 2121 | ✅ (вариант prop) |
| `01c135da` | `gmsh` | 965 | ⬜ старый Gonzago-меш |
| `dfad9f51` | `cell` | 412 | ⬜ (бета ≠ SporeModder) |
| `4b9ef6dc` | `structure` | 313 | ⬜ (бета ≠ SporeModder) |
| `250fe9a2` | `spui` | 284 | ⬜ |
| `497767b9` | `pfx` | 260 | ✅ текст (Pure Data) |
| `02393756` | `cur` | 136 | ⬜ курсор (картинка?) |
| `2f4e681c` | `raster` | 134 | ✅ |
| `0472329b` | `arth` | 129 | ⬜ бинарный `HTRA` |
| `1a99b06b` | `bem` | 125 | ⬜ |
| `2f7d0006` | `tga` | 119 | ⬜ картинка |
| `055ada24` | `cnv` | 111 | ✅ (дамп) |
| `024a0e52` | `trigger` | 105 | ✅ текст |
| `d92af091` | `loottable` | 101 | ✅ |
| `2b978c46` | `crt` | 93 | ✅ XML (текст) |
| `3c7e0f63` | `mcl` | 86 | ⬜ |
| `0f43029a` | `creaturedata` | 67 | ⬜ |
| `00e6bce5` | `gmdl` | 67 | ⬜ |
| `f2b924b3` | `ltp` | 52 | ⬜ |

Выводы: самое частое из непокрытого — `blockdata` (3239), `gmsh` (965), затем `cell`/`structure`
(бета-специфичны), `spui` (284), картинки `cur`/`tga` (можно конвертить как изображения). `snr` —
аудио (проприетарное, ничего не декодируем).

### 5c.2 Подписи форматов (по первым байтам)
| Тип | Формат | Первые байты / признак |
|-----|--------|------------------------|
| `0x0472329b` | `arth`/`htra` | бинарный, магия `48 54 52 41` = `HTRA` |
| `0x2b978c46` | `crt` | XML (`<?xml ...?><sporemodel>`) |
| `0x030bdee3` | `pollen_metadata` (otdb) | бинарный, начинается `00 00 00 0A 00 00 00 00 …`; в конце — свой ключ (type/group/instance BE) |
| `0x00e6bce5` | `gmdl` | бинарный, начинается `08 00 00 00 00 00 00 00 01 00 00 00` + float-ы |
| `0xea5118b0` | `effdir` (effects) | бинарный, `00 04 00 01 00 01 00 01 …` |
| `0x497767b9` | `pfx` | **текст** Pure Data (`####…`, `#N canvas …`) |
| `0x55ada24` | `cnv` | бинарный (наш парсер) |
| `0x4b9ef6dc` | `structure` | бинарный; **бета ≠ SporeModder** (offset=0) |
| `0xdfad9f51` | `cell` | бинарный; **бета ≠ SporeModder** (float + UTF-16 имя) |

### 5c.2 Наборы тестовых архивов

- **Релиз** (`_PackageFilesFinalVer`): `BoosterPack_01`, `BP2_Data`, `CSA_Audio`, `CSA_Graphics`,
  `CSA_Main`, `CSA_Pack_03`, `Spore_Game`.
- **Бета** (`_PackageFilesBetaVer`): `CellGame`, `CellGame_Layers`, `Cinematics`,
  **`CompiledMaterials`**, **`Effects`**, **`Effects_SkinPaint`**, **`Images`**, `Other`.

**Cell-форматы** (пакет `cell` SporeModder FX; сделан только `loottable`, `look_table`): ещё есть
`CellFile` (мастер, 26 КБ), `CellGlobals`, `CellPopulate`, `CellStructure`, `CellWorld`,
`CellRandomCreature`, `CellLookAlgorithm`, `CellBackgroundMap`, `CellEffectMap`, `CellPowers`.

**Таблица типов (сверено с `reg_type.txt` и конвертерами SporeModder FX):**

| Тип (`0x`) | Имя типа | SporeModder-конвертер | Наш статус |
|-----------|----------|-----------------------|-----------|
| `dfad9f51` | `cell` | CellFile | нет |
| `2a3ce5b7` | `globals` | CellGlobals | нет |
| `433fb70c` | `effectMap` | CellEffectMap | нет |
| `4b9ef6dc` | `structure` | CellStructure | нет (референс не совпал!) |
| `612b3191` | `backgroundMap` | CellBackgroundMap | нет |
| `754be343` | `powers` | CellPowers | нет |
| `9b8e862f` | `world` | CellWorld | нет |
| `da141c1b` | `populate` | CellPopulate | нет |
| `dba35ae2` | `look_algorithm` | CellLookAlgorithm | нет |
| `f9c3d770` | `random_creature` | CellRandomCreature | нет |
| `d92af091` | `loottable` | CellLootTable | **есть** |
| `8c042499` | `look_table` | CellLookTable | **есть** |
| `47b8300` | `lvl` (`scn`) | Scn | нет |
| `ea5118b0` | `effdir` (effects) | Effects | нет |
| `469a3f7` | `smt` | Smt | нет |
| `2d5c9af` | `summary` | Summary | нет |
| `30bdee3` | `pollen_metadata` (otdb) | PollenMetadata | нет |
| `366a930d` | `adv` (Scenario) | Scenario | нет |
| `6efc6aa` | `package` | DBPF | нет (это **вложенный `.package`**!) |
| `617715d9` | `pd` (Pure Data) | — (нет конвертера) | текст |

**Важное наблюдение (проверено на данных):** реальный `.structure` (бета `CellGame.package`,
`struct_eggkid.structure`, 80 байт) **НЕ совпадает** с `CellStructureFile` SporeModder: по формату
SporeModder поле `offset` (6-е int) обязано быть `28`, а в реальном файле оно `0`, и есть данные
дальше (похоже на указательный/иной layout). Вывод: `.structure` (как и предупреждал заказчик) —
**сначала разобрать по реальным файлам**, не переносить референс вслепую.

**Второе расхождение (тоже проверено):** реальный `.cell` (бета `Cell_StabberMed.cell`, 400 байт)
**НЕ совпадает** с `CellFile` SporeModder: файл начинается не с `type` (маленькое enum-число), а с
чего-то вроде `float` + **UTF-16 имени** (`Stabber…`). Итог: **бета-версии cell-форматов отличаются**
от релизных, на которые ориентирован SporeModder. Поэтому cell-форматы начинать **с беты** (реальные
данные), а не переносить код SporeModder.

## 6. Репак/игра

- [ ] `roundtrip` на всех тестовых архивах + Retail.
- [ ] Смоук-тест игры (`tools/smoke-test.ps1 -Auto`, `-w -devDirs`).
- [ ] Проверка папок игры (Main/Data) и тест-контур с батником (когда заказчик даст).

**Проверено (2026-10-11):**
- **`verify` на всех 15 тестовых архивах** (релиз+бета) — **0 проблем** на 39817 ресурсах.
- **`roundtrip` lossless** на всех: финальные (`BP2_Data`, `CSA_Graphics`, `Spore_Game`, `CSA_Main`,
  `BoosterPack_01`) и **бета** (`CellGame`, `CellGame_Layers`, `Cinematics`, `CompiledMaterials`,
  `Effects`, `Effects_SkinPaint`, `Images`); ранее — Retail-игра.
- **Смоук-тест игры** (Retail, `-w -devDirs`) — **PASS** (см. `docs/GAME_SMOKE_TEST.md`).

## 7. Гигиена репозитория

- [ ] `.gitignore` (крупные `refs/*`) корректен; нет случайных файлов (scratch-тесты, temp).
- [ ] `bin/obj` не в git; большие данные не в git.
- [ ] Коммиты осмысленные; дерево чистое.

## 8. Идеи к обсуждению

- [ ] «Проекты» (как в SporeMaster/SporeModder FX) — нужны ли (идея заказчика №9).
- [ ] Полный деманглинг C++-имён из PDB.
- [ ] `.cnv`/`.anim` запись, `scn`/`spui`/`effects`.
