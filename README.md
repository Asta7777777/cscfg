# Phantom CS2 CFG

Персональный конфиг CS2 для AORUS 16X 9KG (i7-13650HX / RTX 4060 Laptop / 16 GB RAM).

Цель: стабильные **120 FPS**, плавная картинка, сохранённые sensitivity/прицел/бинды, включённые трассеры для контроля спрея и максимально убранный в правый нижний угол viewmodel.

## Быстрая установка

Скачай **только один файл**:

`release/PhantomInstaller.exe`

Никакие папки, картинки, DLL или отдельный `Phantom.cfg` рядом с EXE не нужны. `Phantom.cfg`, интерфейс и все изображения уже встроены внутрь установщика.

Запусти EXE и подтверди запрос администратора. Установщик:

- автоматически найдёт Steam и Counter-Strike 2;
- скопирует встроенный `Phantom.cfg` в папку `game\csgo\cfg`;
- перед перезаписью существующего `Phantom.cfg` сохранит резервную копию;
- после установки покажет экран **«ГОТОВО — Phantom cfg установлен»**;
- покажет путь установки;
- даст кнопку **«КОПИРОВАТЬ»** для команды запуска Steam:

`+exec Phantom.cfg`

После этого: Steam → ПКМ по Counter-Strike 2 → Свойства → Основные → Параметры запуска → вставить скопированную команду.

Кнопка **«ГОТОВО»** закрывает установщик.

## Ручная установка

Если нужна ручная установка, скачай `Phantom.cfg` и положи его сюда:

`D:\SteamLibrary\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg\Phantom.cfg`

В Steam Launch Options:

`+exec Phantom.cfg`

## Основное

- `fps_max 120`
- `fps_max_ui 120`
- `cl_crosshair_recoil 0`
- `r_drawtracers_firstperson 1`
- `r_fullscreen_gamma 2.4`
- `viewmodel_fov 68`
- `viewmodel_offset_x 2.5`
- `viewmodel_offset_y 2`
- `viewmodel_offset_z -2`

Для работы `r_fullscreen_gamma 2.4` лучше использовать режим **Fullscreen**.

## Installer

Установщик — отдельное кастомное WPF-приложение в чёрно-белом стиле, без стандартного MSI-мастера.

Изображения организованы в `assets/`:

- `assets/welcome.jpeg` — экран «Вас приветствует мастер установки» и иконка EXE;
- `assets/installing.png` — показывается во время установки;
- `assets/installed.png` — финальный экран после успешной установки.

Картинки не отображаются отдельными квадратными карточками: они растворяются в чёрном фоне через прозрачные градиенты. Между экранами есть fade-анимации, а изображение во время установки имеет мягкую анимацию.

Все эти ресурсы и `Phantom.cfg` встраиваются внутрь единственного `release/PhantomInstaller.exe` при сборке.

Исходники находятся в `installer/`. GitHub Actions автоматически пересобирает standalone EXE при изменении `Phantom.cfg`, `assets/` или исходников установщика.
