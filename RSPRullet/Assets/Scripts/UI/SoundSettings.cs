using UnityEngine;

// 효과음·배경음 클립과 음량 (RSP-74). 에셋은 하나만 만든다.
// 음량은 0~1이다. AudioSource는 1보다 크게 키울 수 없으므로, 소리가 작으면 음원 파일의 게인을 키워서 쓴다.
[CreateAssetMenu(fileName = "SoundSettings", menuName = "RSPRullet/Sound Settings")]
public class SoundSettings : ScriptableObject
{
    // 타이틀·게임 씬 배경음악. 반복 재생한다
    [SerializeField] private AudioClip _bgmClip;

    // 카드를 놓는 소리. 재생할 때마다 목록에서 무작위로 하나를 고른다 (카드 분배·뒤집기·교환)
    [SerializeField] private AudioClip[] _cardPlaceClips;

    // 더미를 섞는 소리
    [SerializeField] private AudioClip _cardShuffleClip;

    // 조합 정산·판정 공개 소리 (계산대)
    [SerializeField] private AudioClip _cashRegisterClip;

    // 버튼 클릭 소리
    [SerializeField] private AudioClip _clickClip;

    // 진행 안내 배너가 뜰 때 소리
    [SerializeField] private AudioClip _bannerClip;

    // 라운드 종료 화면이 다 어두워졌을 때 소리. 이긴 라운드는 클리어, 진 라운드는 게임 오버 소리를 낸다
    [SerializeField] private AudioClip _roundClearStingerClip;
    [SerializeField] private AudioClip _gameOverStingerClip;

    // 배경음악 음량. 효과음에 묻히지 않도록 낮게 둔다
    [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.5f;

    // 카드·정산·클릭·배너 효과음 음량
    [SerializeField, Range(0f, 1f)] private float _sfxVolume = 1f;

    // 카드 소리(놓기·섞기)에만 곱하는 음량. 효과음 음량에 곱해지므로 카드 소리만 더 낮추고 싶을 때 쓴다
    [SerializeField, Range(0f, 1f)] private float _cardVolume = 0.6f;

    // 계산대 소리(판 승패 판정·카드 정산)에만 곱하는 음량. 효과음 음량에 곱해진다
    [SerializeField, Range(0f, 1f)] private float _cashRegisterVolume = 1f;

    // 클리어·게임 오버 소리 음량
    [SerializeField, Range(0f, 1f)] private float _stingerVolume = 1f;

    public AudioClip BgmClip => _bgmClip;
    public AudioClip[] CardPlaceClips => _cardPlaceClips;
    public AudioClip CardShuffleClip => _cardShuffleClip;
    public AudioClip CashRegisterClip => _cashRegisterClip;
    public AudioClip ClickClip => _clickClip;
    public AudioClip BannerClip => _bannerClip;
    public AudioClip RoundClearStingerClip => _roundClearStingerClip;
    public AudioClip GameOverStingerClip => _gameOverStingerClip;
    public float BgmVolume => _bgmVolume;
    public float SfxVolume => _sfxVolume;
    public float CardVolume => _cardVolume;
    public float CashRegisterVolume => _cashRegisterVolume;
    public float StingerVolume => _stingerVolume;
}
