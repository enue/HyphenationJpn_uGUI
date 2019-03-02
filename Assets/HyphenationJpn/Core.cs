using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace HyphenationJpns
{
	public static class Core
	{
		struct Word
		{
			public string Text { get; }
			public char Character { get; }

			public Word(string text, char character)
			{
				Text = text;
				Character = character;
			}

			public bool IsNewLine
			{
				get
				{
					return Character == '\n' || Character == '\r';
				}
			}
		}

		static float GetTextWidth(Font font, int fontSize, FontStyle fontStyle, string message, bool supportRichText)
		{
			if (supportRichText)
			{
				message = RITCH_TEXT_REPLACE.Replace(message, string.Empty);
			}
			float lineWidth = 0f;
			float maxWidth = 0f;
			foreach (var character in message)
			{
				if (character == '\r' || character == '\n')
				{
					maxWidth = Mathf.Max(lineWidth, maxWidth);
					lineWidth = 0f;
				}
				else
				{
					font.GetCharacterInfo(character, out CharacterInfo info, fontSize, fontStyle);
					lineWidth += info.advance;
				}
			}
			return Mathf.Max(maxWidth, lineWidth);
		}

		static float GetCharacterWidth(Font font, int fontSize, FontStyle fontStyle, char character)
		{
			font.GetCharacterInfo(character, out CharacterInfo info, fontSize, fontStyle);
			return info.advance;
		}

		public static string GetFormatedText(Text text, string msg)
		{
			return GetFormatedText(text.rectTransform.rect.width, text.font, text.fontSize, text.fontStyle, msg, text.supportRichText);
		}

		public static string GetFormatedText(float rectWidth, Font font, int fontSize, FontStyle fontStyle, string msg, bool supportRichText)
		{
			if (string.IsNullOrEmpty(msg))
			{
				return string.Empty;
			}

			font.RequestCharactersInTexture(msg, fontSize, fontStyle);

			// work
			StringBuilder lineBuilder = new StringBuilder();

			float lineWidth = 0;
			foreach (var originalLine in GetWordList(msg))
			{
				if (originalLine.IsNewLine)
				{
					lineWidth = 0;
				}
				else
				{
					float textWidth;
					if (originalLine.Text == null)
					{
						textWidth = GetCharacterWidth(font, fontSize, fontStyle, originalLine.Character);
					}
					else
					{
						textWidth = GetTextWidth(font, fontSize, fontStyle, originalLine.Text, supportRichText);
					}
					lineWidth += textWidth;
					if (lineWidth > rectWidth)
					{
						lineBuilder.Append(Environment.NewLine);
						lineWidth = textWidth;
					}
				}
				if (originalLine.Text == null)
				{
					lineBuilder.Append(originalLine.Character);
				}
				else
				{
					lineBuilder.Append(originalLine.Text);
				}
			}

			return lineBuilder.ToString();
		}

		static private IEnumerable<Word> GetWordList(string tmpText)
		{
			StringBuilder line = new StringBuilder();
			char emptyChar = new char();

			for (int characterCount = 0; characterCount < tmpText.Length; characterCount++)
			{
				char currentCharacter = tmpText[characterCount];
				char nextCharacter = (characterCount < tmpText.Length - 1) ? tmpText[characterCount + 1] : emptyChar;
				char preCharacter = (characterCount > 0) ? preCharacter = tmpText[characterCount - 1] : emptyChar;

				line.Append(currentCharacter);

				if (((IsLatin(currentCharacter) && IsLatin(preCharacter)) && (IsLatin(currentCharacter) && !IsLatin(preCharacter))) ||
					(!IsLatin(currentCharacter) && CHECK_HYP_BACK(preCharacter)) ||
					(!IsLatin(nextCharacter) && !CHECK_HYP_FRONT(nextCharacter) && !CHECK_HYP_BACK(currentCharacter)) ||
					(characterCount == tmpText.Length - 1))
				{
					if (line.Length == 1)
					{
						yield return new Word(null, currentCharacter);
					}
					else
					{
						yield return new Word(line.ToString(), default);
					}
					line.Length = 0;
					continue;
				}
			}
		}
		// static
		private static readonly Regex RITCH_TEXT_REPLACE = new Regex(
			"(\\<color=.*?\\>|</color>|" +
			"\\<size=.n\\>|</size>|" +
			"<b>|</b>|" +
			"<i>|</i>)");

		// 禁則処理 http://ja.wikipedia.org/wiki/%E7%A6%81%E5%89%87%E5%87%A6%E7%90%86
		// 行頭禁則文字
		private const string HYP_FRONT =
			(",)]｝、。）〕〉》」』】〙〗〟’”｠»" +// 終わり括弧類 簡易版
			 "ァィゥェォッャュョヮヵヶっぁぃぅぇぉっゃゅょゎ" +//行頭禁則和字 
			 "‐゠–〜ー" +//ハイフン類
			 "?!！？‼⁇⁈⁉" +//区切り約物
			 "・:;" +//中点類
			 "。.");//句点類

		private const string HYP_BACK =
			 "(（[｛〔〈《「『【〘〖〝‘“｟«";//始め括弧類

		private const string HYP_LATIN =
			("abcdefghijklmnopqrstuvwxyz" +
			 "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
			 "0123456789" +
			 "<>=/().,");

		private static bool CHECK_HYP_FRONT(char str)
		{
			return HYP_FRONT.IndexOf(str) >= 0;
		}

		private static bool CHECK_HYP_BACK(char str)
		{
			return HYP_BACK.IndexOf(str) >= 0;
		}

		private static bool IsLatin(char s)
		{
			return HYP_LATIN.IndexOf(s) >= 0;
		}
	}
}
