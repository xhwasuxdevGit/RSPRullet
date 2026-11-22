using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 테이블 가운데 카드 표시: 양쪽 카드 슬롯, 더미 위로 들어 올린 뽑은 카드, 두 슬롯 사이의 교환 표시
// (UI·UX 화면 흐름 세부 기획서 2-2장 카드 슬롯, 3-1장 판-02·판-03, 4장 카드 표시 규칙).
// 진행 흐름(GameFlow)의 단계·카드 변경 알림을 받을 때만 상태를 읽어 CardFaceRules로 표시를 정한다.
// 눈 표시는 단계에 따라 꺼지므로(CardFaceRules.ShowsKnownMarker) 카드 알림뿐 아니라 단계 알림도 받는다.
//
// 카드 연출 (연출·아트·사운드 세부 기획서 3-3~3-6장): 뽑기 들어 올리기, 분배 이동, 교환 곡선 이동, 카드 공개 2박자, 앞뒷면이 바뀌는 뒤집기.
// 알림을 받은 순간의 표시 값으로 연출을 만들어 대기열에 넣고, 코루틴 하나가 앞 연출이 끝나면 다음 연출을 재생한다.
// 진행 흐름은 알림을 한꺼번에 보내므로(예: 분배 → 카드 공개가 같은 순간), 받은 순간의 값을 담아 두어야 카드가 공개 전 모습으로 이동한 뒤 뒤집힌다.
// 연출 중에는 IsAnimating이 참이고, 단계별 버튼과 아이템 칸은 이 값으로 입력을 잠근다.
// 카드 공개 연출이 끝나면 진행 흐름에 Next()를 넣어 판정으로 넘긴다 (GameFlow.RevealMatch).
// 조합 칸 표시는 CombinationSlotsUI에서 맡는다
public class TableCardsUI : MonoBehaviour
{
    [SerializeField] private GameFlowRunner _runner;
    [SerializeField] private DisplayTimeSettings _displayTimeSettings;

    // 진행 안내 배너가 떠 있는 동안에는 다음 연출을 시작하지 않는다 (RSP-86)
    [SerializeField] private BannerUI _banner;

    // 카드 분배·뒤집기·교환 효과음 (RSP-74)
    [SerializeField] private SoundPlayer _sound;

    [SerializeField] private CardView _playerSlot;
    [SerializeField] private CardView _dealerSlot;
    [SerializeField] private CardView _drawnCard;
    [SerializeField] private GameObject _swapMarker;

    // 카드 더미 맨 위 낱장. 뽑은 카드와 분배되는 다음 카드가 이 자리에서 출발한다
    [SerializeField] private Transform _deckTop;

    [SerializeField] private Sprite _cardBack;
    [SerializeField] private Sprite _scissorsFront;
    [SerializeField] private Sprite _rockFront;
    [SerializeField] private Sprite _paperFront;

    private GameFlow _flow;
    private CardMotion _playerSlotMotion;
    private CardMotion _dealerSlotMotion;
    private CardMotion _drawnCardMotion;

    private readonly Queue<IEnumerator> _steps = new Queue<IEnumerator>();

    // 대기열의 연출을 모두 마쳤을 때 각 자리에 보일 카드와 그림. 새 알림이 오면 이 값과 비교해 다음 연출을 정한다
    private Card _plannedDrawn;
    private Card _plannedPlayer;
    private Card _plannedDealer;
    private Sprite _plannedDrawnSprite;
    private Sprite _plannedPlayerSprite;
    private Sprite _plannedDealerSprite;

    // 연출을 재생하는 중인지. 바뀔 때 AnimationStateChanged를 알린다
    public bool IsAnimating { get; private set; }

    public event Action AnimationStateChanged;

    private void Awake()
    {
        _flow = _runner.Flow;
        _playerSlotMotion = _playerSlot.GetComponent<CardMotion>();
        _dealerSlotMotion = _dealerSlot.GetComponent<CardMotion>();
        _drawnCardMotion = _drawnCard.GetComponent<CardMotion>();
        _flow.PhaseChanged += OnPhaseChanged;
        _flow.CardsChanged += OnCardsChanged;
    }

    private void OnDestroy()
    {
        _flow.PhaseChanged -= OnPhaseChanged;
        _flow.CardsChanged -= OnCardsChanged;
    }

    private void OnPhaseChanged(GamePhase previous, GamePhase next)
    {
        if (next == GamePhase.Distribute)
        {
            PlanLift();
        }
        else if (previous == GamePhase.Distribute)
        {
            PlanDeal();
        }
        else if (next == GamePhase.Reveal)
        {
            // 공개는 이 알림 뒤에 일어나므로, 실제로 재생할 때 카드 상태를 읽는다
            Enqueue(RevealMatchCards());
        }
        else
        {
            PlanSync();
        }
    }

    // 뽑기·분배·카드 공개 중에 오는 카드 알림은 곧 이어지는 단계 알림에서 연출로 함께 정하므로 건너뛴다
    private void OnCardsChanged()
    {
        GamePhase phase = _flow.CurrentPhase;
        if (phase == GamePhase.Draw || phase == GamePhase.Distribute || phase == GamePhase.Reveal) { return; }

        if (IsSwapped())
        {
            PlanSwap();
            return;
        }

        PlanSync();
    }

    // ── 연출 정하기: 알림을 받은 순간의 표시 값을 담아 대기열에 넣는다 ──

    // 뽑은 카드가 더미 위로 떠오르며 딜러 쪽 가장자리가 들린 경사로 기운다 (연출·아트·사운드 세부 기획서 3-3장, CardMotion.LiftFrom)
    private void PlanLift()
    {
        Card card = _flow.Table.DrawnCard;
        if (card == null)
        {
            PlanSync();
            return;
        }

        Sprite sprite = GetSprite(card);
        bool showsKnownMarker = CardFaceRules.ShowsKnownMarker(card, _flow.CurrentPhase);
        Enqueue(LiftDrawnCard(sprite, showsKnownMarker));
        _plannedDrawn = card;
        _plannedDrawnSprite = sprite;
    }

    // 뽑은 카드가 받는 쪽 슬롯으로, 더미의 다음 카드가 시차를 두고 남은 슬롯으로 간다 (연출·아트·사운드 세부 기획서 3-4장)
    private void PlanDeal()
    {
        GamePhase phase = _flow.CurrentPhase;
        Card playerCard = GetLastCard(Side.Player);
        Card dealerCard = GetLastCard(Side.Dealer);
        Sprite playerSprite = GetSprite(playerCard);
        Sprite dealerSprite = GetSprite(dealerCard);
        bool playerMarker = CardFaceRules.ShowsKnownMarker(playerCard, phase);
        bool dealerMarker = CardFaceRules.ShowsKnownMarker(dealerCard, phase);

        bool playerGetsDrawn = playerCard == _plannedDrawn;
        if (playerGetsDrawn)
        {
            Enqueue(DealCards(_playerSlot, _playerSlotMotion, playerSprite, playerMarker,
                _dealerSlot, _dealerSlotMotion, dealerSprite, dealerMarker));
        }
        else
        {
            Enqueue(DealCards(_dealerSlot, _dealerSlotMotion, dealerSprite, dealerMarker,
                _playerSlot, _playerSlotMotion, playerSprite, playerMarker));
        }

        _plannedDrawn = null;
        _plannedDrawnSprite = null;
        _plannedPlayer = playerCard;
        _plannedPlayerSprite = playerSprite;
        _plannedDealer = dealerCard;
        _plannedDealerSprite = dealerSprite;
        PlanSwapMarker(phase);
    }

    // 두 슬롯 카드가 서로 엇갈려 자리를 바꾸고, 도착한 뒤 앞뒷면이 바뀌는 카드만 뒤집는다 (연출·아트·사운드 세부 기획서 3-5장)
    private void PlanSwap()
    {
        GamePhase phase = _flow.CurrentPhase;
        Card playerCard = GetLastCard(Side.Player);
        Card dealerCard = GetLastCard(Side.Dealer);
        Sprite playerSprite = GetSprite(playerCard);
        Sprite dealerSprite = GetSprite(dealerCard);

        // 교환 전 각 카드의 그림은 상대 슬롯에 보이던 그림이다
        Enqueue(SwapCards(_plannedDealerSprite, playerSprite, CardFaceRules.ShowsKnownMarker(playerCard, phase),
            _plannedPlayerSprite, dealerSprite, CardFaceRules.ShowsKnownMarker(dealerCard, phase)));

        _plannedPlayer = playerCard;
        _plannedPlayerSprite = playerSprite;
        _plannedDealer = dealerCard;
        _plannedDealerSprite = dealerSprite;
        PlanSwapMarker(phase);
    }

    // 그 밖의 변화: 같은 카드의 앞뒷면이 바뀌면(돋보기 확인 등) 뒤집고, 나머지는 바로 맞춘다
    private void PlanSync()
    {
        GamePhase phase = _flow.CurrentPhase;
        bool hasSlotCards = CardFaceRules.HasSlotCards(phase);

        PlanCard(_drawnCard, _drawnCardMotion, _flow.Table.DrawnCard, phase, ref _plannedDrawn, ref _plannedDrawnSprite);
        PlanCard(_playerSlot, _playerSlotMotion, hasSlotCards ? GetLastCard(Side.Player) : null, phase,
            ref _plannedPlayer, ref _plannedPlayerSprite);
        PlanCard(_dealerSlot, _dealerSlotMotion, hasSlotCards ? GetLastCard(Side.Dealer) : null, phase,
            ref _plannedDealer, ref _plannedDealerSprite);
        PlanSwapMarker(phase);
    }

    // 카드 자리 하나를 card에 맞춘다. plannedCard·plannedSprite는 이 자리의 예정 값이고, 맞춘 뒤 새 값으로 바꿔 둔다
    private void PlanCard(CardView view, CardMotion motion, Card card, GamePhase phase, ref Card plannedCard, ref Sprite plannedSprite)
    {
        Sprite sprite = GetSprite(card);
        bool showsKnownMarker = card != null && CardFaceRules.ShowsKnownMarker(card, phase);

        if (card != null && card == plannedCard && sprite != plannedSprite)
        {
            Enqueue(FlipWithSound(motion, sprite, showsKnownMarker, _displayTimeSettings.CardFlipSeconds));
        }
        else if (IsAnimating)
        {
            Enqueue(ShowNow(view, sprite, showsKnownMarker));
        }
        else
        {
            Show(view, sprite, showsKnownMarker);
        }

        plannedCard = card;
        plannedSprite = sprite;
    }

    // 교환 표시는 교환 연출이 끝난 뒤에 켠다 (연출·아트·사운드 세부 기획서 3-5장)
    private void PlanSwapMarker(GamePhase phase)
    {
        // 교환 여부는 분배 직후와 지금 카드 위치를 비교한 값이다 (TableState.IsMatchCardSwapped)
        bool showsSwapMarker = CardFaceRules.ShowsSwapMarker(_flow.Table.IsMatchCardSwapped, phase);
        if (IsAnimating)
        {
            Enqueue(SetSwapMarker(showsSwapMarker));
            return;
        }

        _swapMarker.SetActive(showsSwapMarker);
    }

    // 두 슬롯의 카드가 예정 값과 서로 엇갈려 있으면 돋보기로 교환한 것이다
    private bool IsSwapped()
    {
        if (!CardFaceRules.HasSlotCards(_flow.CurrentPhase)) { return false; }

        Card playerCard = GetLastCard(Side.Player);
        Card dealerCard = GetLastCard(Side.Dealer);
        return playerCard != dealerCard && playerCard == _plannedDealer && dealerCard == _plannedPlayer;
    }

    // ── 대기열 재생 ──

    private void Enqueue(IEnumerator step)
    {
        _steps.Enqueue(step);
        if (IsAnimating) { return; }

        SetAnimating(true);
        StartCoroutine(PlaySteps());
    }

    private IEnumerator PlaySteps()
    {
        // 진행 흐름은 알림 여러 개를 같은 순간에 보낸다. 그 알림이 모두 끝난 다음 프레임에 재생을 시작한다
        yield return null;

        while (_steps.Count > 0)
        {
            // 배너는 같은 순간의 알림보다 먼저 켜지므로, 다음 프레임에는 이미 떠 있다. 예: 딜러 차례 판의 들어 올리기는 판 시작 배너가 꺼진 뒤에 한다
            while (_banner.IsShowing) { yield return null; }

            yield return StartCoroutine(_steps.Dequeue());
        }

        SetAnimating(false);
    }

    private void SetAnimating(bool isAnimating)
    {
        IsAnimating = isAnimating;
        AnimationStateChanged?.Invoke();
    }

    // ── 연출 ──

    // 더미의 카드는 뒷면이므로 뒷면으로 출발한다. 앞면으로 보일 카드(플레이어가 아는 카드)면 떠오르는 동안 뒤집는다
    private IEnumerator LiftDrawnCard(Sprite sprite, bool showsKnownMarker)
    {
        Sprite revealSprite = sprite == _cardBack ? null : sprite;
        _drawnCard.Show(_cardBack, showsKnownMarker);
        yield return StartCoroutine(_drawnCardMotion.LiftFrom(_deckTop.position, _displayTimeSettings.CardLiftTiltDegrees,
            revealSprite, showsKnownMarker, _displayTimeSettings.CardLiftSeconds));
    }

    private IEnumerator DealCards(CardView drawnReceiver, CardMotion drawnReceiverMotion, Sprite drawnSprite, bool drawnMarker,
        CardView deckReceiver, CardMotion deckReceiverMotion, Sprite deckSprite, bool deckMarker)
    {
        Vector3 drawnPosition = _drawnCard.transform.position;
        _drawnCard.Hide();

        // 뽑은 카드는 기다리지 않고 출발시키고, 더미의 다음 카드는 시차만큼 늦게 출발한다. 두 이동은 겹친다
        drawnReceiver.Show(drawnSprite, drawnMarker);
        _sound.PlayCardPlace();
        StartCoroutine(drawnReceiverMotion.MoveFrom(drawnPosition, _displayTimeSettings.CardMoveSeconds));
        yield return new WaitForSeconds(_displayTimeSettings.CardMoveDelaySeconds);

        // 늦게 출발한 카드가 마지막에 도착하므로 이 카드의 이동만 기다린다
        deckReceiver.Show(deckSprite, deckMarker);
        _sound.PlayCardPlace();
        yield return StartCoroutine(deckReceiverMotion.MoveFrom(_deckTop.position, _displayTimeSettings.CardMoveSeconds));
    }

    private IEnumerator SwapCards(Sprite playerBefore, Sprite playerAfter, bool playerMarker,
        Sprite dealerBefore, Sprite dealerAfter, bool dealerMarker)
    {
        // 이동 중에는 자리가 바뀌므로 출발 전 두 슬롯 위치를 먼저 담아 둔다
        Vector3 playerSlotPosition = _playerSlot.transform.position;
        Vector3 dealerSlotPosition = _dealerSlot.transform.position;
        float seconds = _displayTimeSettings.CardSwapSeconds;

        _playerSlot.Show(playerBefore, playerMarker);
        _dealerSlot.Show(dealerBefore, dealerMarker);
        _sound.PlayCardPlace();
        StartCoroutine(_playerSlotMotion.CurveFrom(dealerSlotPosition, seconds));
        yield return StartCoroutine(_dealerSlotMotion.CurveFrom(playerSlotPosition, seconds));

        float flipSeconds = _displayTimeSettings.CardFlipSeconds;
        if (playerAfter != playerBefore) { yield return StartCoroutine(FlipWithSound(_playerSlotMotion, playerAfter, playerMarker, flipSeconds)); }
        if (dealerAfter != dealerBefore) { yield return StartCoroutine(FlipWithSound(_dealerSlotMotion, dealerAfter, dealerMarker, flipSeconds)); }
    }

    // 카드 놓는 소리와 함께 뒤집는다
    private IEnumerator FlipWithSound(CardMotion motion, Sprite sprite, bool showsKnownMarker, float seconds)
    {
        _sound.PlayCardPlace();
        yield return StartCoroutine(motion.Flip(sprite, showsKnownMarker, seconds));
    }

    // 내 카드 → 딜러 카드 순서로 2박자에 공개한다. 이미 앞면인 카드는 그 박자에 뒤집는 대신 강조한다 (연출·아트·사운드 세부 기획서 3-6장)
    private IEnumerator RevealMatchCards()
    {
        yield return StartCoroutine(RevealBeat(_playerSlotMotion, GetLastCard(Side.Player), _plannedPlayerSprite));
        yield return StartCoroutine(RevealBeat(_dealerSlotMotion, GetLastCard(Side.Dealer), _plannedDealerSprite));

        _plannedPlayerSprite = GetSprite(GetLastCard(Side.Player));
        _plannedDealerSprite = GetSprite(GetLastCard(Side.Dealer));

        // 카드 공개 단계에서 기다리던 진행 흐름을 판정으로 넘긴다
        _flow.Next();
    }

    private IEnumerator RevealBeat(CardMotion motion, Card card, Sprite shownSprite)
    {
        Sprite sprite = GetSprite(card);
        float seconds = _displayTimeSettings.CardFlipSeconds;
        if (sprite != shownSprite)
        {
            yield return StartCoroutine(FlipWithSound(motion, sprite, CardFaceRules.ShowsKnownMarker(card, _flow.CurrentPhase), seconds));
            yield break;
        }

        yield return StartCoroutine(motion.Highlight(seconds));
    }

    private IEnumerator ShowNow(CardView view, Sprite sprite, bool showsKnownMarker)
    {
        Show(view, sprite, showsKnownMarker);
        yield break;
    }

    private IEnumerator SetSwapMarker(bool isShown)
    {
        _swapMarker.SetActive(isShown);
        yield break;
    }

    // ── 표시 값 ──

    private static void Show(CardView view, Sprite sprite, bool showsKnownMarker)
    {
        if (sprite == null)
        {
            view.Hide();
            return;
        }

        view.Show(sprite, showsKnownMarker);
    }

    // 카드가 없으면 null, 있으면 플레이어가 아는 카드는 앞면, 모르면 뒷면 그림
    private Sprite GetSprite(Card card)
    {
        if (card == null) { return null; }

        return CardFaceRules.ShowsFront(card) ? GetFrontSprite(card.Shape) : _cardBack;
    }

    // 카드 슬롯에 카드가 있는 동안 슬롯 카드는 카드 조합 칸의 마지막 카드다 (CardFaceRules.HasSlotCards)
    private Card GetLastCard(Side side)
    {
        IReadOnlyList<Card> slots = _flow.Table.GetCombinationSlots(side);
        return slots[slots.Count - 1];
    }

    // 카드 뒷면 그림. 카드 셔플 연출(CardShuffleUI)이 펼친 카드를 뒤집을 때 받아 쓴다
    public Sprite CardBackSprite => _cardBack;

    // 카드 앞면 그림은 이 컴포넌트에만 연결해 두고, 조합 칸(CombinationSlotsUI)과 카드 셔플 연출(CardShuffleUI)도 여기서 받아 쓴다
    public Sprite GetFrontSprite(HandShape shape)
    {
        switch (shape)
        {
            case HandShape.Scissors:
                return _scissorsFront;
            case HandShape.Rock:
                return _rockFront;
            default:
                return _paperFront;
        }
    }
}
