# Phantom Installer

## Скачать

**[Скачать PhantomInstaller.exe](https://github.com/Alexg7372/cscfg/releases/latest/download/PhantomInstaller.exe)**

Один автономный EXE для Counter-Strike 2. Никакие дополнительные папки, assets, DLL или отдельный `Phantom.cfg` рядом с установщиком не нужны.

## Что умеет

### 1. Phantom CFG

Внутри установщика уже находится мой готовый `Phantom.cfg`:

- `fps_max 120`
- `fps_max_ui 120`
- `cl_crosshair_recoil 0`
- `r_drawtracers_firstperson 1`
- `r_fullscreen_gamma 2.4`
- `viewmodel_fov 68`
- `viewmodel_offset_x 2.5`
- `viewmodel_offset_y 2`
- `viewmodel_offset_z -2`
- сохранены мой прицел, sensitivity и бинды

Кнопка **УСТАНОВИТЬ PHANTOM CFG** сама находит CS2, делает backup старого файла и ставит конфиг в `game\csgo\cfg`.

### 2. Свой CFG

Кнопка **УСТАНОВИТЬ СВОЙ CFG** открывает выбор файла. Можно выбрать любой `.cfg`; Phantom Installer скопирует его в правильную папку CS2, сохранив предыдущий файл с таким именем в backup.

После установки показывается готовая Steam-команда вида:

`+exec MyConfig.cfg`

Её можно скопировать одной кнопкой.

### 3. CFG Builder

Кнопка **СОЗДАТЬ CFG** читает актуальные настройки CS2 из Steam `userdata/<AccountID>/730/local/cfg`, собирает пользовательские convar-настройки и бинды в обычный `.cfg`, после чего открывает сохранение файла.

Имя и папку выбирает пользователь сам. Исходные настройки CS2 при этом не изменяются.

## Установка

1. Скачать `PhantomInstaller.exe` из Releases.
2. Запустить и подтвердить UAC.
3. Выбрать один из трёх режимов.
4. Для установленного CFG скопировать показанную команду в Steam → Counter-Strike 2 → Свойства → Основные → Параметры запуска.

## Сборка

Установщик — кастомное WPF-приложение в чёрно-белом стиле. `welcome.jpeg`, `installing.png`, `installed.png`, встроенный `Phantom.cfg` и self-contained .NET runtime упаковываются в один `PhantomInstaller.exe`.

GitHub Actions автоматически пересобирает EXE и обновляет asset в GitHub Release `v1.0.0`.
