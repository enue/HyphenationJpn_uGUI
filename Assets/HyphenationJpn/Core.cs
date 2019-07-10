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

			public int Length
			{
				get
				{
					if (Text == null)
					{
						return 1;
					}
					return Text.Length;
				}
			}

			public bool StartsWithNewLine
			{
				get
				{
					if (Text == null)
					{
						return Character == '\n';
					}
					if (Text.Length == 0)
					{
						return false;
					}
					var start = Text[0];
					return start == '\n';
				}
			}

			public bool EndsWithNewLine
			{
				get
				{
					if (Text == null)
					{
						return Character == '\n';
					}
					if (Text.Length == 0)
					{
						return false;
					}
					var end = Text[Text.Length - 1];
					return end == '\n';
				}
			}
		}

		public static float GetTextWidth(string message, Font font, int fontSize, FontStyle fontStyle, bool richText, bool updateTexture = true)
		{
			if (richText)
			{
				message = RITCH_TEXT_REPLACE.Replace(message, string.Empty);
			}

			if (updateTexture)
			{
				font.RequestCharactersInTexture(message, fontSize, fontStyle);
			}

			var lineWidth = 0f;
			var maxWidth = 0f;
			foreach (var it in message)
			{
				if (it == '\n')
				{
					maxWidth = Mathf.Max(maxWidth, lineWidth);
					lineWidth = 0f;
				}
				else
				{
					lineWidth += GetCharacterWidth(font, fontSize, fontStyle, it);
				}
			}

			return Mathf.Max(maxWidth, lineWidth);
		}

		static float GetLastLineWidth(Font font, int fontSize, FontStyle fontStyle, string message, bool supportRichText)
		{
			if (supportRichText)
			{
				message = RITCH_TEXT_REPLACE.Replace(message, string.Empty);
			}
			float lineWidth = 0f;
			foreach (var character in message)
			{
				if (character == '\n')
				{
					lineWidth = 0f;
				}
				else
				{
					lineWidth += GetCharacterWidth(font, fontSize, fontStyle, character);
				}
			}
			return lineWidth;
		}

		static float GetCharacterWidth(Font font, int fontSize, FontStyle fontStyle, char character)
		{
			var foundInfo = font.GetCharacterInfo(character, out var info, fontSize, fontStyle);
			UnityEngine.Assertions.Assert.IsTrue(foundInfo, "not found character info : " + character);
			return info.advance;
		}

		public static string GetFormattedText(Text text, string message)
		{
			return GetFormattedText(text.rectTransform.rect.width, text.font, text.fontSize, text.fontStyle, message, text.supportRichText);
		}

		public static string GetFormattedText(float rectWidth, Font font, int fontSize, FontStyle fontStyle, string message, bool supportRichText)
		{
			var newLinePositions = GetNewLinePositions(rectWidth, font, fontSize, fontStyle, message, supportRichText);

			newLinePositions.Sort();
			for (int i = 0; i < newLinePositions.Count; ++i)
			{
				var position = newLinePositions[newLinePositions.Count - 1 - i];
				message = message.Insert(position, "\n");
			}
			return message;
		}

		public static List<int> GetNewLinePositions(float rectWidth, Font font, int fontSize, FontStyle fontStyle, string message, bool supportRichText)
		{
			var result = new List<int>();

			if (string.IsNullOrEmpty(message))
			{
				return result;
			}

			font.RequestCharactersInTexture(message, fontSize, fontStyle);

			// work
			var currentPosition = 0;

			float lineWidth = 0f;
			foreach (var word in GetWordList(message))
			{
				if (word.EndsWithNewLine)
				{
					lineWidth = 0f;
					currentPosition += word.Length;
				}
				else if (word.Text == null)
				{
					float textWidth = GetCharacterWidth(font, fontSize, fontStyle, word.Character);
					if (lineWidth != 0f && lineWidth + textWidth > rectWidth)
					{
						result.Add(currentPosition);
						lineWidth = 0f;
					}
					lineWidth += textWidth;
					currentPosition += word.Length;
				}
				else if (supportRichText)
				{
					float textWidth = GetLastLineWidth(font, fontSize, fontStyle, word.Text, supportRichText);
					if (word.StartsWithNewLine)
					{
						lineWidth = 0f;
					}
					else if (lineWidth != 0f && lineWidth + textWidth > rectWidth)
					{
						result.Add(currentPosition);
						lineWidth = 0f;
					}
					lineWidth += textWidth;
					currentPosition += word.Length;
				}
				else
				{
					var textWidth = GetLastLineWidth(font, fontSize, fontStyle, word.Text, supportRichText);
					if (lineWidth + textWidth <= rectWidth)
					{
						if (word.StartsWithNewLine)
						{
							lineWidth = 0f;
						}
						lineWidth += textWidth;
						currentPosition += word.Length;
					}
					else if (textWidth <= rectWidth)
					{
						if (word.StartsWithNewLine)
						{
							lineWidth = 0f;
						}
						else if (lineWidth != 0f)
						{
							result.Add(currentPosition);
							lineWidth = 0f;
						}
						lineWidth += textWidth;
						currentPosition += word.Length;
					}
					else
					{
						// wordの横幅がrectの横幅を超える場合は禁則を無視して改行するしかない
						foreach(var character in word.Text)
						{
							if (character == '\n')
							{
								++currentPosition;
								lineWidth = 0f;
							}
							else
							{
								var characterWidth = GetCharacterWidth(font, fontSize, fontStyle, character);
								if (lineWidth > 0f && lineWidth + characterWidth > rectWidth)
								{
									result.Add(currentPosition);
									lineWidth = 0f;
								}
								++currentPosition;
								lineWidth += characterWidth;
							}
						}
					}
				}
			}

			return result;
		}

		static private IEnumerable<Word> GetWordList(string tmpText)
		{
			StringBuilder line = new StringBuilder();
			char emptyChar = new char();

			for (int characterCount = 0; characterCount < tmpText.Length; characterCount++)
			{
				char currentCharacter = tmpText[characterCount];
				char nextCharacter = (characterCount < tmpText.Length - 1) ? tmpText[characterCount + 1] : emptyChar;
				char preCharacter = (characterCount > 0) ? tmpText[characterCount - 1] : emptyChar;

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
			 "<>=/().,#");

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
