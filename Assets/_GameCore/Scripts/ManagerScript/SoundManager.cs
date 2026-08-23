using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioSource bgmAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip hitSFX;
    [SerializeField] private AudioClip backgroundMusic;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // เล่นเพลงประกอบฉากทันทีเมื่อเริ่มเกม (ถ้ามีการตั้งค่า AudioClip ไว้)
        if (backgroundMusic != null)
        {
            PlayBGM(backgroundMusic);
        }
    }

    // ฟังก์ชันกลางสำหรับเล่นเสียง SFX
    public void PlaySound(AudioClip clip)
    {
        if (clip != null && sfxAudioSource != null)
        {
            sfxAudioSource.PlayOneShot(clip);
        }
    }

    // ฟังก์ชันเรียกใช้เสียงตี/โดนโจมตี ตามที่ต้องการ
    public void PlayHitSound()
    {
        PlaySound(hitSFX);
    }

    // ฟังก์ชันเล่นเพลงประกอบ (Background Music)
    public void PlayBGM(AudioClip clip)
    {
        if (bgmAudioSource != null && clip != null)
        {
            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = true;
            bgmAudioSource.Play();
        }
    }

    // ฟังก์ชันควบคุมเสียง (Optional สำหรับทำหน้า Setting)
    public void SetMasterVolume(float volume)
    {
        AudioListener.volume = Mathf.Clamp01(volume);
    }
}