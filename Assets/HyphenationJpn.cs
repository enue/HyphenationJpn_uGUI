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

	private RectTransform _RectTransform{
		get{
			if( _rectTransform == null )
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
	private RectTransform _rectTransform;

	private Text _Text{
		get{
			if( _text == null )
				_text = GetComponent<Text>();
			return _text;
		}
	}
	private Text _text;

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
		float rectWidth = _RectTransform.rect.width;
		_Text.text = HyphenationJpns.Core.GetFormatedText(rectWidth, _Text, str);
	}
	
	public void SetText(string str)
	{
		text = str;
		UpdateText(text);
	}


	// helper
	public float textWidth{
		set{
			_RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value);
		}
		get{
			return _RectTransform.rect.width;
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