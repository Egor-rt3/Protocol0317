using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Protocol0317
{
    public partial class ProtocolGame : MonoBehaviour
    {
        private void SetCCTVFocus(bool focus)
        {
            cctvFocused = focus;
            panelFocused = false;
            terminalFocused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (screenRaycaster != null) screenRaycaster.enabled = false;
            if (cctv != null) cctv.SetInteractive(false);
            cameraFromPosition = sceneCamera.transform.position;
            cameraFromRotation = sceneCamera.transform.rotation;
            cameraFromFov = sceneCamera.fieldOfView;
            cameraBlend = 0f;
            cameraTransition = true;
            yaw = pitch = 0f;
            PlaySound(clickSound, .25f);
        }

        private void SetTerminalFocus(bool focus)
        {
            if (terminalFocused == focus && !cameraTransition) return;
            terminalFocused = focus;
            panelFocused = false;
            cctvFocused = false;
            if (cctv != null) cctv.SetInteractive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (screenRaycaster != null) screenRaycaster.enabled = false;
            cameraFromPosition = sceneCamera.transform.position;
            cameraFromRotation = sceneCamera.transform.rotation;
            cameraFromFov = sceneCamera.fieldOfView;
            cameraBlend = 0f;
            cameraTransition = true;
            yaw = 0f;
            pitch = 0f;
            PlaySound(clickSound,.25f);
        }

        private void SetPanelFocus(bool focus)
        {
            panelFocused = focus;
            terminalFocused = false;
            cctvFocused = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (screenRaycaster != null) screenRaycaster.enabled = false;
            if (cctv != null) cctv.SetInteractive(false);
            cameraFromPosition = sceneCamera.transform.position;
            cameraFromRotation = sceneCamera.transform.rotation;
            cameraFromFov = sceneCamera.fieldOfView;
            cameraBlend = 0f;
            cameraTransition = true;
            PlaySound(clickSound, .25f);
        }

    }
}
