using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Protocol0317
{
    public partial class ProtocolGame : MonoBehaviour
    {
        private void OnGUI()
        {
            if (tutorialVisible && !introActive)
            {
                float width = Mathf.Min(820f, Screen.width - 32f);
                float height = Mathf.Min(510f, Screen.height - 24f);
                GUILayout.BeginArea(new Rect((Screen.width-width)/2f, (Screen.height-height)/2f, width, height), GUI.skin.box);
                GUILayout.Space(14f);
                GUILayout.Label("ИНСТРУКТАЖ ОПЕРАТОРА  —  " + (tutorialPage + 1) + "/3", HeaderStyle());
                GUILayout.Space(18f);
                if (tutorialPage == 0)
                {
                    GUILayout.Label("Ваша задача — удержать комплекс до конца смены. Здесь две связанные системы управления:", LabelStyle());
                    GUILayout.Space(12f);
                    GUILayout.Label("ТЕРМИНАЛ (E): распределение ограниченной мощности между СВЕТОМ, ВЕНТИЛЯЦИЕЙ и ТЕРМИНАЛОМ. Для каждого события требуются свои настройки.", LabelStyle());
                    GUILayout.Space(12f);
                    GUILayout.Label("ЭЛЕКТРОЩИТ (R): отдельные выключатели четырёх помещений — РЕАКТОР, НАСОСЫ, ВЕНТИЛЯЦИЯ, ШЛЮЗ. Их состояние влияет на прочность помещений (HP).", LabelStyle());
                }
                else if (tutorialPage == 1)
                {
                    GUILayout.Label("КАК СИСТЕМЫ ВЛИЯЮТ ДРУГ НА ДРУГА", HeaderStyle());
                    GUILayout.Space(14f);
                    GUILayout.Label("НАСОСЫ → РЕАКТОР: если насосы обесточены или их HP ниже 35%, реактор теряет охлаждение и прочность.", LabelStyle());
                    GUILayout.Space(12f);
                    GUILayout.Label("РЕАКТОР → ВЕНТИЛЯЦИЯ И ШЛЮЗ: разрушенный реактор постепенно повреждает оба помещения.", LabelStyle());
                    GUILayout.Space(12f);
                    GUILayout.Label("ВЕНТИЛЯЦИЯ → НАСОСЫ: отключённая или разрушенная вентиляция постепенно повреждает насосы.", LabelStyle());
                    GUILayout.Space(12f);
                    GUILayout.Label("ВАЖНО: отключение питания замедляет прямой урон в аварийном помещении, но может вызвать цепную аварию в другом.", LabelStyle());
                }
                else
                {
                    GUILayout.Label("ПОРЯДОК ДЕЙСТВИЙ ПРИ АВАРИИ", HeaderStyle());
                    GUILayout.Space(14f);
                    GUILayout.Label("1. Нажмите «НАЧАТЬ СМЕНУ» на терминале.", LabelStyle());
                    GUILayout.Label("2. Нажмите «ЗАПУСТИТЬ ДИАГНОСТИКУ» — она занимает 5 секунд.", LabelStyle());
                    GUILayout.Label("3. Нажмите Q: проверьте камеры и HP помещений.", LabelStyle());
                    GUILayout.Label("4. Нажмите R: при необходимости переключите питание помещений, учитывая зависимости.", LabelStyle());
                    GUILayout.Label("5. Нажмите E: настройте мощность и подтвердите решение. В щитке 1–4 — ремонт, 0 — отмена.", LabelStyle());
                    GUILayout.Space(12f);
                    GUILayout.Label("Esc — назад. Удерживайте правую кнопку мыши для осмотра комнаты.", LabelStyle());
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(tutorialPage < 2 ? "ДАЛЕЕ  →" : "ПОНЯТНО — К СМЕНЕ", ButtonStyle(), GUILayout.Height(58f)))
                {
                    if (tutorialPage < 2) tutorialPage++;
                    else { tutorialVisible = false; SetTerminalFocus(true); }
                }
                GUILayout.Space(12f);
                GUILayout.EndArea();
                return;
            }
            if (lossChoiceVisible)
            {
                float width = Mathf.Min(650f, Screen.width - 32f);
                float height = 310f;
                GUILayout.BeginArea(new Rect((Screen.width-width)/2f, (Screen.height-height)/2f, width, height), GUI.skin.box);
                GUILayout.Space(14f);
                GUILayout.Label("ПРОТОКОЛ 03:17 — ВРЕМЕННАЯ ПЕТЛЯ", HeaderStyle());
                GUILayout.Space(18f);
                GUILayout.Label("Комплекс разрушен. Выберите, что делать дальше.", LabelStyle());
                GUILayout.Space(24f);
                if (GUILayout.Button("ОСТАТЬСЯ В КОМПЛЕКСЕ", ButtonStyle(), GUILayout.Height(65f))) RestartTimeLoop();
                GUILayout.Space(10f);
                if (GUILayout.Button("ЭВАКУИРОВАТЬСЯ", ButtonStyle(), GUILayout.Height(65f))) RestartTimeLoop();
                GUILayout.EndArea();
                return;
            }
            if (timeLoopMessageVisible)
            {
                float width = Mathf.Min(690f, Screen.width - 32f);
                float height = 270f;
                GUILayout.BeginArea(new Rect((Screen.width-width)/2f, (Screen.height-height)/2f, width, height), GUI.skin.box);
                GUILayout.Space(20f);
                GUILayout.Label("ПРОТОКОЛ 03:17 — ВРЕМЕННАЯ ПЕТЛЯ", HeaderStyle());
                GUILayout.Space(20f);
                GUILayout.Label("Ваш выбор ничего не изменил. Вы попали во временную петлю.\nВремя возвращается к началу смены — всё начинается сначала.", LabelStyle());
                GUILayout.Space(22f);
                if (GUILayout.Button("НАЧАТЬ СМЕНУ ЗАНОВО", ButtonStyle(), GUILayout.Height(65f))) CompleteTimeLoopRestart();
                GUILayout.EndArea();
                return;
            }
            if (!panelFocused || introActive || station == null || endingInProgress) return;
            Matrix4x4 oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1000f, Screen.height / 650f);
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1f));
            float w = 960f;
            float h = 600f;
            GUILayout.BeginArea(new Rect((Screen.width/scale-w)/2f, (Screen.height/scale-h)/2f, w, h), GUI.skin.box);
            GUILayout.Space(12f);
            GUILayout.Label("ЭЛЕКТРОЩИТ / РУЧНОЕ УПРАВЛЕНИЕ", HeaderStyle());
            GUILayout.Space(12f);
            string[] labels = {"РЕАКТОР", "НАСОСЫ", "ВЕНТИЛЯЦИЯ", "ШЛЮЗ"};
            for (int i=0;i<4;i++)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(labels[i] + "  HP " + Mathf.CeilToInt(station.Health(i)) + "%", LabelStyle(), GUILayout.Width(390f), GUILayout.Height(45f));
                int index = i;
                Color oldColor = GUI.color;
                GUI.color = station.IsPowered(i) ? new Color(.5f,1f,.6f) : new Color(1f,.55f,.55f);
                GUILayout.Label(station.IsPowered(i) ? "ВКЛ" : "ОТКЛ", LabelStyle(), GUILayout.Width(85f));
                GUI.color = oldColor;
                if (GUILayout.Button(station.IsPowered(i) ? "Выключить" : "Включить", ButtonStyle(), GUILayout.Width(180f), GUILayout.Height(48f)))
                { station.TogglePower(index); PlaySound(clickSound, .3f); }
                bool oldEnabled = GUI.enabled;
                GUI.enabled = started && station.Health(i) < 100f && station.RepairRoom != i;
                if (GUILayout.Button("Ремонт " + (i+1), ButtonStyle(), GUILayout.Width(180f), GUILayout.Height(48f))) station.SendRepair(index);
                GUI.enabled = oldEnabled;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(12f);
            string crew = station.RepairRoom < 0 ? "Свободна" : labels[station.RepairRoom] + (station.TravelLeft > 0f ? " — в пути " + Mathf.CeilToInt(station.TravelLeft) + " с" : " — ремонт +3%/с");
            GUILayout.Label("РЕМОНТНАЯ БРИГАДА (1): " + crew, HeaderStyle());
            GUILayout.Label("1–4 — отправить / перенаправить. 0 — отменить. Переход 5 с.", LabelStyle());
            GUILayout.Label("После ремонта разрушенного помещения включите питание вручную.", LabelStyle());
            if (GUILayout.Button("ОТОЗВАТЬ БРИГАДУ (0)", ButtonStyle(), GUILayout.Height(48f))) station.CancelRepair();
            if (GUILayout.Button("НАЗАД (R / ESC)", ButtonStyle(), GUILayout.Height(52f))) SetPanelFocus(false);
            GUILayout.EndArea();
            GUI.matrix = oldMatrix;
        }

        private GUIStyle HeaderStyle()
        {
            if (switchboardHeaderStyle == null) { switchboardHeaderStyle = new GUIStyle(GUI.skin.label); switchboardHeaderStyle.fontSize = 27; switchboardHeaderStyle.fontStyle = FontStyle.Bold; switchboardHeaderStyle.alignment = TextAnchor.MiddleCenter; switchboardHeaderStyle.wordWrap = true; switchboardHeaderStyle.normal.textColor = Color.white; }
            return switchboardHeaderStyle;
        }
        private GUIStyle LabelStyle()
        {
            if (switchboardLabelStyle == null) { switchboardLabelStyle = new GUIStyle(GUI.skin.label); switchboardLabelStyle.fontSize = 21; switchboardLabelStyle.alignment = TextAnchor.MiddleLeft; switchboardLabelStyle.wordWrap = true; switchboardLabelStyle.normal.textColor = Color.white; }
            return switchboardLabelStyle;
        }
        private GUIStyle ButtonStyle()
        {
            if (switchboardButtonStyle == null) { switchboardButtonStyle = new GUIStyle(GUI.skin.button); switchboardButtonStyle.fontSize = 22; switchboardButtonStyle.fontStyle = FontStyle.Bold; }
            return switchboardButtonStyle;
        }

        private void RestartTimeLoop()
        {
            // Both choices lead to the same explanation, then restart only on confirmation.
            lossChoiceVisible = false;
            timeLoopMessageVisible = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void CompleteTimeLoopRestart()
        {
            timeLoopMessageVisible = false;
            endingInProgress = false;
            StopAllCoroutines();
            if (cinematicPanel != null) cinematicPanel.SetActive(false);
            ResetGame();
            if (cctv != null) cctv.SetIncident(-1, false);
            introActive = true;
            introTime = 0f;
            sceneCamera.transform.SetPositionAndRotation(introStart, introStartRotation);
            screenRoot.SetActive(false);
            if (introSound != null && soundSource != null) soundSource.PlayOneShot(introSound, .45f);
            if (emergencyLight != null) StartCoroutine(IntroLighting());
        }

    }
}
