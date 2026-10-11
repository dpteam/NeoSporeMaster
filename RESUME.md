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
- `docs/SPOREMODDER_FORMATS.md` — **полный каталог форматов SporeModder FX** и наш статус.
- Комментарии в коде — английские `/// <summary>`.

## 5. Снимок состояния (обновлять по мере продвижения)

- Версия — около **3.85.0**; тесты — **171/171**; дерево git чистое.
- Готово в Core: DBPF 2.0/3.0/DBBF (read/write, бета и релиз); RefPack (байт-в-байт);
  `.prop`+`.prop.xml`+`.prop_t` (все 25 типов свойств); `.locale`+авто-локали+**CSV/TSV**;
  `.raster`⇄DDS; bitmap⇄PNG (свой PNG-кодек + DXT-декодер); **TGA** и **`.cur`/ICO**→PNG; **RW4** (контейнер, меши, геометрия
  pos/uv/normal, скелеты, скины, ключевые анимации, материалы `CompiledState`, морфы/блендшейпы,
  растры + экспорт **DDS/OBJ+MTL/Ogre XML**, сэмплер анимаций, позёр скелета); `.anim` (чтение +
  текст); конвертеры `.pctp/.tlsa/.gait/.cnv(дамп)/.loottable/.look_table`, **cell `world`**,
  **otdb `summary`/`pollen_metadata`** и **`arth` (HTRA)** (все — байт-точно); KeyMap/DDFList/расширения; hash guesser (+`--validate`); поиск (маски/regex/содержимое);
  `diff`; `roundtrip`; `pdb`/`strings`; `export-models`; `list --save-names`.
- GUI (WPF): проводник (дерево+флажки+мультивыбор, список, превью, навигация, крошки, адресная строка,
  фильтр с масками/regex/содержимым, сортировка, плоский режим, контекстные меню, drag-in/out, иконки
  типов, панель сведений и **панель свойств**), превью props/текстур/PNG/`.rw4` (текстура+скелет+
  **слайдер анимации**)/`.anim`/trigger/hex, экспорт всех моделей, сравнение архивов, загрузка
  names/KeyMap, **переключение языка RU/EN**, self-test со скриншотами.
- CLI-команды (полный список): `info list(ls) extract unpack unpackdata pack prop propt locale texture
  bitmap image guess search pctp tlsa gait cnv loottable looktable cellworld cellmap summary pollen arth keymap
  rw4 export-models verify roundtrip diff strings pdb anim hash types` (+ `--lang en|ru`, `--help`, `--version`).
- Отложено (см. раздел 7 и `docs/FINAL_REVIEW.md`).

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

## 7. Что дальше (roadmap; что уже сделано — отмечено ✅)

### Сделано ✅
- ✅ Проверка репака: `roundtrip` lossless на всех архивах; **смоук-тест игры** PASS (`-w -devDirs`);
  требования игры разобраны по PDB (`docs/PACKAGE_REQUIREMENTS.md`).
- ✅ `unpackdata` выборочно (`--group/--type/--name`), тип→расширение из `KeyMap`, `DDFList`.
- ✅ GUI-проводник (флажки/мультивыбор, drag&drop, адресная строка, контекстные меню, drag-out,
  локализация RU+EN, панель сведений и свойств, экспорт всех моделей, сравнение архивов, загрузка
  names/KeyMap).
- ✅ 3D-превью RW4 (текстура, скелет, слайдер анимации).
- ✅ `.anim` — чтение (заголовок/каналы/кадры/интерполяторы) + текст; Ogre XML экспорт RW4.
- ✅ Умный поиск (маски/regex/содержимое) в GUI и CLI.
- ✅ Локализация (CLI `--lang`, GUI View→Language).
- ✅ Cell `world` (байт-точно) + `cellworld`.

### Осталось (варианты следующего)
1. **Продолжить ревизию** — `effects`/`simulator`/`otdb`/`gmdl`, код `argscript`/`filestructures`;
   «внутренности» Leftovers; финальный проход `docs/*` и «хвостов». Чек-лист: `docs/FINAL_REVIEW.md`.
2. **Разобрать бета-`populate`** (маркер 64 б) / `cell`/`structure`/`globals` по реальным данным
   (экспериментально; сейчас НЕ реализованы — Расхождения с SporeModder).
3. **3D-скиннинг меша** (`SkinMatrixBuffer`/blend) — меш гнётся по скелету (сейчас движется только скелет).
4. **Запись `.anim`** (byte-exact) — крупно, рискованно.
5. **Деманглинг C++-имён** из PDB; полное исследование строк EXE.
6. **Открытый вопрос №9:** «проекты» (как в SporeMaster/SporeModder FX) — нужны ли.
7. **`scn`/`spui`/`otdb`** и прочие форматы из инвентаря (проверять на данных).
8. **`.cnv` импорт текст→бинарный**. **`.prop_t` transform TRS-форма**. `list --save-names` (сделано).
9. **ФИНАЛЬНАЯ РЕВИЗИЯ** в самом конце (см. п.1).

## 8. Полезные ориентиры в `refs/`

- `refs/SporeModder-FX-master/` — основной референс форматов (Java).
- `refs/SporeEdit`, `refs/CheckAll`, `refs/EAPackageBrowser-main`, `refs/_AlsoOnlyIfNeed` — прочие.
- `refs/SporeUnpackedData/SporeBeta2008/Data/` — распакованные данные беты, `KeyMap.txt`,
  `*_DDFList.txt`.
- `refs/SporeUnpackedData/SporeHeroWiiPCLeftOvers/Data/` — leftovers (README.txt про DDFList).
- `refs/SporeGames/` — полные игры (для финальных тестов репака).
- Легаси `SporeMaster/` — исходники SporeMaster 2 для сверки (не трогать).
