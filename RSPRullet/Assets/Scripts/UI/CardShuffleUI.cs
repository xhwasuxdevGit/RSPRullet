using System.Collections;
using UnityEngine;

// 카드 셔플 연출 (연출·아트·사운드 세부 기획서 3-1장 카드 셔플 연출, 7-1장 연출 시간).
// 카드풀 공개: 이번 카드풀 7장을 펼침 자리에 앞면으로 늘어놓고 더미 낱장은 숨긴다. 공개를 끝내는 시점은 GameFlowRunner가 정한다.
// 시작 카드를 나눠 받으면(카드 변경 알림): 펼친 카드를 뒷면으로 뒤집기 → 더미로 모으기 → 더미 섞기 →
// 더미 맨 위 카드가 앞면으로 양쪽 조합 칸 첫 칸으로 이동 → 진행 흐름에 Next()를 넣어 선 정하기로 넘긴다.
// 더미 낱장은 남은 장수와 관계없이 7장 그대로 두는 장식이라, 연출 동안만 위치와 그림을 바꿨다가 제자리·뒷면으로 돌려놓는다.
// 도착한 시작 카드는 선 정하기 단계부터 조합 칸(CombinationSlotsUI)이 보여 준다 (CombinationSlotRules.GetShownCount).
// 셔플 단계에는 켤 단계별 버튼이 없어 연출 중 입력을 따로 잠그지 않는다 (PhaseButtonRules)
public class CardShuffleUI : MonoBehaviour
{
    // 펼침 자리를 채우는 손 모양 순서. 카드풀 정보의 손 모양 칸과 같은 가위·바위·보 순서다
    private static readonly HandShape[] PoolShapeOrder = { HandShape.Scissors, HandShape.Rock, HandShape.Paper };

    // 더미 섞기 동안 낱장이 좌우로 벌어졌다 모이는 횟수
    private const int ShuffleSwingCount = 2;

    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

    // 카드 앞면·뒷면 그림을 가진 테이블 카드 표시. 그림 연결을 한 곳에만 둔다
    [SerializeField] private TableCardsUI _tableCards;

    // 라운드·셔플 시작 배너가 떠 있으면 카드풀 공개를 배너가 꺼진 뒤로 미룬다 (RSP-86)
    [SerializeField] private BannerUI _banner;

    // 카드 뒤집기·섞기·시작 카드 이동 효과음 (RSP-74)
    [SerializeField] private SoundPlayer _sound;

    // 카드풀 공개 때 7장을 늘어놓는 자리 (PFB_TableCard)
    [SerializeField] private CardView[] _spreadCards;

    // 카드 더미 낱장. 아래부터 위 순서로 넣어, 마지막이 맨 위 카드다
    [SerializeField] private SpriteRenderer[] _deckCards;

    // 시작 카드가 도착할 플레이어·딜러 조합 칸의 첫 칸
    [SerializeField] private Transform _playerStartSlot;
    [SerializeField] private Transform _dealerStartSlot;

    // 더미 섞기에서 낱장이 좌우로 벌어지는 폭(월드 단위)
    [SerializeField, Min(0f)] private float _shuffleSpread = 0.08f;

    private GameFlow _flow;
    private CardMotion[] _spreadMotions;
    private Vector3[] _deckHomeLocalPositions;
    private bool _isPlaying;

    // 배너가 꺼지면 카드풀을 공개해야 하는지
    private bool _isCardPoolPending;

    private void Awake()
    {
        _flow = _runner.Flow;

        _spreadMotions = new CardMotion[_spreadCards.Length];
        for (int i = 0; i < _spreadCards.Length; i++)
        {
            _spreadMotions[i] = _spreadCards[i].GetComponent<CardMotion>();
        }

        _deckHomeLocalPositions = new Vector3[_deckCards.Length];
        for (int i = 0; i < _deckCards.Length; i++)
        {
            _deckHomeLocalPositions[i] = _deckCards[i].transform.localPosition;
        }

        _flow.PhaseChanged += OnPhaseChanged;
        _flow.CardsChanged += OnCardsChanged;
        _banner.ShowingChanged += OnBannerShowingChanged;
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _flow.CardsChanged -= OnCardsChanged;
        _banner.ShowingChanged -= OnBannerShowingChanged;
    }

    // 카드 셔플 시작과 선 정하기가 비겨 다시 섞을 때 모두 카드풀 공개로 들어온다
    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        if (!_flow.IsShowingCardPool) { return; }

        if (_banner.IsShowing)
        {
            _isCardPoolPending = true;
            return;
        }

        ShowCardPool();
    }

    // 카드풀 공개 표시 시간도 배너가 꺼진 뒤부터 세므로(GameFlowRunner), 공개 중인 상태 그대로다
    private void OnBannerShowingChanged()
    {
        if (_banner.IsShowing || !_isCardPoolPending) { return; }

        _isCardPoolPending = false;
        if (_flow.IsShowingCardPool) { ShowCardPool(); }
    }

    // 카드풀 공개가 끝나 시작 카드를 나눠 받으면 연출을 시작한다
    private void OnCardsChanged()
    {
        if (_isPlaying || _flow.CurrentPhase != GamePhase.StartCardDeal || _flow.IsShowingCardPool) { return; }

        StartCoroutine(PlayShuffle());
    }

    private void ShowCardPool()
    {
        int index = 0;
        foreach (HandShape shape in PoolShapeOrder)
        {
            for (int i = 0; i < _flow.Table.GetPoolCardCount(shape); i++)
            {
                _spreadCards[index].Show(_tableCards.GetFrontSprite(shape), false);
                index++;
            }
        }

        SetDeckShown(false);
    }

    private IEnumerator PlayShuffle()
    {
        _isPlaying = true;

        // 7장을 한꺼번에 뒤집는다. 모두 같은 시간이라 마지막 카드의 뒤집기만 기다린다
        Sprite cardBack = _tableCards.CardBackSprite;
        float flipSeconds = _displayTimeSettings.CardFlipSeconds;
        int lastIndex = _spreadMotions.Length - 1;
        _sound.PlayCardPlace();
        for (int i = 0; i < lastIndex; i++)
        {
            StartCoroutine(_spreadMotions[i].Flip(cardBack, false, flipSeconds));
        }
        yield return StartCoroutine(_spreadMotions[lastIndex].Flip(cardBack, false, flipSeconds));

        yield return StartCoroutine(GatherDeck());
        yield return StartCoroutine(ShuffleDeck());
        yield return StartCoroutine(MoveStartCards());

        _isPlaying = false;

        // 시작 카드 배분 단계에서 기다리던 진행 흐름을 선 정하기로 넘긴다
        _flow.Next();
    }

    // 펼친 카드를 숨기고, 그 자리에서 더미 낱장이 출발해 더미로 모인다
    private IEnumerator GatherDeck()
    {
        var fromPositions = new Vector3[_deckCards.Length];
        for (int i = 0; i < _deckCards.Length; i++)
        {
            fromPositions[i] = _spreadCards[i].transform.position;
            _spreadCards[i].Hide();
        }

        SetDeckShown(true);

        float seconds = _displayTimeSettings.CardMoveSeconds;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            for (int i = 0; i < _deckCards.Length; i++)
            {
                _deckCards[i].transform.position = Vector3.Lerp(fromPositions[i], GetDeckHomePosition(i), t);
            }
            yield return null;
        }

        ResetDeckPositions();
    }

    // 짝수 번째와 홀수 번째 낱장이 서로 반대쪽으로 벌어졌다 모이기를 반복해, 두 묶음을 엇갈려 섞는 모양을 낸다
    private IEnumerator ShuffleDeck()
    {
        float seconds = _displayTimeSettings.DeckShuffleSeconds;
        _sound.PlayCardShuffle();
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            // 0 → 1 → 0을 ShuffleSwingCount번 반복하는 값
            float swing = Mathf.Abs(Mathf.Sin(Mathf.PI * ShuffleSwingCount * elapsed / seconds));
            for (int i = 0; i < _deckCards.Length; i++)
            {
                float direction = i % 2 == 0 ? 1f : -1f;
                _deckCards[i].transform.position = GetDeckHomePosition(i) + Vector3.right * (_shuffleSpread * swing * direction);
            }
            yield return null;
        }

        ResetDeckPositions();
    }

    // 더미 맨 위 카드는 플레이어 시작 카드, 그 아래 카드는 딜러 시작 카드로 앞면을 보이며 조합 칸 첫 칸으로 간다.
    // 분배 이동처럼 두 번째 카드는 시차를 두고 출발한다 (연출·아트·사운드 세부 기획서 3-4장)
    private IEnumerator MoveStartCards()
    {
        SpriteRenderer playerCard = _deckCards[_deckCards.Length - 1];
        SpriteRenderer dealerCard = _deckCards[_deckCards.Length - 2];
        Sprite playerBack = playerCard.sprite;
        Sprite dealerBack = dealerCard.sprite;
        float seconds = _displayTimeSettings.CardMoveSeconds;

        playerCard.sprite = _tableCards.GetFrontSprite(_flow.Table.GetCombinationSlots(Side.Player)[0].Shape);
        _sound.PlayCardPlace();
        StartCoroutine(MoveCard(playerCard.transform, _playerStartSlot.position, seconds));
        yield return new WaitForSeconds(_displayTimeSettings.CardMoveDelaySeconds);

        dealerCard.sprite = _tableCards.GetFrontSprite(_flow.Table.GetCombinationSlots(Side.Dealer)[0].Shape);
        _sound.PlayCardPlace();
        yield return StartCoroutine(MoveCard(dealerCard.transform, _dealerStartSlot.position, seconds));

        // 도착한 시작 카드는 이어서 조합 칸이 보여 주므로, 낱장은 제자리·뒷면으로 돌려놓는다
        playerCard.sprite = playerBack;
        dealerCard.sprite = dealerBack;
        ResetDeckPositions();
    }

    private static IEnumerator MoveCard(Transform card, Vector3 toPosition, float seconds)
    {
        Vector3 fromPosition = card.position;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.deltaTime)
        {
            card.position = Vector3.Lerp(fromPosition, toPosition, Mathf.SmoothStep(0f, 1f, elapsed / seconds));
            yield return null;
        }

        card.position = toPosition;
    }

    private Vector3 GetDeckHomePosition(int index)
    {
        return _deckCards[index].transform.parent.TransformPoint(_deckHomeLocalPositions[index]);
    }

    private void ResetDeckPositions()
    {
        for (int i = 0; i < _deckCards.Length; i++)
        {
            _deckCards[i].transform.localPosition = _deckHomeLocalPositions[i];
        }
    }

    private void SetDeckShown(bool isShown)
    {
        foreach (SpriteRenderer card in _deckCards)
        {
            card.gameObject.SetActive(isShown);
        }
    }
}
