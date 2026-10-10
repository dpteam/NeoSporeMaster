# RESUME.md — шпаргалка для возобновления работы (после сброса контекста)

> Прочитай этот файл **первым**, если чат сбросился или контекст потерян. Здесь — только самое
> важное: как собрать, как тестировать, где что лежит, что уже готово и что делать дальше.
> Детали — в `README.md`, `AGENT.md`, `MEMORY.md`, `HISTORY.md`, `docs/*`.

## 0. TL;DR

**SporeMaster 3** — современный (.NET 9, только BCL, без NuGet) инструментарий для чтения/записи
архивов Spore `.package` (DBPF), плюс GUI-проводник и CLI `spore`. Документация — по-русски,
интерфейс — по-английски. Репозиторий: `E:\_DPT_Projects_E\_Harness\SporeMaster3`.

## 1. Быстрый старт (команды)

```powershell
cd "E:\_DPT_Projects_E\_Harness\SporeMaster3"

# Сборка (должна быть без ошибок)
dotnet build SporeMaster3.sln -c Release -v q -nologo

# Все тесты (свой раннер, без NuGet). Фильтр — подстрока имени.
dotnet run --project tests/SporeMaster.Tests -c Release --filter "RW4"

# CLI (exe "spore")
dotnet run --project src/SporeMaster.Cli -- help
dotnet run --project src/SporeMaster.Cli -- list <архив.package>

# GUI (WPF, самопроверка без показа окна; отчёт в %TEMP%\sporemaster-gui-selftest.txt)
dotnet run --project src/SporeMaster.Gui -- --selftest <архив.package>
```

Коммит: `git add -A; git -c user.name="SporeMaster3" -c user.email="sporemaster3@example.invalid" commit -m "..."`.
При коммите PowerShell сыпет `warning: LF will be replaced by CRLF` — это норма, не ошибка.

## 2. Карта репозитория

```
SporeMaster3.sln
src/SporeMaster.Core      вся логика форматов (без UI)
  Dbpf/       DBPF (read/write), RefPack, ResourceKey, ResourceTypeIds
  Properties/ .prop (binary) + .prop.xml; PropertyScript (.prop_t)
  Locale/     .locale + авто-локали
  Textures/   .raster ⇄ DDS; Bitmaps/ .*Image ⇄ PNG (свой PNG-кодек)
  RenderWare4/ контейнер, меши, геометрия, скелеты, скины, анимации, материалы, морфы, OBJ
  Hashes|Names FNV, реестры reg_*, hash guesser, KeyMap
  Pctp|Tlsa|Gait|Cnv|Cell(loottable|look_table)  конвертеры
  Presentation/ PackageSession, ResourceItem, ExplorerModel, PreviewEngine, ResourceGlyph
  Search/     полнотекстовый поиск
  Diff/       сравнение архивов (ArchiveDiff)
  Pdb/ Binary/ реверс-инжиниринг: MSF/PDB, MSVC-строки, strings из бинарников
  lang/en.lang  строки CLI/интерфейса (английский)
src/SporeMaster.Cli       exe "spore" (Program.cs диспетчер, Commands.cs)
src/SporeMaster.Gui       WPF net9.0-windows, exe "SporeMaster", иконка SporeMaster3.ico
tests/SporeMaster.Tests   собственный раннер [SporeTest], Assert, exit code
tools/smoke-test.ps1      смоук-тест игры (проверка репака в игре)
data/                     reg_*.txt (embedded) + guess_left/right.txt (~20 МБ, рядом с exe)
docs/*.md                 техническая документация (RU)
refs/                     справочные материалы и реальные архивы (НЕ трогать/не коммитить крупное)
```

## 3. Незыблемые правила

1. **.NET 9, только BCL.** Никаких NuGet (ни для тестов, ни для парсинга аргументов).
2. **Отказоустойчивость**: Core — `DiagnosticSink` (Warn, продолжать); CLI — `try/catch` + код
   выхода; GUI — `Guarded(...)` на каждый обработчик + глобальный `DispatcherUnhandledException`.
   Исключения — только для «это вообще не наш формат».
3. **GUI обязан собираться с `InvariantGlobalization=false`** — иначе WPF падает на
   `XmlLanguage.GetSpecificCulture()` при отрисовке `GridView`.
4. **Документация — RU, интерфейс — EN.** Видимые пользователю строки CLI — из `Localizer`
   (`src/SporeMaster.Core/lang/en.lang`).
5. **Проверяемость**: правки ядра сопровождаются headless-тестами. Тесты на реальных данных
   должны **корректно пропускаться**, если данных нет.
6. **Не выдумывать форматы вслепую** — сверять с `refs/` и реальными архивами. Если референс не
   подтверждается на данных, формат не реализуем (например, `.structure`).
7. Не трогаем `refs/` и легаси `SporeMaster/`; не коммитим `bin/`, `obj/`, крупные папки.

## 4. Где что документируется (после каждой правки)

- `HISTORY.md` — человекочитаемый список изменений (сверху свежее); обновлять **всегда**.
- `MEMORY.md` — решения, «почему так», идеи заказчика, проверенные факты.
- `README.md` — обзор, список возможностей и примеры команд.
- `docs/ARCHITECTURE.md` — структура Core по папкам.
- `docs/*_FORMAT.md` — заметки по конкретным форматам (PROP, CNV, GAIT, TLSA, PCTP, LOOKTABLE,
  HASH_GUESSING, RW4_FORMAT, UNPACK_DATA, PDB_RESEARCH).
- Комментарии в коде — английские `/// <summary>`.

## 5. Снимок состояния (обновлять по мере продвижения)

- Версия — около **3.77.0**; тесты — **147/147**; дерево git чистое.
- Готово в Core: DBPF 2.0/3.0 (в т.ч. бета) read/write; RefPack (байт-в-байт); `.prop`+`.prop.xml`;
  `.prop_t`; `.locale`+авто-локали; `.raster`⇄DDS; bitmap⇄PNG; RW4 (контейнер, меши, геометрия
  pos/uv/normal + OBJ, скелеты, скины, ключевые анимации, **материалы** `CompiledState`, морфы/
  блендшейпы, метаданные текстур-растров + экспорт текстур в DDS и OBJ+MTL); `.anim` (чтение: заголовок/события/каналы + ключевые кадры компонент + интерполяторы); 3D-превью моделей (с текстурами); hash guesser + `guess --validate`;
  KeyMap; полнотекстовый поиск; `diff`; `pdb`, `strings` (реверс-инжиниринг). CLI: `info list extract
  unpack unpackdata pack prop propt locale texture bitmap guess search pctp tlsa gait cnv loottable
  looktable keymap rw4 anim verify diff strings pdb hash types`.
- GUI: проводник (дерево папок, список, превью, назад/вперёд/вверх, крошки, фильтр, сортировка,
  плоский режим, контекстные меню, извлечение, **иконки по типам**, **панель сведений**).
- Отложено (список ниже).

## 6. Грабли и подводные камни (проверено болью)

- **Смешанная эндианность** форматов: `.prop` — дескриптор+скаляры big-endian, композиты
  little-endian; `.cnv` — счётчики/действия LE, длины/ключи/text-id BE; `.tlsa` — целиком BE
  (магия `"tsla"`); заголовок bitmap — **big-endian** (частая ошибка!).
- Идентификаторы **type/property — это константы EA** (не FNV). FNV хеширует группы/имена.
  Например `loottable` = `0xD92AF091`, `trigger` = `0x024A0E52`.
- **RW4-ссылки**: старшие 2 бита = тип ссылки (`0` — индекс секции, `1` — «нет объекта»,
  `2` — подссылка). Значение `1<<22` встречается в реальных файлах — не считать ошибкой.
- `unpack→pack→verify` **не байт-точно** через XML (паддинг/флаги). Не заявлять lossless-репак.
  Но **`unpack --raw → pack → diff` — lossless** (по ключам/содержимому): layout
  `_raw/{type}/{group}/{instance}.bin`, pack собирает ключи из пути (без имён).
- Распаковка по умолчанию **lossless**; `--clean-autolocales` — опционально.
- В C# `"\x1aD"` — это `\x1aD` (жадный hex!); для байтов писать массив байт.
- Реальные данные лежат в `refs/_PackageFilesBetaVer` (бета) и `refs/_PackageFilesFinalVer` (релиз);
  крупные папки (`refs/SporeGames`, `refs/SporeUnpackedData`, `_PackageFiles*`) — в `.gitignore`.
- **Lossless raw-перепаковка подтверждена:** `unpack --raw → pack --compress → diff` идентично на
  `BP2_Data` 184/184, `CSA_Graphics` 5822/5822, **`Spore_Game` 11676/11676, `CSA_Main` 8431/8431**,
  а также на **реальной Retail-игре** (`refs/SporeGames/SporeRetail/Data`): `Spore_Game` 11529/11529,
  `Spore_Pack_03` 266/266.
- `refs/SporeUnpackedData/**/KeyMap.txt` — карта «ключ → реальный путь» (авторитетные имена);
  `*.trigger`/`.txt` (тип `0x024A0E52`) — **обычный текст** (это `Config/*.txt`).
- `refs/SporeGames/SporeBeta190208/SporeApp.pdb` — MSF 7.00 с 429k символов; инструмент `pdb`.
- **3D-превью RW4**: меши + диффузная текстура + **наложение скелета** (View → Show skeleton).
  Часть моделей **без статической геометрии**
  (процедурные/анимированные, ссылка на вершины `1<<22`) — для них показывается пояснение.
- «Зелёная» текстура у некоторых деталей — это реальный **tint-масочный** цвет модели, не баг.

## 7. Что дальше (roadmap, сверху — приоритетнее; см. MEMORY.md §3)

1. **Проверка репака оригиналов игры** — **сделано** (3.59.0–3.72.0):
   - **lossless-перепаковка** (`roundtrip`) подтверждена на `BP2_Data` 184/184, бета `CellGame`
     442/442, `CSA_Graphics` 5822/5822, `Spore_Game` 11676/11676, `CSA_Main` 8431/8431 и Retail
     (`Spore_Game` 11529/11529, `Spore_Pack_03` 266/266);
   - **смоук-тест в игре** (`tools/smoke-test.ps1 -Auto`, запуск `-w -devDirs`): репак Retail
     `Spore_Game` → игра **работала 60 с (не вылетела)**, **дампов нет** → PASS;
   - **требования игры к пакету** разобраны по PDB: `docs/PACKAGE_REQUIREMENTS.md`.
2. **Улучшить распаковку/`unpackdata`** — выборочно — **сделано** (`--group/--type/--name`, 3.36.0);
   таблица «тип → расширение» из `KeyMap` — **сделано** (3.40.0); **`DDFList`** — **сделано**
   (3.42.0, `unpackdata --ddflist` — только каталоги сборки). Дальше: пометка текстовых типов,
   мультивыбор папок в GUI. (Идея заказчика №8 — «точно нужно».)
3. **GUI** (Explorer-like): **мультивыбор папок (флажки), drag&drop, адресная строка, контекстное
   меню дерева, экспорт перетаскиванием, локализация RU+EN готовы** (3.37.0–3.62.0).
4. **`.anim` (SPAnimation)** — чтение заголовка/событий/каналов, **ключевых кадров компонент и
   интерполяторов**, **текстовый дамп** (`.anim_t`) готовы (3.46.0, 3.63.0); осталось: запись.
   **Ogre XML** экспорт RW4 готов (3.61.0).
5. **Деманглинг C++-имён** из PDB (чтобы читать `?…@@`), плюс дальнейшее исследование строк EXE.
6. **Открытый вопрос**: делать ли «проекты» (как в SporeMaster/SporeModder FX) или обойтись —
   см. MEMORY.md §4, идея заказчика №9. Решить при проработке сборки модов.
7. **Просмотр 3D-моделей** — **сделано** (3.56.0–3.75.0): 3D RW4 с текстурой, **скелет** (View →
   Show skeleton) и **слайдер времени** (поза скелета). Скиннинг меша (деформация вершин) — позже.
8. **`SPUI`** — layout'ы интерфейса (позже, вместе с cell/UI-форматами).
9. **Смоук-тест игры** — **сделано** (3.60.0, прогнан 3.72.0): `tools/smoke-test.ps1`
   (бэкап → lossless репак → подмена → запуск Retail-игры `-w -devDirs` → проверка новых
   `*.mdmp`/`*exception*.txt` → восстановление). См. `docs/GAME_SMOKE_TEST.md`.
   **Запуск игры всегда с `-w -devDirs`** (иначе своя ошибка ~2001 о ненайденных package).
10. **ФИНАЛЬНАЯ РЕВИЗИЯ (в конце).** Пройтись по всему проекту: перепроверить функции/форматы/
    команды/меню, найти что улучшить/доделать/починить, недостающие тесты и доки; причесать UI/UX;
    закрыть хвосты из списка идей. **Чек-лист: `docs/FINAL_REVIEW.md`.**
11. **3D: скины/деформация меша** — скиннинг вершин (`SkinMatrixBuffer`/blend-данные), чтобы меш
    гнулся по скелету (сейчас двигается только скелет). **Позже.**

## 8. Полезные ориентиры в `refs/`

- `refs/SporeModder-FX-master/` — основной референс форматов (Java).
- `refs/SporeEdit`, `refs/CheckAll`, `refs/EAPackageBrowser-main`, `refs/_AlsoOnlyIfNeed` — прочие.
- `refs/SporeUnpackedData/SporeBeta2008/Data/` — распакованные данные беты, `KeyMap.txt`,
  `*_DDFList.txt`.
- `refs/SporeUnpackedData/SporeHeroWiiPCLeftOvers/Data/` — leftovers (README.txt про DDFList).
- `refs/SporeGames/` — полные игры (для финальных тестов репака).
- Легаси `SporeMaster/` — исходники SporeMaster 2 для сверки (не трогать).
