using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Text))]
[ExecuteInEditMode]
public class HyphenationJpn : UIBehaviour
{
	// http://answers.unity3d.com/questions/424874/showing-a-textarea-field-for-a-string-variable-in.html
	[TextArea(3,10), SerializeField]
	private string text;

	private Text _Text{
		get{
			if( _text == null )
				_text = GetComponent<Text>();
			return _text;
		}
	}
	private Text _text;
	readonly HyphenationJpns.CharacterWidthCache cache = new HyphenationJpns.CharacterWidthCache();

	protected override void OnRectTransformDimensionsChange ()
	{
		base.OnRectTransformDimensionsChange();
		UpdateText(text);
	}

	protected override void OnValidate()
	{
		base.OnValidate();
		UpdateText(text);
	}

	void UpdateText(string str)
	{
		// override
		_Text.horizontalOverflow = HorizontalWrapMode.Overflow;

		// update Text
		_Text.text = HyphenationJpns.Core.GetFormattedText(_Text, str, cache);
	}
	
	public void SetText(string str)
	{
		text = str;
		UpdateText(text);
	}


	// helper
	public float textWidth{
		set{
			_Text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
		}
		get{
			return _Text.rectTransform.rect.width;
		}
	}
	public int fontSize
	{
		set{
			_Text.fontSize = value;
		}
		get{
			return _Text.fontSize;
		}
	}

}