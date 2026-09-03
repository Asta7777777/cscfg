# Phantom CS2 CFG

Персональный конфиг CS2 для AORUS 16X 9KG (i7-13650HX / RTX 4060 Laptop / 16 GB RAM).

Цель: стабильные **120 FPS**, плавная картинка, сохранённые sensitivity/прицел/бинды, включённые трассеры для контроля спрея и максимально убранный в правый нижний угол viewmodel.

## Быстрая установка

Скачай:

`release/PhantomInstaller.exe`

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

Установщик сделан как отдельное кастомное WPF-приложение в чёрно-белом стиле, без стандартного MSI-мастера. `IMG_0240.jpeg` используется внутри интерфейса и автоматически конвертируется при сборке в иконку EXE.

Исходники находятся в `installer/`. GitHub Actions автоматически пересобирает `release/PhantomInstaller.exe`, если меняются `Phantom.cfg`, `IMG_0240.jpeg` или исходники установщика.
