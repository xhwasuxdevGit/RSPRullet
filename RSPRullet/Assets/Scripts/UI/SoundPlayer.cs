using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// 씬의 배경음악과 효과음을 재생한다 (RSP-74). 타이틀 씬과 게임 씬에 하나씩 둔다.
// 재생할 소리와 음량은 SoundSettings에서 읽는다. 각 화면은 재생이 필요한 시점에 Play... 메서드를 부른다.
// 배경음악은 씬이 시작될 때 처음부터 반복 재생한다.
// 단, 게임 씬이 라운드 전환으로 다시 불릴 때는 KeepBgmPosition으로 남겨 둔 위치부터 이어서 재생한다.
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private SoundSettings _settings;

    // 배경음악 전용과 효과음 전용 AudioSource. 효과음이 겹쳐 울려도 배경음악은 끊기지 않는다
    [SerializeField] private AudioSource _bgmSource;
    [SerializeField] private AudioSource _sfxSource;

    // 누르면 클릭 소리를 내는 버튼. 씬에 새 버튼을 만들면 여기에 추가한다
    [SerializeField] private Button[] _clickButtons;

    // 다음 씬이 배경음악을 이어서 재생할 위치(초). static 값은 씬을 다시 불러와도 남고, 한 번 쓰면 0으로 돌아간다.
    // 이어서 재생하라고 따로 남기지 않으면 0이므로, 타이틀에서 게임 씬으로 들어갈 때는 처음부터 재생한다
    private static float _resumeBgmTime;

    private void Start()
    {
        _bgmSource.clip = _settings.BgmClip;
        _bgmSource.loop = true;
        _bgmSource.volume = _settings.BgmVolume;
        _bgmSource.time = _resumeBgmTime;
        _resumeBgmTime = 0f;
        _bgmSource.Play();

        foreach (Button button in _clickButtons) { button.onClick.AddListener(PlayClick); }
    }

    private void OnDestroy()
    {
        foreach (Button button in _clickButtons) { button.onClick.RemoveListener(PlayClick); }
    }

    // 씬을 다시 불러오기 전에 부른다. 다시 불린 씬이 지금 위치부터 배경음악을 이어서 재생한다
    public void KeepBgmPosition()
    {
        _resumeBgmTime = _bgmSource.time;
    }

    // 카드 분배·뒤집기·교환. 놓는 소리 4개 중 하나를 무작위로 낸다
    public void PlayCardPlace()
    {
        AudioClip[] clips = _settings.CardPlaceClips;
        PlaySfx(clips[Random.Range(0, clips.Length)], _settings.CardVolume);
    }

    public void PlayCardShuffle() { PlaySfx(_settings.CardShuffleClip, _settings.CardVolume); }

    // 조합 정산과 판정 공개
    public void PlayCashRegister() { PlaySfx(_settings.CashRegisterClip, _settings.CashRegisterVolume); }

    public void PlayClick() { PlaySfx(_settings.ClickClip); }

    public void PlayBanner() { PlaySfx(_settings.BannerClip); }

    public void PlayRoundClearStinger() { PlayStinger(_settings.RoundClearStingerClip); }

    public void PlayGameOverStinger() { PlayStinger(_settings.GameOverStingerClip); }

    // volumeScale은 효과음 음량에 곱할 값이다. 카드 소리처럼 따로 조절할 때만 1이 아닌 값을 준다
    private void PlaySfx(AudioClip clip, float volumeScale = 1f)
    {
        _sfxSource.PlayOneShot(clip, _settings.SfxVolume * volumeScale);
    }

    // 클리어·게임 오버 소리가 나는 동안은 배경음악을 끈다. 소리가 끝나면 배경음악 음량을 되돌린다
    private void PlayStinger(AudioClip clip)
    {
        _sfxSource.PlayOneShot(clip, _settings.StingerVolume);
        StartCoroutine(MuteBgmFor(clip.length));
    }

    private IEnumerator MuteBgmFor(float seconds)
    {
        _bgmSource.volume = 0f;
        yield return new WaitForSeconds(seconds);
        _bgmSource.volume = _settings.BgmVolume;
    }
}
