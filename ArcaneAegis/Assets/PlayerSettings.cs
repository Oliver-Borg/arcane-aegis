using UnityEngine;
using UnityEngine.UI;

public class PlayerSettings : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] ThirdPersonOrbitCamBasic thirdPersonOrbitCamBasic;
    [SerializeField] private float maxSensitivity = 100f;

    void Start()
    {
        sensitivitySlider.value = PlayerPrefs.GetFloat("sensitivity", 1f);
        volumeSlider.value = PlayerPrefs.GetFloat("volume", 1f);
        SetSensitivity(sensitivitySlider.value);
        SetVolume(volumeSlider.value);
    }

    public void SetSensitivity(float sensitivity) {
        PlayerPrefs.SetFloat("sensitivity", sensitivity);
        thirdPersonOrbitCamBasic.horizontalAimingSpeed = sensitivity * maxSensitivity + 10f;
        thirdPersonOrbitCamBasic.verticalAimingSpeed = sensitivity * maxSensitivity + 10f;
    }

    public void SetVolume(float volume) {
        PlayerPrefs.SetFloat("volume", volume);
        AudioListener.volume = volume;
    }

    public void ExitGame() {
        Application.Quit();
    }
}
