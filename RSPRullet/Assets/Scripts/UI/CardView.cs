using UnityEngine;

// 테이블 위 카드 자리 하나(카드 슬롯, 더미 위로 들어 올린 뽑은 카드)의 카드 그림과 눈 아이콘·"상대가 알고 있음" 표시.
// 어떤 그림을 보여 줄지와 표시를 켤지는 TableCardsUI가 정해서 넘긴다
public class CardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _cardRenderer;

    // 카드 바깥 위 눈 아이콘과 카드 바깥 아래 문구를 묶은 오브젝트 (UI·UX 화면 흐름 세부 기획서 4-1장 기본 원칙)
    [SerializeField] private GameObject _knownMarker;

    // 카드 그림 폭의 절반(월드 단위). 뒤집을 때 테이블에 닿지 않도록 띄울 높이에 쓴다 (CardMotion.Flip)
    public float CardHalfWidth => _cardRenderer.sprite.bounds.extents.x * _cardRenderer.transform.lossyScale.x;

    public void Show(Sprite cardSprite, bool showsKnownMarker)
    {
        _cardRenderer.sprite = cardSprite;
        _cardRenderer.gameObject.SetActive(true);
        _knownMarker.SetActive(showsKnownMarker);
    }

    public void Hide()
    {
        _cardRenderer.gameObject.SetActive(false);
        _knownMarker.SetActive(false);
    }
}
