using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Arcanist
{
    public class OptionWindowController : MonoBehaviour
    {
        private const string FULLSCREEN_KEY = "Option.Fullscreen";
        private const string RESOLUTION_INDEX_KEY = "Option.ResolutionIndex";

        [Header("Panels")]
        [SerializeField] private GameObject screenPanel;
        [SerializeField] private GameObject soundPanel;
        [SerializeField] private GameObject controlPanel;

        [Header("Header Texts")]
        [SerializeField] private TMP_Text screenHeaderText;
        [SerializeField] private TMP_Text soundHeaderText;
        [SerializeField] private TMP_Text controlHeaderText;

        [Header("Screen")]
        [SerializeField] private TMP_Text fullscreenStateText;
        [SerializeField] private TMP_Text resolutionStateText;
        [SerializeField] private Vector2Int[] resolutions =
        {
            new Vector2Int(1280, 720),
            new Vector2Int(1600, 900),
            new Vector2Int(1920, 1080)
        };

        [Header("Sound")]
        [SerializeField] private Slider masterVolumeSlider;

        private int resolutionIndex;
        private bool isFullscreen;

        private void OnEnable()
        {
            LoadSettings();
            ShowScreenPanel();
            RefreshUI();
        }

        public void ShowScreenPanel()
        {
            ShowPanel(screenPanel);
            SetHeaderUnderline(screenHeaderText);
        }

        public void ShowSoundPanel()
        {
            ShowPanel(soundPanel);
            SetHeaderUnderline(soundHeaderText);
        }

        public void ShowControlPanel()
        {
            ShowPanel(controlPanel);
            SetHeaderUnderline(controlHeaderText);
        }

        private void ShowPanel(GameObject targetPanel)
        {
            screenPanel?.SetActive(screenPanel == targetPanel);
            soundPanel?.SetActive(soundPanel == targetPanel);
            controlPanel?.SetActive(controlPanel == targetPanel);
        }

        private void SetHeaderUnderline(TMP_Text selectedHeader)
        {
            SetUnderline(screenHeaderText, screenHeaderText == selectedHeader);
            SetUnderline(soundHeaderText, soundHeaderText == selectedHeader);
            SetUnderline(controlHeaderText, controlHeaderText == selectedHeader);
        }

        private void SetUnderline(TMP_Text headerText, bool isSelected)
        {
            if (headerText == null)
                return;

            if (isSelected)
                headerText.fontStyle |= FontStyles.Underline;
            else
                headerText.fontStyle &= ~FontStyles.Underline;
        }

        public void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            ApplyResolution();

            PlayerPrefs.SetInt(FULLSCREEN_KEY, isFullscreen ? 1 : 0);
            PlayerPrefs.Save();

            UpdateFullscreenText();
            UpdateResolutionText();
        }

        public void SelectNextResolution()
        {
            if (resolutions == null || resolutions.Length == 0)
                return;

            resolutionIndex = (resolutionIndex + 1) % resolutions.Length;
            ApplyResolution();
            SaveResolution();
            UpdateResolutionText();
        }

        public void SelectPreviousResolution()
        {
            if (resolutions == null || resolutions.Length == 0)
                return;

            resolutionIndex--;

            if (resolutionIndex < 0)
                resolutionIndex = resolutions.Length - 1;

            ApplyResolution();
            SaveResolution();
            UpdateResolutionText();
        }

        public void SetMasterVolume(float value)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }

        private void LoadSettings()
        {
            isFullscreen = PlayerPrefs.HasKey(FULLSCREEN_KEY)
                ? PlayerPrefs.GetInt(FULLSCREEN_KEY) == 1
                : Screen.fullScreen;

            int defaultResolutionIndex = GetDefaultResolutionIndex();
            int savedResolutionIndex = PlayerPrefs.HasKey(RESOLUTION_INDEX_KEY)
                ? PlayerPrefs.GetInt(RESOLUTION_INDEX_KEY)
                : defaultResolutionIndex;

            int maxIndex = resolutions is { Length: > 0 } ? resolutions.Length - 1 : 0;
            resolutionIndex = Mathf.Clamp(savedResolutionIndex, 0, maxIndex);
        }


        private void ApplyResolution()
        {
            if (isFullscreen)
            {
                Resolution currentResolution = Screen.currentResolution;
                Screen.SetResolution(
                    currentResolution.width,
                    currentResolution.height,
                    FullScreenMode.FullScreenWindow
                );
                return;
            }

            if (resolutions == null || resolutions.Length == 0)
                return;

            Vector2Int resolution = resolutions[resolutionIndex];
            Screen.SetResolution(
                resolution.x,
                resolution.y,
                FullScreenMode.Windowed
            );
        }

        private void RefreshUI()
        {
            UpdateFullscreenText();
            UpdateResolutionText();

            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
        }

        private void UpdateFullscreenText()
        {
            if (fullscreenStateText == null)
                return;

            fullscreenStateText.text = isFullscreen ? "켜짐" : "꺼짐";
        }

        private void UpdateResolutionText()
        {
            if (resolutionStateText == null)
                return;

            if (isFullscreen)
            {
                Resolution currentResolution = Screen.currentResolution;
                resolutionStateText.text = $"{currentResolution.width} x {currentResolution.height}";
                return;
            }

            if (resolutions == null || resolutions.Length == 0)
            {
                resolutionStateText.text = "없음";
                return;
            }

            Vector2Int resolution = resolutions[resolutionIndex];
            resolutionStateText.text = $"{resolution.x} x {resolution.y}";
        }

        private void SaveResolution()
        {
            PlayerPrefs.SetInt(RESOLUTION_INDEX_KEY, resolutionIndex);
            PlayerPrefs.Save();
        }

        private int GetDefaultResolutionIndex()
        {
            if (resolutions == null || resolutions.Length == 0)
                return 0;

            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].x == Screen.width && resolutions[i].y == Screen.height)
                    return i;
            }

            return resolutions.Length - 1;
        }
    }
}
