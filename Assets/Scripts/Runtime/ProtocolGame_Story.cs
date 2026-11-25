using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Protocol0317
{
    public partial class ProtocolGame : MonoBehaviour
    {
        private void SetEmergency(bool active)
        {
            emergencyMode = active;
            emergencyClock = 0f;
            if (redBeacons != null)
                foreach (var light in redBeacons) if (light != null) light.intensity = active ? 2f : 0f;
            if (normalLight != null) normalLight.intensity = active ? .25f : 1.1f;
            if (sirenSource != null)
            {
                if (active && !sirenSource.isPlaying) sirenSource.Play();
                if (!active) sirenSource.Stop();
            }
        }

        private void LoadSounds()
        {
            soundSource = sceneCamera.gameObject.AddComponent<AudioSource>();
            soundSource.playOnAwake = false;
            soundSource.spatialBlend = 0f;
            clickSound = Resources.Load<AudioClip>("Audio/button");
            errorSound = Resources.Load<AudioClip>("Audio/error");
            successSound = Resources.Load<AudioClip>("Audio/success");
            alarmSound = Resources.Load<AudioClip>("Audio/alarm_custom");
            introSound = Resources.Load<AudioClip>("Audio/intro");
            sirenSource = sceneCamera.gameObject.AddComponent<AudioSource>();
            sirenSource.playOnAwake = false;
            sirenSource.loop = true;
            sirenSource.spatialBlend = 0f;
            sirenSource.volume = .085f;
            sirenSource.clip = alarmSound;
        }

        private void PlaySound(AudioClip clip, float volume = .6f)
        {
            if (clip != null && soundSource != null) soundSource.PlayOneShot(clip, volume);
        }

        private IEnumerator IntroLighting()
        {
            for (int i=0;i<20;i++)
            {
                if (emergencyLight != null) emergencyLight.intensity = (i%5==0) ? 0.12f : 0.7f;
                yield return new WaitForSeconds(.14f);
            }
            if (emergencyLight != null && !emergencyMode) emergencyLight.intensity = 0f;
        }

        private IEnumerator GeneratorFailureEffect()
        {
            yield return new WaitForSeconds(1.5f);
            feedback.text += " [ТРЕВОГА: мерцание освещения]";
            // Continuous low-volume siren is managed by SetEmergency().
            StartCoroutine(AlarmEffect(true));
        }

        private IEnumerator AlarmEffect(bool severe)
        {
            if (effectActive) yield break;
            effectActive = true;
            float baseIntensity = state.PowerLight > 0 ? 1.1f : .25f;
            for(int i=0;i<(severe?16:8);i++)
            {
                if(emergencyLight != null)
                {
                    emergencyLight.intensity = (i%2==0) ? .02f : (severe?2.8f:1.8f);
                    emergencyLight.color = (i%2==0) ? Color.red : new Color(1f,.42f,.18f);
                }
                yield return new WaitForSeconds(severe?.13f:.09f);
            }
            if(emergencyLight != null)
            {
                emergencyLight.intensity = emergencyMode ? 2f : 0f;
                emergencyLight.color = new Color(.7f,.25f,.22f);
            }
            effectActive = false;
        }

        private void StartDiagnostics()
        {
            if (!started || state.Finished || cinematicActive) { feedback.text = "Сначала начните смену."; return; }
            if (diagnosticsRunning || diagnosticsReady) return;
            diagnosticsRunning = true;
            diagnosticsTimer = diagnosticsDuration;
            feedback.text = "Диагностика запущена. Пока идёт сканирование, настройте питание.";
            PlaySound(clickSound, .4f);
        }

        private IEnumerator StoryBeat(int beat)
        {
            string[] titles = {
                "03:17 — НОЧНАЯ СМЕНА", "03:24 — ПОСТОРОННИЙ СИГНАЛ",
                "03:31 — СБОЙ ВНУТРЕННЕЙ СЕТИ", "03:39 — ПОСЛЕДНИЙ ПРОТОКОЛ"
            };
            string[] lines = {
                "Автоматический журнал: оператор принят. Системы работают нестабильно.\nНе открывайте аварийную дверь до завершения протокола.",
                "Камера у шлюза фиксирует движение. Дверь заблокирована.\nВосстановите связь и проверьте инженерные системы.",
                "Внимание: внешний канал не отвечает.\nРезервная мощность ограничена. Продолжайте ручное управление.",
                "Последняя серия проверок.\nСохраните целостность комплекса, иначе система запустит эвакуацию."
            };
            cinematicActive = true;
            if (screenRaycaster != null) screenRaycaster.enabled = false;
            if (cinematicPanel == null)
            {
                var overlay = new GameObject("StoryOverlay", typeof(Canvas), typeof(CanvasScaler));
                var c = overlay.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 90;
                cinematicPanel = overlay;
                CreatePanel(overlay.transform,Vector2.zero,Vector2.one,new Color(.005f,.015f,.025f,.93f));
                cinematicText = CreateText(overlay.transform,new Vector2(.15f,.32f),new Vector2(.85f,.68f),34,TextAnchor.MiddleCenter,FontStyle.Bold);
                cinematicText.color = new Color(.85f,.96f,.96f);
            }
            cinematicPanel.SetActive(true);
            cinematicText.text = titles[beat] + "\n\n" + lines[beat];
            PlaySound(introSound,.22f);
            yield return new WaitForSeconds(storyDurations[beat]);
            cinematicPanel.SetActive(false);
            cinematicActive = false;
            if (screenRaycaster != null) screenRaycaster.enabled = terminalFocused && !cameraTransition;
        }

        private IEnumerator EndingBeat(bool good)
        {
            endingInProgress = true;
            cinematicActive = true;
            if (screenRaycaster != null) screenRaycaster.enabled = false;
            if (cinematicPanel == null)
            {
                var overlay = new GameObject("EndingOverlay", typeof(Canvas), typeof(CanvasScaler));
                var c = overlay.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 90;
                cinematicPanel = overlay;
                CreatePanel(overlay.transform, Vector2.zero, Vector2.one, new Color(.005f,.015f,.025f,.95f));
                cinematicText = CreateText(overlay.transform, new Vector2(.1f,.25f), new Vector2(.9f,.75f), 34, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            cinematicPanel.SetActive(true);
            cinematicText.text = good ? "04:02 — СМЕНА ЗАВЕРШЕНА\n\nКомплекс сохранён. Вы покидаете станцию и возвращаетесь домой." :
                "04:02 — КОМПЛЕКС РАЗРУШЕН\n\nПротокол эвакуации запущен. Время остановилось...";
            yield return new WaitForSeconds(good ? 8f : 6f);
            cinematicPanel.SetActive(false);
            cinematicActive = false;
            endingInProgress = false;
            if (good)
            {
                // The successful ending is final. Do not restart the shift.
                Application.Quit();
#if UNITY_EDITOR
                Debug.Log("Protocol 03:17: successful ending — shift complete; standalone build will close.");
#endif
            }
            else
            {
                lossChoiceVisible = true;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void ResetGame()
        {
            state = new GameState(); started = false; unexpected = false; currentIndex = 0;
            lossChoiceVisible = false; timeLoopMessageVisible = false; endingInProgress = false; tutorialVisible = false; tutorialPage = 0;
            diagnosticsRunning = false; diagnosticsReady = false; failureStreak = 0;
            if (diagnosticButtonText != null) diagnosticButtonText.text = "ЗАПУСТИТЬ ДИАГНОСТИКУ";
            SetEmergency(false);
            if (station != null) station.ResetStation();
            panelFocused = false;
            if (cinematicPanel != null) cinematicPanel.SetActive(false);
            cinematicActive = false;
            headline.text = "ПРОТОКОЛ 03:17";
            narrative.text = "Вы оператор аварийной лаборатории. Распределяйте питание между тремя системами.\n\n" +
                "Свет: минимум 1. Вентиляция: минимум 2. Терминал: минимум 1.\n" +
                "Для выполнения операции нужны подходящие настройки. Каждая попытка меняет состояние комплекса.\n\n" +
                "E — терминал, Q — камеры, R — электрощит, Esc — назад.\n" +
                "Осмотр: удерживайте ПКМ и двигайте мышь. На мониторе нажимайте кнопки ЛКМ.\n" +
                "Начните смену, затем проверьте первое сообщение.";
            feedback.text = "Ожидание запуска смены";
            buttonText.text = "НАЧАТЬ СМЕНУ";
            UpdateUI();
        }

        private void BeginOrResolve()
        {
            if (state.Finished) return; // Endings are handled by the cinematic and choice screen.
            if (!started) { if (incidents.Length == 0) { feedback.text = "Нет корректной конфигурации аварий"; return; } if (station != null) station.BeginIncident(incidents[0].roomIndex); if (cctv != null) cctv.SetIncident(incidents[0].roomIndex, true); started = true; buttonText.text = "ПОДТВЕРДИТЬ РЕШЕНИЕ"; ShowIncident(); StartCoroutine(StoryBeat(0)); return; }
            if (diagnosticsRunning) { feedback.text = "Дождитесь завершения диагностики."; return; }
            if (!diagnosticsReady) { feedback.text = "Сначала запустите диагностику текущего события."; PlaySound(errorSound,.2f); return; }
            if (incidents.Length == 0) { feedback.text = "Нет конфигурации аварий"; return; }
            var incident = incidents[currentIndex];
            bool success = state.Meets(incident);
            if (station != null) station.ResolveIncident(success);
            state.Resolve(incident);
            failureStreak = success ? 0 : failureStreak + 1;
            PlaySound(success ? successSound : errorSound);
            SetEmergency(state.Integrity <= 35 || failureStreak >= 2);
            if (cctv != null) cctv.SetIncident(incident.roomIndex, !success);
            StartCoroutine(AlarmEffect(!success));
            feedback.text = success ? "Система восстановлена. +" + incident.reward + " очков." :
                "Решение не сработало. Целостность -" + incident.penalty + ".";
            if (state.Finished) { Finish(false); return; }
            if (!unexpected && state.Stage >= 2)
            {
                unexpected = true;
                StartCoroutine(GeneratorFailureEffect());
                state.PowerCapacity = 3;
                feedback.text += " ВНЕЗАПНО: резервный генератор вышел из строя. Доступно единиц питания: " + state.PowerCapacity + "!";
                // Avoid an impossible configuration after capacity shrinks.
                while (state.UsedPower > state.PowerCapacity)
                {
                    if (state.PowerLight > 0) state.PowerLight--;
                    else if (state.PowerTerminal > 0) state.PowerTerminal--;
                    else state.PowerVent--;
                }
            }
            if (state.Stage >= incidents.Length) { Finish(state.Integrity >= 50 && state.Solved >= Mathf.CeilToInt(incidents.Length * 0.75f)); return; }
            currentIndex = state.Stage;
            if (unexpected)
            {
                var next = incidents[currentIndex];
                int minimum = (next.requiresVent ? 2 : 0) + (next.requiresLight ? 1 : 0) + (next.requiresTerminal ? 1 : 0);
                state.PowerCapacity = Mathf.Max(3, Mathf.Max(minimum, next.requiredPower));
                while (state.UsedPower > state.PowerCapacity)
                {
                    if (state.PowerLight > 0) state.PowerLight--;
                    else if (state.PowerTerminal > 0) state.PowerTerminal--;
                    else state.PowerVent--;
                }
            }
            if (cctv != null) cctv.SetIncident(incidents[currentIndex].roomIndex, true);
            if (station != null) station.BeginIncident(incidents[currentIndex].roomIndex);
            ShowIncident(); UpdateUI();
            if (state.Stage == 3) StartCoroutine(StoryBeat(1));
            else if (state.Stage == 7) StartCoroutine(StoryBeat(2));
            else if (state.Stage == 10) StartCoroutine(StoryBeat(3));
        }

        private void Finish(bool good)
        {
            state.Finished = true; state.EndingGood = good;
            SetEmergency(false);
            StartCoroutine(EndingBeat(good));
            headline.text = good ? "КОМПЛЕКС СТАБИЛИЗИРОВАН" : "АВАРИЙНАЯ ЭВАКУАЦИЯ";
            narrative.text = good ? "Системы удалось сохранить. За стеклом больше никого нет. Диспетчерская переходит в безопасный режим." :
                "Ресурсов оказалось недостаточно. Автоматическая защита остановила комплекс. Оператор эвакуирован.";
            feedback.text = "Итог: " + state.Score + " очков; решено " + state.Solved + " из " + state.Stage + " событий.";
            buttonText.text = good ? "СМЕНА ЗАВЕРШЕНА" : "ОЖИДАНИЕ РЕШЕНИЯ"; UpdateUI();
        }

        private void ShowIncident()
        {
            if (incidents.Length == 0) { headline.text = "ОШИБКА КОНФИГУРАЦИИ"; narrative.text = "Проверьте Assets/Resources/incidents.json"; return; }
            diagnosticsRunning = false; diagnosticsReady = false;
            if (diagnosticButtonText != null) diagnosticButtonText.text = "ЗАПУСТИТЬ ДИАГНОСТИКУ";
            var i = incidents[currentIndex];
            if (objectiveText != null) objectiveText.text = "ЗАДАНИЕ " + (state.Stage+1) + "/" + incidents.Length + ": ДИАГНОСТИКА → ПИТАНИЕ → РЕШЕНИЕ";
            headline.text = "СИГНАЛ " + (state.Stage + 1) + ": " + i.title.ToUpperInvariant();
            narrative.text = i.description + "\n\nТребования: суммарная мощность не меньше " + i.requiredPower +
                "; вентиляция " + (i.requiresVent ? "2+" : "любая") +
                "; терминал " + (i.requiresTerminal ? "1+" : "любой") +
                "; свет " + (i.requiresLight ? "1+" : "любой") + ".\n\n" +
                "Настройте питание кнопками + и − и подтвердите решение.";
        }

        private void ChangePower(int system, int delta)
        {
            if (!started || state.Finished) { feedback.text = "Сначала начните смену"; return; }
            int current = system == 0 ? state.PowerLight : system == 1 ? state.PowerVent : state.PowerTerminal;
            if (!state.TrySetPower(system, current + delta))
            {
                feedback.text = "Нельзя изменить питание: проверьте общий лимит и диапазон 0–3.";
                PlaySound(errorSound, .35f);
            }
            else
            {
                feedback.text = "Распределение питания обновлено.";
                PlaySound(clickSound, .35f);
            }
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (state == null) return;
            string[] names = { "СВЕТ", "ВЕНТИЛЯЦИЯ", "ТЕРМИНАЛ" };
            int[] values = { state.PowerLight, state.PowerVent, state.PowerTerminal };
            for (int i = 0; i < 3; i++) powerLabels[i].text = names[i] + ": " + values[i];
            readout.text = "ЭНЕРГИЯ: " + state.UsedPower + "/" + state.PowerCapacity +
                "     ЦЕЛОСТНОСТЬ: " + state.Integrity + "%     ОЧКИ: " + state.Score;
            if (room != null)
            {
                var light = room.GetComponentInChildren<Light>();
                if (light != null && !effectActive && !emergencyMode) light.intensity = state.PowerLight > 0 ? 1.1f : .25f;
            }
        }

    }
}
