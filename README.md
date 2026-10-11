# SporeMaster 3

> Инструмент для исследования и модификации игровых архивов Spore (`.package`, формат DBPF).
> Переосмысленная и обновлённая версия классического **SporeMaster 2.0** с учётом наработок
> **SporeModder FX**. Целевая платформа — **.NET 9**.

**Язык интерфейса программы — английский.** Вся документация ведётся **на русском**.
Полная локализация приложения (в т.ч. на русский) вынесена в список идей — см. `MEMORY.md`.

---

## Что уже умеет

- **Чтение архивов DBPF** всех встречающихся в Spore вариантов:
  - обычные `DBPF` и большие `DBBF` (64-битные смещения);
  - версии заголовка/индекса 1.x, 2.x и 3.x — включая **бета-версию игры** (DBPF 2.0 с общими
    полями индекса);
  - защищённый контейнер `DBPP` (диагностика, best-effort).
- **Запись архивов** с выбором версии (2.0 / 3.0), с RefPack-сжатием и в «большом» формате.
- **Распаковка и сборка** архивов с восстановлением имён ресурсов по реестрам SporeModder FX.
- **RefPack / QFS** — полный порт компрессора и декомпрессора (байт-совместимо с эталоном).
- **Свойства (`.prop`)** — чтение/запись бинарного формата **байт-в-байт**, конвертация в
  понятный XML (`.prop.xml`) и в ArgScript-текст (`.prop_t`, формат SporeModder FX).
  Поддержаны все типы значений.
- **Локали (`.locale`)** — чтение/запись текстовых таблиц локализации и **авто-генерация**
  таблиц из текстов-заглушек свойств при упаковке (как в SporeMaster 2).
- **Текстуры (`.raster` ⇄ DDS)** — конвертация растровых текстур Spore в стандартный DDS и
  обратно (байт-точно), включая блочные форматы (DXT/ATI) и мипмапы.
- **RenderWare 4 (`.rw4`)** — разбор контейнера (заголовок, манифест, таблица секций, типы),
  чтение мешей и **декодирование геометрии** (позиции + индексы) с экспортом в **OBJ** и **Ogre XML**
  (плюс **`.mtl`**/`.material` и текстуры в **DDS**); чтение
  **скелетов** (иерархия костей), **скинов** (матрицы, bind-позы), **ключевых анимаций**
  (каналы, кадры, таблица «id → анимация»), **материалов** (декомпиляция `CompiledState`:
  шейдер, цвета, render/stage/sampler states, текстуры), **морфов/блендшейпов** и метаданных
  **текстур-растров** (формат, размер, мипы).
- **Bitmap (`.bitImage`/`8bitImage`/`32bitImage`/`48bitImage` ⇄ PNG)** — конвертация «сырых»
  растровых ресурсов Spore в PNG и обратно. PNG-кодек написан нами (только BCL+zlib).
- **TGA и значки/курсоры Windows** (`tga`, `cur`/ICO ⇄ PNG) — декодирование в PNG и превью в GUI.
- **Восстановление имён по хешам** — «hash guesser» с обращением FNV и словарями фрагментов
  (`data/guess_left.txt`, `data/guess_right.txt`); пакетный режим восстанавливает все
  неизвестные хеши архива и сохраняет их в `names.txt` для повторного использования.
- **KeyMap** — конвертация карт ключей игры (`KeyMap.txt` → `names.txt`) в авторитетный реестр имён
  (точнее фрагментного guesser'а); **валидация гиссера** на реальных именах.
- **Экспериментальная распаковка в `Data`** (`unpackdata`) — раскладка по реальным игровым путям
  из карты ключей (см. `docs/UNPACK_DATA.md`).
- **Поиск по тексту** — полнотекстовый поиск по распакованным файлам (регистронезависимый),
  с номерами строк и сниппетами. В GUI/`list` — **маски** (`*.rw4`), **regex** (`re:...`) и поиск
  **по содержимому** (`text:`/`hex:`).
- **Экспорт списка ресурсов** в CSV/JSON; **очистка авто-локалей** при распаковке.
- **Сравнение двух архивов** (`diff`) — добавочные/удалённые/изменённые ресурсы по ключу и
  содержимому (не зависит от порядка, сжатия и паддинга) — удобно для модов и проверки репака.
- **Проверка репака** (`roundtrip`) — пересборка архива нашим писателем и проверка, что результат
  **идентичен** (по ключу и содержимому); проходит на бета- и релизных архивах. Плюс смоук-тест в
  самой игре (`tools/smoke-test.ps1`, критерий — отсутствие дамп-файлов) и разбор требований игры к
  пакету по PDB (`docs/PACKAGE_REQUIREMENTS.md`).
- **Выборочная распаковка** (`unpack`/`unpackdata --group --type --name`) — выбрать несколько
  папок (групп), отдельные типы или подстроку имени вместо всего архива.
- **Конвертеры**: part-capabilities (`.pctp` ⇄ `.pctp_t`), анимационные группы (`.tlsa` ⇄
  `.tlsa_t`), походка (`.gait` ⇄ `.gait_t`), беседы (`.cnv` → `.cnv_t`, дамп), таблицы добычи
  (`.loottable` ⇄ `.loottable_t`) и «взгляда» (`.look_table` ⇄ `.look_table_t`); плюс `.world`
  (cell world — дамп, чтение/запись байт-точно).
- **Локали**: `locale` — просмотр, экспорт в **CSV/TSV** и импорт обратно (для переводчиков).
- **Анимации `.anim`** (`anim`) — разбор заголовка, событий и каналов (имена костей, число
  кадров) и **текстовый дамп** (`.anim_t`); см. `docs/ANIM_FORMAT.md`.
- **Экспорт моделей** (`export-models`) — пакетная выгрузка всех декодируемых RW4-моделей архива в
  OBJ (+`.mtl`+текстуры) или Ogre XML.
- **Реверс-инжиниринг игры** (`pdb`, `strings`) — чтение отладочных символов PDB (MSF): модули,
  имена функций и **декодированные строковые литералы** MSVC; извлечение строк из EXE/DLL
  (см. `docs/PDB_RESEARCH.md`).
- **Хеши именования** — FNV-1 (как в SporeMaster 2 и SporeModder FX).
- **Реестры имён** `reg_type`, `reg_file`, `reg_property`, `reg_simulator` — встроены в сборку.
- **Устойчивость к ошибкам**: битые и обрезанные архивы не роняют программу, а дают диагностику.
  GUI дополнительно защищён глобальным обработчиком исключений (лог в `%TEMP%\sporemaster-gui-crash.log`).
- **Локализация**: интерфейс CLI — `--lang en|ru`; GUI — **View → Language** (сохранение в
  `%APPDATA%\SporeMaster3\settings.txt`). Все строки — в `lang/en.lang` и `lang/ru.lang`.
- **Headless-тесты**: собственный тест-раннер без сторонних библиотек (133 теста на реальных
  игровых архивах).

Не реализовано (в планах, см. `MEMORY.md`): перекомпиляция материалов и блендшейпов RenderWare4,
запись `.anim`, полный список конвертеров.
---

## Структура репозитория

```
SporeMaster3/
├─ SporeMaster3.sln            решение .NET 9
├─ Directory.Build.props       общие свойства сборки
├─ data/                       реестры имён и словари фрагментов (reg_*.txt, guess_*.txt)
├─ docs/                       техническая документация (RU)
├─ src/
│  ├─ SporeMaster.Core/        ядро: DBPF, RefPack, хеши, свойства, локали, текстуры, RW4
│  ├─ SporeMaster.Cli/         headless-утилита командной строки (`spore`)
│  └─ SporeMaster.Gui/         графический интерфейс (WPF, net9.0-windows)
├─ tests/
│  └─ SporeMaster.Tests/       собственный тест-раннер и тесты
├─ tools/
│  └─ smoke-test.ps1           смоук-тест игры (проверка репака в самой игре)
├─ refs/                       эталонные материалы и тестовые .package (не в сборке)
├─ SporeMaster/                исходники SporeMaster 2.0 (легаси, для сверки)
├─ RESUME.md                   шпаргалка для быстрого возобновления работы
├─ AGENT.md                    правила работы над проектом
├─ HISTORY.md                  история изменений
├─ MEMORY.md                   «память» проекта: решения, дорожная карта, идеи
└─ PROMPT.md                   постановка задачи
```

---

## Сборка

Нужен установленный **.NET SDK 9.0** (проверялось на `9.0.311`).

```powershell
dotnet build SporeMaster3.sln -c Release
```

Собрать и запустить тесты:

```powershell
dotnet run --project tests/SporeMaster.Tests -c Release
```

Тесты читают реальные архивы из `refs/_PackageFilesBetaVer` и `refs/_PackageFilesFinalVer`.
Если этих папок нет — связанные тесты сообщат об этом.

---

## Использование CLI

Утилита называется `spore`.

### Графический интерфейс

```powershell
dotnet run --project src/SporeMaster.Gui
# либо передать архив сразу:
dotnet run --project src/SporeMaster.Gui -- путь\к\архиву.package
```

Окно-обозреватель в стиле **Проводника Windows**:

- слева — **дерево папок** (групп комплекта), по центру — **список «файлов»** (ресурсов),
  справа — **превью**; у папок **флажки** (стандартная логика дерева: у корня «пакета» —
  трёхпозиционный «выбрать всё»: отмечено всё / часть (неопределённо) / ничего) — отметить папки и
  извлечь их разом (Tools → Extract Checked Folders);
- **drag&drop**: перетащите `.package` в окно, чтобы открыть; **перетащите ресурс из списка** в
  проводник (или другой файловый менеджер), чтобы экспортировать его (с обычной конвертацией);
- **адресная строка** (как в проводнике): выпадающий список папок, наберите имя папки (или `all`)
  и нажмите Enter, чтобы перейти;
- **иконки по типам** ресурсов в списке (свойство, модель, текстура, локаль, данные);
- **панель сведений** внизу (как в проводнике): имя, тип, ключ, размеры, путь выбранного ресурса
  (View → Details pane);
- **панель свойств** (View → Properties panel): подробности выбранного ресурса — файл/версия/тип
  индекса/число записей, путь, тип, группа, инстанс, ключ, индексы и размеры;
- навигация **назад/вперёд/вверх**, «хлебные крошки» (строка пути);
- **Edit → Select All / Invert Selection**; при выборе нескольких ресурсов панель сведений
  показывает суммарные размеры (как в проводнике);
- фильтр-поиск, сортировка по столбцам (клик по заголовку), режим «плоский список»
  (Tools → Back to single list); поиск поддерживает **маски** (`*.rw4`), **regex** (`re:...`) и
  **по содержимому** (`text:...` / `hex:...` — по Enter);
- контекстное меню: извлечь ресурс/папку, копировать ключ/имя, открыть в превью, **«Открыть как
  пакет»** (для вложенных `.package` ресурсов);
- ПКМ по папке в дереве: открыть, отметить/снять флажок, извлечь папку, копировать имя папки;
- **File → Back to previous package** и **Up** на верхнем уровне возвращают к родительскому
  пакету (для вложенных `.package`);
- превью: свойства как XML, текстуры/bitmap/PNG как изображение, **текстуры `.rw4`** как изображение,
  **3D-модели `.rw4`**
  (интерактивный просмотр: вращение мышью, зум колесом, **диффузная текстура** на модели,
  наложение **скелета** — View → Show skeleton — и **слайдер времени** для проигрывания анимации
  скелета), RenderWare4 как сводка (меши,
  скелеты, скины, анимации, материалы, морфы) + список секций, **анимации `.anim`** как список
  каналов/событий, `trigger` как текст, иначе — hex;
- извлечение выбранного (в т.ч. **нескольких** сразу)/всех/папки с автоматической конвертацией
  (`.prop.xml`, `.dds`, `.png`);
- **Экспорт всех моделей** (Tools → Export all models → OBJ/Ogre XML) с материалами и текстурами;
- **Сравнение архивов** (Tools → Compare with package...) — добавочные/удалённые/изменённые ресурсы;
- **Загрузка карты ключей** (Tools → Load game KeyMap.txt...) — авторитетные игровые имена;
- через меню **Tools → Load recovered names.txt** можно подключить восстановленные имена.

У GUI есть режим самопроверки (создаёт окно, открывает архив, строит превью и завершает работу
без показа интерфейса — удобно для CI):

```powershell
dotnet run --project src/SporeMaster.Gui -- --selftest путь\к\архиву.package
```

Отчёт пишется в `%TEMP%\sporemaster-gui-selftest.txt`.

### Команды CLI

```powershell
# Информация об архиве
dotnet run --project src/SporeMaster.Cli -- info <архив.package>

# Список ресурсов (с фильтрами: имя — маски/regex; --content — поиск по байтам)
dotnet run --project src/SporeMaster.Cli -- list <архив.package> --type prop --limit 20
dotnet run --project src/SporeMaster.Cli -- list <архив.package> --name "*.rw4"
dotnet run --project src/SporeMaster.Cli -- list <архив.package> --content "text:property"

# Распаковать всё (свойства сохраняются как .prop.xml, имена — по реестрам)
dotnet run --project src/SporeMaster.Cli -- unpack <архив.package> <папка>

# Байтово-точная распаковка (ключи в пути _raw/type/group/instance.bin, без конвертаций).
# Используйте её, если после правок нужен побайтово идентичный репак:
#   unpack --raw → (правки) → pack → diff  =>  0 различий
dotnet run --project src/SporeMaster.Cli -- unpack <архив.package> <папка> --raw

# Собрать архив из папки (.prop.xml автоматически превращаются обратно в .prop)
dotnet run --project src/SporeMaster.Cli -- pack <папка> <архив.package> --version 2.0 --compress

# Конвертировать один файл свойств в XML и обратно
dotnet run --project src/SporeMaster.Cli -- prop <файл.prop> <файл.prop.xml>
dotnet run --project src/SporeMaster.Cli -- prop <файл.prop.xml> <файл.prop>

# Конвертировать свойства в ArgScript-текст (.prop_t, формат SporeModder) и обратно
dotnet run --project src/SporeMaster.Cli -- propt <файл.prop> <файл.prop_t>
dotnet run --project src/SporeMaster.Cli -- propt <файл.prop_t> <файл.prop>

# Посмотреть таблицу локализации
dotnet run --project src/SporeMaster.Cli -- locale <файл.locale>

# Конвертировать текстуру (raster ⇄ DDS, определяется автоматически)
dotnet run --project src/SporeMaster.Cli -- texture <файл.raster> <файл.dds>
dotnet run --project src/SporeMaster.Cli -- texture <файл.dds> <файл.raster>

# Разобрать структуру ресурса RenderWare 4 (и выгрузить меши в OBJ или Ogre XML, текстуры в DDS)
dotnet run --project src/SporeMaster.Cli -- rw4 <файл.rw4>
dotnet run --project src/SporeMaster.Cli -- rw4 <файл.rw4> --export <файл.obj>            # + .mtl и текстуры
dotnet run --project src/SporeMaster.Cli -- rw4 <файл.rw4> --export-ogre <файл.mesh.xml>  # + .material
dotnet run --project src/SporeMaster.Cli -- rw4 <файл.rw4> --export-textures <папка>

# Выгрузить ВСЕ декодируемые модели RW4 из архива (OBJ+MTL или Ogre XML) с текстурами
dotnet run --project src/SporeMaster.Cli -- export-models <архив.package> <папка> --format obj|ogre

# Конвертировать растровый ресурс (bitmap ⇄ PNG, определяется автоматически)
dotnet run --project src/SporeMaster.Cli -- bitmap <файл.32bitImage> <файл.32bitImage.png>
dotnet run --project src/SporeMaster.Cli -- bitmap <файл.32bitImage.png> <файл.32bitImage>

# Конвертировать картинку TGA или значок/курсор Windows (.cur/.ico) в PNG
dotnet run --project src/SporeMaster.Cli -- image <файл.tga> <файл.png>
dotnet run --project src/SporeMaster.Cli -- image <файл.cur> <файл.png>

# Восстановить имя по хешу
dotnet run --project src/SporeMaster.Cli -- guess 0x73BF81A5

# Восстановить все неизвестные имена в архиве и сохранить их
dotnet run --project src/SporeMaster.Cli -- guess --package <архив.package> --save names.txt

# Извлечь авторитетные имена из карты ключей игры
dotnet run --project src/SporeMaster.Cli -- keymap <KeyMap.txt> names.txt --types types.txt --extensions ext.txt

# Экспериментальная полная распаковка в игровой вид (реальные пути из карты ключей)
dotnet run --project src/SporeMaster.Cli -- unpackdata <архив.package> <папка> --keymap <KeyMap.txt>
# ...только каталоги выбранной сборки (DDFList) — с картой ключей
dotnet run --project src/SporeMaster.Cli -- unpackdata <архив.package> <папка> --keymap <KeyMap.txt> --ddflist <Spore_DDFList.txt>

# Экспортировать список ресурсов и найти текст
dotnet run --project src/SporeMaster.Cli -- list <архив.package> --format csv --out resources.csv
dotnet run --project src/SporeMaster.Cli -- search <папка> "anim_dilation" --ext .prop.xml
dotnet run --project src/SporeMaster.Cli -- search <папка> "re:property\s+\w+" --regex
dotnet run --project src/SporeMaster.Cli -- search <папка> "0B0A2C" --hex

# Конвертировать часть (part capabilities)
dotnet run --project src/SporeMaster.Cli -- pctp <файл.pctp> <файл.pctp_t>

# Конвертировать таблицы анимационных групп (TLSA)
dotnet run --project src/SporeMaster.Cli -- tlsa <файл.tlsa> <файл.tlsa_t>

# Конвертировать параметры походки (GAIT)
dotnet run --project src/SporeMaster.Cli -- gait <файл.gait> <файл.gait_t>

# Сделать читаемый дамп беседы (CNV)
dotnet run --project src/SporeMaster.Cli -- cnv <файл.cnv> <файл.cnv_t>

# Конвертировать таблицу добычи (loot table)
dotnet run --project src/SporeMaster.Cli -- loottable <файл.loottable> <файл.loottable_t>

# Конвертировать таблицу «взгляда» клетки (look table)
dotnet run --project src/SporeMaster.Cli -- looktable <файл.look_table> <файл.look_table_t>

# Дамп файла cell world (populate/advect)
dotnet run --project src/SporeMaster.Cli -- cellworld <файл.world> <файл.txt>

# Дамп метаданных редактора существа (.summary / .pollen_metadata)
dotnet run --project src/SporeMaster.Cli -- summary <файл.summary> <файл.txt>
dotnet run --project src/SporeMaster.Cli -- pollen <файл.pollen_metadata> <файл.txt>

# Дамп программы Arithmetica (HTRA): операции, функции, строки
dotnet run --project src/SporeMaster.Cli -- arth <файл.htra> <файл.txt>

# Дамп уровня/сценария Spore (.scn/.lvl): маркеры с позицией/ориентацией
dotnet run --project src/SporeMaster.Cli -- scn <файл.lvl> <файл.txt>

# Дамп карт клетки (background/effect/random_creature)
dotnet run --project src/SporeMaster.Cli -- cellmap background <файл.backgroundMap> <файл.txt>

# Дамп cell populate / structure (релиз)
dotnet run --project src/SporeMaster.Cli -- cellmap populate <файл.populate> <файл.txt>
dotnet run --project src/SporeMaster.Cli -- cellmap structure <файл.structure> <файл.txt>

# Дамп сохранения редактора существ (способности и морфы)
dotnet run --project src/SporeMaster.Cli -- creaturedata <файл.creaturedata> <файл.txt>

# Дамп заголовка UI-разметки Spore (.spui): ресурсы, hit-mask, классы
dotnet run --project src/SporeMaster.Cli -- spui <файл.spui> <файл.txt>

# Дамп каталога эффектов (.effdir): секции компонентов и ресурсов
dotnet run --project src/SporeMaster.Cli -- effdir <файл.effdir> <файл.txt>

# Распаковать, убрав сгенерированные локали (они пересоберутся при упаковке)
dotnet run --project src/SporeMaster.Cli -- unpack <архив.package> <папка> --clean-autolocales

# Распаковать выборочно: несколько папок (групп), типы или подстрока имени
dotnet run --project src/SporeMaster.Cli -- unpack <архив.package> <папка> --group creature_parts,animations~ --raw
dotnet run --project src/SporeMaster.Cli -- unpack <архив.package> <папка> --type prop --name brute

# Реальная lossless-перепаковка: --raw раскладывает по числовому ключу (_raw/type/group/instance.bin),
# pack собирает обратно по ключам (без зависимости от имён)
dotnet run --project src/SporeMaster.Cli -- unpack <архив.package> <папка> --raw
dotnet run --project src/SporeMaster.Cli -- pack <папка> <repack.package> --compress
dotnet run --project src/SporeMaster.Cli -- diff <архив.package> <repack.package>

# Проверить целостность (прочитать и распаковать все ресурсы)
dotnet run --project src/SporeMaster.Cli -- verify <архив.package>

# Проверка репака: пересобрать нашим писателем и убедиться, что результат идентичен
dotnet run --project src/SporeMaster.Cli -- roundtrip <архив.package> --compress

# Смоук-тест в игре: пересобрать архив игры, запустить и проверить отсутствие дамп-файлов
powershell -ExecutionPolicy Bypass -File tools\smoke-test.ps1

# Сравнить два архива (добавленные/удалённые/изменённые ресурсы)
dotnet run --project src/SporeMaster.Cli -- diff <a.package> <b.package>

# Разобрать анимацию (.anim): каналы, события, длительность
dotnet run --project src/SporeMaster.Cli -- anim <файл.anim> --channels
# Выгрузить анимацию в читаемый текст (.anim_t)
dotnet run --project src/SporeMaster.Cli -- anim <файл.anim> <файл.anim_t>

# Реверс-инжиниринг: символы из PDB и строки из EXE/DLL
dotnet run --project src/SporeMaster.Cli -- pdb <SporeApp.pdb> --strings --save literals.txt
dotnet run --project src/SporeMaster.Cli -- strings <SporeApp.exe> --min 6 --filter Spore

# Хеш имени и список известных типов
dotnet run --project src/SporeMaster.Cli -- hash prop
dotnet run --project src/SporeMaster.Cli -- types
```

Глобальные параметры: `--lang <код>` (напр. `--lang ru`), `--quiet`, `--no-color`, `--help`, `--version`.

Дополнительные источники имён (для любой команды): `--names <names.txt>` (восстановленные/проектные
имена) и `--keymap <KeyMap.txt>` (авторитетные имена из карты ключей игры). Пример:
`spore list <архив.package> --keymap <KeyMap.txt>`.

---

## Совместимость форматов

| Формат | Чтение | Запись | Примечание |
|--------|:------:|:------:|-----------|
| DBPF 1.x (Sims 2) | частично | — | индекс без заголовка-таблицы |
| **DBPF 2.0 (бета Spore)** | да | да | общий индекс, встречаются общие type/group |
| DBPF 3.0 (релиз) | да | да | тип индекса `0x04` |
| DBBF (большой) | да | да | 64-битные смещения |
| DBPP (protected) | диагностика | — | best-effort |
| RefPack / QFS | да | да | байт-совместимо |

---

## Лицензия и происхождение

Проект основан на открытых исходниках SporeMaster 2.0 и идеях SporeModder FX. Перед публикацией
результатов убедитесь, что соблюдаете условия исходных лицензий (см. `refs/…/LICENSE.md`).
