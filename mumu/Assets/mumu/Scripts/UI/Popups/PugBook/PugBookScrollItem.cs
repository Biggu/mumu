using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using mumu;

public class PugBookScrollItem : MonoBehaviour
{
    private static readonly Color DisabledColor = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color SilhouetteColor = new Color(0f, 0f, 0f, 0.5f);
    private static readonly Color[] TierBackColors =
    {
        new Color(1f, 1f, 1f, 0f),
        new Color(0.7f, 0.9f, 1f, 0.45f),
        new Color(0.85f, 0.7f, 1f, 0.45f),
        new Color(1f, 0.85f, 0.35f, 0.6f),
        new Color(1f, 0.6f, 0.5f, 0.6f),
        new Color(1f, 0.55f, 0.85f, 0.7f),
    };

    [SerializeField]
    private GameObject newIcon;
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI equippedText;
    [SerializeField]
    private TextMeshProUGUI ownedText;
    [SerializeField]
    private TextMeshProUGUI priceText;
    [SerializeField]
    private Image characterImage;
    [SerializeField]
    private MumuButton button;
    [SerializeField]
    private MumuButton plateButton;
    [SerializeField]
    private Image[] stars;

    private CharacterData character = null;
    private Image tierImage = null;

    public void Setup(CharacterData _character, Action<int> _onPriceClick, Action<int> _onPlateClick)
    {
        character = _character;
        Transform tier = transform.Find("tier");
        tierImage = tier != null ? tier.GetComponent<Image>() : null;
        Image plateImage = GetComponent<Image>();
    
        plateButton.targetGraphic = plateImage;
        plateButton.transition = Selectable.Transition.None;
        if (stars == null || stars.Length == 0)
        {
            stars = findStars();
        }

        tierImage.color = TierBackColors[Mathf.Clamp(character.Tier, CharacterMaster.MinTier, CharacterMaster.MaxTier) - 1];
        characterImage.sprite = PugIcons.Get(character.Id);
        nameText.text = character.Name;
        priceText.text = character.Price.ToString();

        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].gameObject.SetActive(i < character.Tier);
        }

        button.SetOnClick(() => _onPriceClick?.Invoke(character.Id));
        plateButton.SetOnClick(() => _onPlateClick?.Invoke(character.Id));
        Refresh(SaveDataManager.Data.bones, PugCollection.EquippedId);
    }

    public void Refresh(int _bones, int _equippedId)
    {
        if (character == null)
        {
            return;
        }

        int count = PugCollection.Count(character.Id);
        bool isHidden = character.IsSecret && count == 0;
        bool isEquipped = count > 0 && character.Id == _equippedId;
        newIcon.SetActive(count == 0 && !character.IsSecret);
        equippedText.text = MumuText.PUG_EQUIPPED();
        equippedText.gameObject.SetActive(isEquipped);
        ownedText.text = character.IsForSale
            ? MumuText.PUG_OWNED()
            : MumuText.PUG_NOT_FOR_SALE();
        ownedText.gameObject.SetActive(!isEquipped && (count > 0 || !character.IsForSale));
        characterImage.color = isHidden ? SilhouetteColor : Color.white;
        nameText.text = isHidden ? MumuText.UNKNOWN() : character.Name;

        button.gameObject.SetActive(character.IsForSale && count == 0);
        button.image.color = _bones >= character.Price ? Color.white : DisabledColor;
        plateButton.enabled = count > 0;
        plateButton.targetGraphic.raycastTarget = count > 0;
        if (!isHidden && character.Tier >= mumu.PugAura.MinTier)
        {
            PugAura.Attach(characterImage.rectTransform, character.Tier, 120f);
        }
    }

    private Image[] findStars()
    {
        var found = new List<Image>();
        foreach (Image image in GetComponentsInChildren<Image>(true))
        {
            if (image.name == "star" || image.name.StartsWith("star (", StringComparison.Ordinal))
            {
                found.Add(image);
            }
        }
        return found.ToArray();
    }
}
