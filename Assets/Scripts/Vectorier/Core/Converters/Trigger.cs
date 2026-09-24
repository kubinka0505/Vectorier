using UnityEngine;

using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable

#pragma warning disable CS8600
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604

// -=-=-=- //

namespace DynamicExpressionSystem {
	// ENTRY POINT
	public static class DynamicExpression {
		public static Expression Expression(string expression) {
			return new Expression(expression);
		}
	}

	public sealed class Expression {
		private readonly string _expression;
		private Expr? _parsed;

		private static Dictionary<int, Expr> _cache = new Dictionary<int, Expr>();

		public Expression(string? expression = null) {
			_expression = expression ?? "";
		}

		private static int Hash(string input) {
			if (string.IsNullOrEmpty(input)) {
				return 0;
			}

			int h = 17;

			for (int i = 0; i < input.Length; i++) {
				char c = input[i];

				if (!char.IsWhiteSpace(c)) {
					h = h * 31 + c;
				}
			}

			return h;
		}

		private Expr Parse() {
			string expr = _expression.Trim();

			while (expr.EndsWith("|") || expr.EndsWith("&")) {
				expr = expr.Substring(0, expr.Length - 1).TrimEnd();
			}

			int h = Hash(expr);

			if (_cache.ContainsKey(h)) {
				_parsed = _cache[h];
				return _parsed;
			}

			Lexer lexer = new Lexer(expr);

			List<Token> tokens = Lexer.NormalizeTokens(lexer.Tokenize());

			Parser parser = new Parser(tokens);

			Expr parsed = parser.Parse();

			_parsed = Simplify(parsed);

			_cache[h] = _parsed ?? null;

			return _parsed;
		}

		// AST SIMPLIFICATION
		private Expr? Simplify(Expr? expr) {
			if (expr is BinaryExpr bin) {
				Expr? left = Simplify(bin.Left);
				Expr right = Simplify(bin.Right);

				// X == X -> X
				if (IsEqual(left, right)) {
					return left;
				}

				// AND identity: 0 & X -> 0
				if (bin.Operator == TokenType.And) {
					if (IsZero(left)) {
						return left;
					}

					if (IsZero(right)) {
						return right;
					}
				}

				return new BinaryExpr {
					Left = left,
					Right = right,
					Operator = bin.Operator
				};
			}

			return expr;
		}

		private bool IsEqual(Expr? a, Expr? b) {
			CompareExpr? ca = a as CompareExpr;
			CompareExpr? cb = b as CompareExpr;

			if (ca != null && cb != null) {
				return ca.Value == cb.Value &&
					ca.Comparison == cb.Comparison &&
				    ca.Negated == cb.Negated;
			}

			return false;
		}

		private bool IsZero(Expr? expr) {
			CompareExpr cmp = expr as CompareExpr;

			return cmp != null &&
			    !cmp.Negated &&
			    cmp.Comparison == TokenType.Equal &&
			    cmp.Value == "0";
		}

		// PUBLIC COMPILATION
		public XmlElement? CompileNumber(
			XmlDocument xml,
			string leftValue,
			string? defaultValue = null
		) {
			Expr expr = Parse();

			if (!string.IsNullOrEmpty(defaultValue)) {
				if (DynamicExpressionCompiler.IsEquivalentToSingleEqual(expr, defaultValue)) {
					return null;
				}
			}

			return DynamicExpressionCompiler.BuildPublic(expr, xml, leftValue);
		}
	}

	// TOKENS
	public enum TokenType {
		EOF,
		Number,
		And,
		Or,
		Not,
		LParen,
		RParen,
		Equal,
		Greater,
		Less,
		GreaterEqual,
		LessEqual
	}

	public sealed class Token {
		public TokenType Type;
		public string Value;

		public Token(TokenType type, string value = "") {
			Type = type;
			Value = value;
		}
	}

	// LEXER
	public sealed class Lexer {
		private readonly string _text;
		private int _pos;

		public Lexer(string text) {
			_text = text ?? "";
		}

		private char Current =>
			_pos >= _text.Length ? '\0' : _text[_pos];

		private void Advance() => _pos++;

		private bool IsStartOfNumber() {
			return char.IsDigit(Current) || (Current == '-' && PeekIsDigit());
		}

		private bool PeekIsDigit() {
			return (_pos + 1 < _text.Length) && char.IsDigit(_text[_pos + 1]);
		}

		public List<Token> Tokenize() {
			List<Token> tokens = new List<Token>();

			while (Current != '\0') {
				if (char.IsWhiteSpace(Current)) {
					Advance();
					continue;
				}

				if (IsStartOfNumber()) {
					string num = "";

					if (Current == '-') {
						num += Current;
						Advance();
					}

					if (!char.IsDigit(Current)) {
						throw new Exception("Invalid number");
					}

					while (char.IsDigit(Current)) {
						num += Current;
						Advance();
					}

					tokens.Add(new Token(TokenType.Number, num));
					continue;
				}

				switch (Current) {
					case '&':
						tokens.Add(new Token(TokenType.And));
						Advance();
						break;

					case '|':
						tokens.Add(new Token(TokenType.Or));
						Advance();
						break;

					case '!':
						tokens.Add(new Token(TokenType.Not));
						Advance();
						break;

					case '(':
						tokens.Add(new Token(TokenType.LParen));
						Advance();
						break;

					case ')':
						tokens.Add(new Token(TokenType.RParen));
						Advance();
						break;

					case '=':
						tokens.Add(new Token(TokenType.Equal));
						Advance();
						break;

					case '>':
						Advance();
						if (Current == '=') {
							Advance();
							tokens.Add(new Token(TokenType.GreaterEqual));
						} else {
							tokens.Add(new Token(TokenType.Greater));
						}
						break;

					case '<':
						Advance();
						if (Current == '=') {
							Advance();
							tokens.Add(new Token(TokenType.LessEqual));
						} else {
							tokens.Add(new Token(TokenType.Less));
						}
						break;

					default:
						throw new Exception("Unexpected char: " + Current);
				}
			}

			tokens.Add(new Token(TokenType.EOF));
			return tokens;
		}

		public static List<Token> NormalizeTokens(List<Token> tokens) {
			List<Token> result = new List<Token>();

			for (int i = 0; i < tokens.Count; i++) {
				Token t = tokens[i];

				if (t.Type == TokenType.EOF) {
					continue;
				}

				if (
					result.Count == 0 &&
					(t.Type == TokenType.And || t.Type == TokenType.Or)
				) {
					continue;
				}

				if (
					(t.Type == TokenType.And || t.Type == TokenType.Or) &&
					(i + 1 >= tokens.Count || tokens[i + 1].Type == TokenType.EOF)
				) {
					continue;
				}

				result.Add(t);
			}

			if (result.Count == 0 || result[result.Count - 1].Type != TokenType.EOF) {
				result.Add(new Token(TokenType.EOF));
			}

			return result;
		}
	}

	// =========================================================
	// AST
	// =========================================================

	public abstract class Expr {}

	public sealed class BinaryExpr : Expr {
		public Expr? Left = null;
		public Expr? Right = null;
		public TokenType? Operator = null;
	}

	public sealed class CompareExpr : Expr {
		public TokenType Comparison;
		public string? Value;
		public bool Negated;
	}

	// PARSER
	public sealed class Parser {
		private readonly List<Token> _tokens;
		private int _pos;

		public Parser(List<Token> tokens) {
			_tokens = tokens;
		}

		private Token Current {
			get {
				if (_pos >= _tokens.Count)
					return _tokens[_tokens.Count - 1];
				return _tokens[_pos];
			}
		}

		private Token Consume() {
			return _tokens[_pos++];
		}

		private bool Match(TokenType type) {
			if (Current.Type == type) {
				Consume();
				return true;
			}
			return false;
		}

		public Expr Parse() {
			return ParseOr();
		}

		private Expr ParseOr() {
			Expr expr = ParseAnd();

			while (Match(TokenType.Or)) {
				expr = new BinaryExpr {
					Left = expr,
					Right = ParseAnd(),
					Operator = TokenType.Or
				};
			}

			return expr;
		}

		private Expr ParseAnd(){
			Expr expr = ParseUnary();

			while (Match(TokenType.And)) {
				expr = new BinaryExpr {
					Left = expr,
					Right = ParseUnary(),
					Operator = TokenType.And
				};
			}

			return expr;
		}

		private Expr ParseUnary() {
			bool neg = false;

			while (Match(TokenType.Not))
				neg = !neg;

			Expr expr = ParsePrimary();

			CompareExpr? cmp = expr as CompareExpr;
			if (cmp != null) {
				cmp.Negated ^= neg;
			}

			return expr;
		}

		private Expr ParsePrimary() {
			if (Match(TokenType.LParen)) {
				Expr e = ParseOr();

				if (!Match(TokenType.RParen)) {
					throw new Exception("Missing ')'");
				}

				return e;
			}

			return ParseComparison();
		}

		private Expr ParseComparison() {
			TokenType comp = TokenType.Equal;

			switch (Current.Type) {
				case TokenType.Equal:
				case TokenType.Greater:
				case TokenType.Less:
				case TokenType.GreaterEqual:
				case TokenType.LessEqual:
					comp = Consume().Type;
					break;
			}

			if (Current.Type != TokenType.Number) {
				throw new Exception("Expected number");
			}

			return new CompareExpr {
				Comparison = comp,
				Value = Consume().Value
			};
		}
	}

	// COMPILER
	public static class DynamicExpressionCompiler {
		public static bool IsEquivalentToSingleEqual(Expr? expr, string value) {
			CompareExpr? cmp = expr as CompareExpr;

			return cmp != null &&
				!cmp.Negated &&
			    cmp.Comparison == TokenType.Equal &&
			    cmp.Value == value;
		}

		public static XmlElement? BuildPublic(Expr? expr, XmlDocument xml, string leftValue) {
			return Build(expr, xml, leftValue);
		}

		private static XmlElement? Build(Expr? expr, XmlDocument xml, string leftValue) {
			if (expr == null) return null;

			if (expr is BinaryExpr binary) {
				return BuildBinary(binary, xml, leftValue);
			}

			if (expr is CompareExpr compare) {
				return BuildCompare(compare, xml, leftValue);
			}

			return null;
		}

		private static XmlElement BuildBinary(BinaryExpr expr, XmlDocument xml, string leftValue) {
			XmlElement op = xml.CreateElement("Operator");

			op.SetAttribute("Type", expr.Operator == TokenType.And ? "And" : "Or");

			if (expr.Left != null) {
				var leftEl = Build(expr.Left, xml, leftValue);
				if (leftEl != null) op.AppendChild(leftEl);
			}

			if (expr.Right != null) {
				var rightEl = Build(expr.Right, xml, leftValue);
				if (rightEl != null) op.AppendChild(rightEl);
			}

			return op;
		}

		private static XmlElement BuildCompare(
			CompareExpr expr,
			XmlDocument xml,
			string leftValue
		) {
			string node = expr.Comparison == TokenType.Equal ? "Equal" :
				expr.Comparison == TokenType.Greater ? "Greater" :
				expr.Comparison == TokenType.Less ? "Less" :
				expr.Comparison == TokenType.GreaterEqual ? "GreaterEqual" :
				"LessEqual";

			XmlElement el = xml.CreateElement(node);

			if (expr.Comparison == TokenType.Equal) {
				el.SetAttribute("Value1", leftValue);
				el.SetAttribute("Value2", expr.Value);
			} else {
				el.SetAttribute("Value", leftValue);
				el.SetAttribute("Than", expr.Value);
			}

			if (expr.Negated) {
				el.SetAttribute("Not", "1");
			}

			return el;
		}
	}
}

namespace Vectorier.Core.Components {
	public static class TriggerComponent {
		public static void Convert(
			GameObject go,
			XmlNode node,
			XmlElement? parentElement = null,
			int floatPrecision = -1,
			bool legacy = false
		) {
			if (!TriggerRules.CanExport(go, legacy)) {
				return;
			}

			var data = TriggerExtractor.Extract(go, floatPrecision);
			if (data == null) {
				return;
			}

			var xml = node.OwnerDocument;
			var element = TriggerXmlBuilder.Build(
				go,
				data,
				xml,
				floatPrecision
			);

			if (element == null) {
				return;
			}

			XmlNode targetParent = parentElement ?? node.FirstChild;
			if (targetParent == null) {
				return;
			}

			var repeater = go.GetComponent<AppendRepeater>();
			if (repeater != null && repeater.enabled) {
				for (int i = 0; i < repeater.Multiplier; i++) {
					targetParent.AppendChild(element.CloneNode(true));
				}
			}

			targetParent.AppendChild(element);
		}

		internal sealed class TriggerData {
			public string? ObjectName = null;

			public string? SpriteName = null;

			public float X;
			public float Y;

			public float Width;
			public float Height;

			public TriggerSettings? StaticSettings;
			public DynamicTrigger? DynamicSettings;

			public int Precision;
		}

		internal static class TriggerRules {
			public static bool CanExport(GameObject go, bool legacy) {
				if (!Helpers.WriteMode.CanWrite(go, legacy)) {
					return false;
				}

				return true;
			}
		}

		internal static class TriggerExtractor {
			public static TriggerData? Extract(GameObject go, int precision) {
				var sr = go.GetComponent<SpriteRenderer>();

				if (sr == null || !sr.enabled || sr.sprite == null) {
					return null;
				}

				var staticTrigger = go.GetComponent<TriggerSettings>();
				var dynamicTrigger = go.GetComponent<DynamicTrigger>();

				if (staticTrigger != null && dynamicTrigger != null) {
					Debug.LogError(
						$"GameObject '{go.name}' cannot contain both TriggerSetting and DynamicTrigger, skipping.",
						go
					);
					return null;
				}

				if (staticTrigger == null && dynamicTrigger == null) {
					Debug.LogError(
						$"GameObject '{go.name}' must contain at least TriggerSetting or DynamicTrigger, skipping.",
						go
					);
					return null;
				}

				var norm = Vectorier.Core.Helpers.BoundNormalizer.ExtractNormalized(go, precision);

				return new TriggerData {
					ObjectName = Helpers.Get.Name(go),
					SpriteName = sr.sprite.name,

					X = norm.X * 100,
					Y = norm.Y * 100,
					Width = norm.Width * 10000,
					Height = norm.Height * 10000,

					StaticSettings = staticTrigger,
					DynamicSettings = dynamicTrigger,

					Precision = precision
				};
			}
		}

		internal static class TriggerXmlBuilder {
			public static XmlElement? Build(
				GameObject go,

				TriggerData? data,
				XmlDocument xml,

				int precision
			) {
				var el = xml.CreateElement("Trigger");

				if (
					data != null &&
					!string.IsNullOrEmpty(data.ObjectName) &&
					!data.ObjectName.StartsWith(">") &&
					data.ObjectName != data.SpriteName &&
					!string.IsNullOrEmpty(go.name)
				) {
					el.SetAttribute("Name", go.name);
				}

				if (data.X != 0) {
					el.SetAttribute("X", Helpers.Format.Multiply(data.X, precision));
				}

				if (data.Y != 0) {
					el.SetAttribute("Y", Helpers.Format.Multiply(-data.Y, precision));
				}

				if (data.Width != 0) {
					el.SetAttribute("Width", Helpers.Format.ToString(data.Width, precision));
				}

				if (data.Height != 0) {
					el.SetAttribute("Height", Helpers.Format.ToString(data.Height, precision));
				}

				if (data.StaticSettings != null && data.StaticSettings.enabled) {
					var content = TriggerHelpers.Build.Static(
						data.StaticSettings,
						xml
					);

					if (content.HasChildNodes) {
						el.AppendChild(content);
					}
				}

				if (data.DynamicSettings != null && data.DynamicSettings.enabled) {
					var content = TriggerHelpers.Build.Dynamic(
						go,
						data.DynamicSettings,
						xml
					);

					if (content.HasChildNodes) {
						el.AppendChild(content);
					}
				}

				Helpers.WriteMode.Apply(go, el);

				return el.HasChildNodes ? el : null;
			}
		}

		public static class TriggerHelpers {
			public static class Build {
				public static XmlElement Static(
					TriggerSettings settings,

					XmlDocument xml
				) {
					XmlElement contentElement = xml.CreateElement("Content");

					// try to parse settings.Content
					// fallback to fragment if it's not well-formed XML
					XmlDocument tempDoc = new XmlDocument();

					try {
						// wrapper allows multiple top-level nodes
						tempDoc.LoadXml($"<x>{settings.Content}</x>");

						foreach (XmlNode node in tempDoc.DocumentElement.ChildNodes) {
							// If the node is an outer <Content> element, append its children (unwrap it)
							if (
								node.NodeType == XmlNodeType.Element &&
								string.Equals(node.LocalName, "Content", StringComparison.OrdinalIgnoreCase)
							) {
								foreach (XmlNode inner in node.ChildNodes) {
									contentElement.AppendChild(xml.ImportNode(inner, true));
								}
							} else {
								contentElement.AppendChild(xml.ImportNode(node, true));
							}
						}
					} catch (XmlException) {
						// treat as fragment (best-effort)
						XmlDocumentFragment fragment = xml.CreateDocumentFragment();
						fragment.InnerXml = settings.Content ?? string.Empty;

						foreach (XmlNode child in fragment.ChildNodes) {
							// if a top-level fragment child is <Content>, unwrap it as above
							if (
								child.NodeType == XmlNodeType.Element &&
								string.Equals(child.LocalName, "Content", StringComparison.OrdinalIgnoreCase)
							) {
								foreach (XmlNode inner in child.ChildNodes) {
									contentElement.AppendChild(inner.CloneNode(true));
								}
							} else {
								contentElement.AppendChild(child.CloneNode(true));
							}
						}
					}

					return contentElement;
				}

				private static Dictionary<string, bool> parseSplittableAttributes(
					string attrs,
					GameObject go
				) {
					var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

					var parts = attrs
						.Split(Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray())
						.Select(s => s.Trim())
						.Where(s => !string.IsNullOrWhiteSpace(s));

					foreach (var part in parts) {
						if (part.StartsWith(">")) {
							var key = part.Substring(1).Trim();

							if (!string.IsNullOrEmpty(key))
								result[key] = false;
						}
						else {
							result[part] = true;
						}
					}

					return result;
				}

				public static XmlElement Dynamic(
					GameObject go,

					DynamicTrigger settings,

					XmlDocument xml
				) {
					XmlElement contentElement = xml.CreateElement("Content");
					XmlElement initElement = xml.CreateElement("Init");

					var dynamicComponents = go.transform.parent.GetComponents<Dynamic>();

					float accLatency = 0f;

					// build valid transformation name set
					HashSet<string> validNames = new HashSet<string>();

					if (settings.MultipleTransformation) {
						foreach (var name in settings.TransformationNames) {
							if (string.IsNullOrWhiteSpace(name)) {
								continue;
							}

							if (name.StartsWith(">")) {
								continue;
							}

							validNames.Add(name);
						}
					} else {
						if (!string.IsNullOrWhiteSpace(settings.TriggerTransformName)) {
							validNames.Add(settings.TriggerTransformName);
						}
					}

					// --- LATENCY CALCULATION ---
					if (settings.Latency < 0f) {
						float totalDuration = 0f;

						if (dynamicComponents != null && dynamicComponents.Length > 0) {
							foreach (var component in dynamicComponents) {
								if (!validNames.Contains(component.TransformationName)) {
									continue;
								}

								totalDuration += component.GetTotalLatency();
							}
						}

						// apply multiplier
						accLatency = totalDuration * Mathf.Abs(settings.Latency);
					} else {
						// direct value (0 or positive)
						accLatency = settings.Latency;
					}

					// Animations
					var splittedAnimations = parseSplittableAttributes(settings.Animations, go);

					// Sound & Flags
					var sounds = parseSplittableAttributes(settings.Sound, go);

					XmlElement svActive = xml.CreateElement("SetVariable");
					svActive.SetAttribute("Name", "$Active");
					svActive.SetAttribute("Value", "1");
					initElement.AppendChild(svActive);

					// AI
					var AIs = settings.AIAllowed
						.Split(Vectorier.Core.Game.Properties.AttributeSeparator.ToCharArray())
						.ToList();

					XmlElement svAI = xml.CreateElement("SetVariable");
					svAI.SetAttribute("Name", "$AI");

					int AiCount = AIs.Count;
					string AiVal = AiCount == 1 ? AIs[0] : "-1";

					if (string.IsNullOrEmpty(AiVal)) {
						AiVal = "0";
						Debug.LogWarning($@"GameObject named ""{go.name}"" had no AI attached to its dynamic trigger component, using defaulted value.", go);
					}

					svAI.SetAttribute("Value", AiVal);
					initElement.AppendChild(svAI);

					XmlElement svNode = xml.CreateElement("SetVariable");
					svNode.SetAttribute("Name", "$Node");
					svNode.SetAttribute("Value", string.IsNullOrEmpty(settings.modelNode) ? "COM" : settings.modelNode);
					initElement.AppendChild(svNode);

					if (settings.PlaySound && sounds.Count > 0) {
						XmlElement svSound = xml.CreateElement("SetVariable");
						svSound.SetAttribute("Name", "Sound");
						svSound.SetAttribute("Value", sounds.First().Key);
						initElement.AppendChild(svSound);
					}

					string usedKey = settings.KeyActivating.ToString();
					bool usesKey = usedKey != "Any";

					XmlElement svFlag = xml.CreateElement("SetVariable");
					svFlag.SetAttribute("Name", "Flag1");
					svFlag.SetAttribute("Value", "0");
					initElement.AppendChild(svFlag);

					contentElement.AppendChild(initElement);

					// Loop
					XmlElement loopElement = xml.CreateElement("Loop");

					// Events
					XmlElement eventsElement = xml.CreateElement("Events");

					// Block
					XmlElement eventBlock_EventType = xml.CreateElement("EventBlock");
					eventBlock_EventType.SetAttribute("Template", "FreqUsed." + settings.EventType.ToString());
					eventsElement.AppendChild(eventBlock_EventType);

					if (usesKey) {
						XmlElement eventBlock_Key = xml.CreateElement("EventBlock");
						eventBlock_Key.SetAttribute("Template", "FreqUsed.KeyPressed");
						eventsElement.AppendChild(eventBlock_Key);
					}

					// Condition
					XmlElement conditionsElement = xml.CreateElement("Conditions");

					// Actions
					XmlElement actionsElement = xml.CreateElement("Actions");

					if (!string.IsNullOrEmpty(settings.Animations)) {
						XmlElement eventBlock_ReqAnim = xml.CreateElement("EventBlock");

						eventBlock_ReqAnim.SetAttribute("Template", "CommonLib.RequiredAnimation");

						eventsElement.AppendChild(eventBlock_ReqAnim);
					}

					// Optimize single EventBlock
					if (eventsElement.ChildNodes.Count == 1 &&
						eventsElement.FirstChild is XmlElement singleEventBlock &&
						singleEventBlock.Name == "EventBlock"
					) {
						eventsElement.SetAttribute("Template", singleEventBlock.GetAttribute("Template"));
						eventsElement.RemoveChild(singleEventBlock);
					}

					loopElement.AppendChild(eventsElement);

					// OPERATORS

					// Operators
					XmlElement operatorOrElement = xml.CreateElement("Operator");
					operatorOrElement.SetAttribute("Type", "Or");

					XmlElement operatorAndElement = xml.CreateElement("Operator");
					operatorAndElement.SetAttribute("Type", "And");

					string aiExpression = settings.AIAllowed;

					if (!string.IsNullOrWhiteSpace(aiExpression)) {
						try {
							XmlElement compiled = DynamicExpressionSystem.DynamicExpression
								.Expression(aiExpression)
								.CompileNumber(xml, "?getModel[_$Model].AI", AiVal);

							if (compiled != null) {
								conditionsElement.AppendChild(compiled);
							}
						} catch (Exception e) {
							Debug.LogError($"Dynamic Trigger error: {e}", go);
						}
					}

					// Key
					if (usesKey) {
						// Original
						XmlElement keyNode = xml.CreateElement("Equal");

						keyNode.SetAttribute("Value1", "_$Key");
						keyNode.SetAttribute("Value2", usedKey);

						conditionsElement.AppendChild(keyNode);
					}

					// Direction
					DynamicTrigger.Directions direction = settings.Direction;

					if (direction != DynamicTrigger.Directions.Any) {
						XmlElement directionEqual = xml.CreateElement("Equal");

						directionEqual.SetAttribute("Value1", "?getModel[_$Model].direction");
						directionEqual.SetAttribute("Value2", direction == DynamicTrigger.Directions.FromLeft ? "1" : "-1");

						conditionsElement.AppendChild(directionEqual);
					}

					// Animations
					if (splittedAnimations != null && splittedAnimations.Count > 0) {
						foreach (var kv in splittedAnimations) {
							string name = kv.Key;
							bool enabled = kv.Value;

							XmlElement equalAnim = xml.CreateElement("Equal");
							equalAnim.SetAttribute("Value1", name);
							equalAnim.SetAttribute("Value2", "?getModel[_$Model].animationName");

							// disabled animations become negated
							if (!enabled) {
								equalAnim.SetAttribute("Not", "1");
							}

							if (splittedAnimations.Count > 1) {
								operatorOrElement.AppendChild(equalAnim);
							} else {
								conditionsElement.AppendChild(equalAnim);
							}
						}
					}

					// -=-=-=- //

					if (operatorOrElement.HasChildNodes) {
						conditionsElement.AppendChild(operatorOrElement);
					}

					if (operatorAndElement.HasChildNodes) {
						conditionsElement.AppendChild(operatorAndElement);
					}

					// -=-=-=- //

					if (conditionsElement.HasChildNodes) {
						loopElement.AppendChild(conditionsElement);
					}

					// ACTIONS

					// Sounds
					if (accLatency == 0 && settings.PlaySound) {
						if (sounds.Count == 1) {
							var kv = sounds.First();

							XmlElement sndElement = xml.CreateElement("Sound");
							sndElement.SetAttribute("Name", "_Sound");

							actionsElement.AppendChild(sndElement);
						} else if (sounds.Count > 1) {
							XmlElement sndChooseElement = xml.CreateElement("Choose");
							sndChooseElement.SetAttribute("Order", "Random");
							sndChooseElement.SetAttribute("Set", "1");

							foreach (var kv in sounds) {
								string sound = kv.Key;
								bool enabled = kv.Value;

								if (!enabled) {
									continue;
								}

								XmlElement sndElement = xml.CreateElement("Sound");
								sndElement.SetAttribute("Name", sound);

								sndChooseElement.AppendChild(sndElement);
							}

							if (sndChooseElement.HasChildNodes) {
								actionsElement.AppendChild(sndChooseElement);
							}
						}
					}

					XmlElement transformElement = xml.CreateElement("Transform");

					var settingsParent = go.GetComponentInParent<Dynamic>();

					string? finalName = null;

					// Validate trigger name
					if (
						!string.IsNullOrEmpty(settings.TriggerTransformName) &&
						settings.TriggerTransformName.StartsWith("_")
					) {
						Debug.LogError(
							"Dynamic trigger name cannot start with \"_\", node entering bounds will result in game crash.",
							go
						);
					}

					// Warn if mismatch
					if (settingsParent != null &&
						!string.IsNullOrEmpty(settings.TriggerTransformName) &&
						settings.TriggerTransformName != settingsParent.TransformationName)
					{
						// TODO: change the model so each transformation detect its "child" (bonded) trigger
						//Debug.LogWarning("Trigger name does not match its parent transformation name", go);
					}

					// Decide final name
					if (string.IsNullOrEmpty(settings.TriggerTransformName) &&
						settingsParent != null &&
						!string.IsNullOrEmpty(settingsParent.TransformationName)
					) {
						finalName = settingsParent.TransformationName;
					} else {
						finalName = settings.TriggerTransformName;
					}

					transformElement.SetAttribute("Name", finalName);

					// accLatency
					if (accLatency > 0) {
						XmlElement chooseElement = xml.CreateElement("Choose");
						chooseElement.SetAttribute("Order", "Sync");
						//chooseElement.SetAttribute("Set", "1");
						chooseElement.AppendChild(transformElement);

						XmlElement nestedChooseElement = xml.CreateElement("Choose");
						nestedChooseElement.SetAttribute("Order", "Straight");
						//nestedChooseElement.SetAttribute("Set", "1");

						XmlElement waitElement = xml.CreateElement("Wait");
						waitElement.SetAttribute(
							"Frames",
							Math.Round(accLatency * Vectorier.Core.Game.Properties.FrameRate).ToString()
						);
						nestedChooseElement.AppendChild(waitElement);

						// Sounds
						if (settings.PlaySound && sounds.Count > 0) {
							if (sounds.Count == 1) {
								// single sound so always _Sound
								XmlElement sndElement = xml.CreateElement("Sound");
								sndElement.SetAttribute("Name", "_Sound");
								nestedChooseElement.AppendChild(sndElement);
							} else {
								XmlElement sndChooseElement = xml.CreateElement("Choose");
								sndChooseElement.SetAttribute("Order", "Random");
								sndChooseElement.SetAttribute("Set", "1");

								foreach (var kv in sounds) {
									string sound = kv.Key;
									bool enabled = kv.Value;

									if (!enabled) {
										continue;
									}

									XmlElement sndElement = xml.CreateElement("Sound");
									sndElement.SetAttribute("Name", sound);

									sndChooseElement.AppendChild(sndElement);
								}

								if (sndChooseElement.HasChildNodes) {
									nestedChooseElement.AppendChild(sndChooseElement);
								}
							}
						}

						if (!settings.Reusable) {
							XmlElement setActiveElement = xml.CreateElement("SetVariable");
							setActiveElement.SetAttribute("Name", "$Active");
							setActiveElement.SetAttribute("Value", "0");
							actionsElement.AppendChild(setActiveElement);
						}

						chooseElement.AppendChild(nestedChooseElement);
						actionsElement.AppendChild(chooseElement);
					} else {
						if (!settings.Reusable) {
							XmlElement setActiveElement = xml.CreateElement("SetVariable");
							setActiveElement.SetAttribute("Name", "$Active");
							setActiveElement.SetAttribute("Value", "0");
							actionsElement.AppendChild(setActiveElement);
						}

						actionsElement.AppendChild(transformElement);
					}

					if (actionsElement.HasChildNodes) {
						loopElement.AppendChild(actionsElement);
					}

					if (loopElement.HasChildNodes) {
						contentElement.AppendChild(loopElement);
					}

					return contentElement;
				}
			}
		}
	}
}