# НЕ ОТКРЫВАЙ: Протокол 03:17

Версия Unity: 6000.6.8f1.

## Запуск
1. Откройте проект через Unity Hub.
2. Дождитесь импорта.
3. Выберите Protocol 03:17 → Create or Rebuild Main Scene.
4. Откройте `Assets/Scenes/Main.unity` и нажмите Play.
5. Для EXE используйте File → Build Profiles → Windows → Build.

## Файлы
- `ProtocolGame.cs` — запуск и обновление игры.
- `ProtocolGame_Camera.cs` — переключение камеры и обзор.
- `ProtocolGame_Panel.cs` — панель управления и экраны.
- `ProtocolGame_Story.cs` — аварии, сюжет, завершение.
- `ProtocolGame_Room.cs` — интерфейс и помещение.
- `GameState.cs` — состояние игры.
- `StationSystems.cs` — системы станции.
- `CCTVSystem.cs` — видеонаблюдение.
- `IncidentData.cs` — чтение и проверка JSON.

## Данные НТО
`Assets/Resources/incidents.json` — активная конфигурация.
`NTO_Demonstration` — примеры до/после и ошибочные данные.
