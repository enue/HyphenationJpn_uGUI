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

			public bool StartsWithNewLine
			{
				get
				{
					if (Text == null)
					{
						return Character == '\n' || Character == '\r';
					}
					if (Text.Length == 0)
					{
						return false;
					}
					var start = Text[0];
					return start == '\n' || start == '\r';
				}
			}

			public bool EndsWithNewLine
			{
				get
				{
					if (Text == null)
					{
						return Character == '\n' || Character == '\r';
					}
					if (Text.Length == 0)
					{
						return false;
					}
					var end = Text[Text.Length - 1];
					return end == '\n' || end == '\r';
				}
			}
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
				if (character == '\r' || character == '\n')
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
			if (string.IsNullOrEmpty(message))
			{
				return string.Empty;
			}

			font.RequestCharactersInTexture(message, fontSize, fontStyle);

			// work
			StringBuilder lineBuilder = new StringBuilder();

			float lineWidth = 0f;
			foreach (var word in GetWordList(message))
			{
				if (word.EndsWithNewLine)
				{
					lineWidth = 0f;
				}
				else if (word.Text == null)
				{
					float textWidth = GetCharacterWidth(font, fontSize, fontStyle, word.Character);
					if (lineWidth + textWidth > rectWidth)
					{
						if (lineWidth != 0f)
						{
							lineBuilder.Append(Environment.NewLine);
							lineWidth = 0f;
						}
					}
					lineWidth += textWidth;
					lineBuilder.Append(word.Character);
				}
				else if (supportRichText)
				{
					float textWidth = GetLastLineWidth(font, fontSize, fontStyle, word.Text, supportRichText);
					if (lineWidth + textWidth > rectWidth)
					{
						if (lineWidth != 0f)
						{
							if (!word.StartsWithNewLine)
							{
								lineBuilder.Append(Environment.NewLine);
								lineWidth = 0f;
							}
						}
					}
					lineWidth += textWidth;
					lineBuilder.Append(word.Text);
				}
				else
				{
					var processingIndex = 0;
					var enableForceNewLine = false;
					for(int i=0; i<word.Text.Length; ++i)
					{
						var character = word.Text[i];
						if (character == '\n' || character == '\r')
						{
							for (int j = processingIndex; j <= i; ++j)
							{
								lineBuilder.Append(word.Text[j]);
							}
							processingIndex = i+1;
							lineWidth = 0f;
						}
						else
						{
							var characterWidth = GetCharacterWidth(font, fontSize, fontStyle, character);
							if (lineWidth == 0f)
							{
								lineWidth = characterWidth;
							}
							else if (lineWidth + characterWidth > rectWidth)
							{
								lineBuilder.Append(Environment.NewLine);
								lineWidth = 0f;
								for (int j = processingIndex; j <= i; ++j)
								{
									lineBuilder.Append(word.Text[j]);
									lineWidth += GetCharacterWidth(font, fontSize, fontStyle, word.Text[j]);
								}
								processingIndex = i + 1;
								enableForceNewLine = true;
							}
							else if (enableForceNewLine)
							{
								lineBuilder.Append(character);
								lineWidth += characterWidth;
								processingIndex = i + 1;
							}
							else
							{
								lineWidth += characterWidth;
							}
						}
					}
					for (int i = processingIndex; i < word.Text.Length; ++i)
					{
						lineBuilder.Append(word.Text[i]);
					}

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
