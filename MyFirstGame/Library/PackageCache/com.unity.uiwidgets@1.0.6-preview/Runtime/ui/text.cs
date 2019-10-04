﻿using System;
using System.Collections.Generic;

namespace Unity.UIWidgets.ui {
    public enum FontStyle {
        normal,
        italic,
    }

    public enum TextBaseline {
        alphabetic,
        ideographic,
    }

    public enum TextAlign {
        left,
        right,
        center,
        justify,
    }

    public class ParagraphConstraints : IEquatable<ParagraphConstraints> {
        public readonly float width;

        public ParagraphConstraints(float width) {
            this.width = width;
        }

        public bool Equals(ParagraphConstraints other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return this.width.Equals(other.width);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((ParagraphConstraints) obj);
        }

        public override int GetHashCode() {
            return this.width.GetHashCode();
        }

        public static bool operator ==(ParagraphConstraints left, ParagraphConstraints right) {
            return Equals(left, right);
        }

        public static bool operator !=(ParagraphConstraints left, ParagraphConstraints right) {
            return !Equals(left, right);
        }

        public override string ToString() {
            return $"Width: {this.width}";
        }
    }

    class TextStyle : IEquatable<TextStyle> {
        public readonly Color color = Color.fromARGB(255, 0, 0, 0);
        public readonly float fontSize = 14.0f;
        public readonly FontWeight fontWeight = FontWeight.w400;
        public readonly FontStyle fontStyle = FontStyle.normal;
        public readonly float letterSpacing = 0.0f;
        public readonly float wordSpacing = 0.0f;
        public readonly TextBaseline textBaseline = TextBaseline.alphabetic;
        public readonly float height = 1.0f;
        public readonly TextDecoration decoration = TextDecoration.none;
        public readonly Color decorationColor;
        public readonly TextDecorationStyle decorationStyle = TextDecorationStyle.solid;
        public readonly string fontFamily = "Helvetica";
        public readonly Paint background;

        internal UnityEngine.Color UnityColor {
            get { return this.color.toColor(); }
        }

        internal UnityEngine.FontStyle UnityFontStyle {
            get {
                bool isBold = this.fontWeight.index == FontWeight.bold.index;
                if (this.fontStyle == FontStyle.italic) {
                    if (isBold) {
                        return UnityEngine.FontStyle.BoldAndItalic;
                    }
                    else {
                        return UnityEngine.FontStyle.Italic;
                    }
                }
                else if (isBold) {
                    return UnityEngine.FontStyle.Bold;
                }

                return UnityEngine.FontStyle.Normal;
            }
        }

        internal int UnityFontSize {
            get { return (int) this.fontSize; }
        }

        public static TextStyle applyStyle(TextStyle currentStyle, painting.TextStyle style, float textScaleFactor) {
            if (currentStyle != null) {
                return new TextStyle(
                    color: style.color ?? currentStyle.color,
                    fontSize: style.fontSize != null ? style.fontSize * textScaleFactor : currentStyle.fontSize,
                    fontWeight: style.fontWeight ?? currentStyle.fontWeight,
                    fontStyle: style.fontStyle ?? currentStyle.fontStyle,
                    letterSpacing: style.letterSpacing ?? currentStyle.letterSpacing,
                    wordSpacing: style.wordSpacing ?? currentStyle.wordSpacing,
                    textBaseline: style.textBaseline ?? currentStyle.textBaseline,
                    height: style.height ?? currentStyle.height,
                    decoration: style.decoration ?? currentStyle.decoration,
                    decorationColor: style.decorationColor ?? currentStyle.decorationColor,
                    fontFamily: style.fontFamily ?? currentStyle.fontFamily,
                    background: style.background ?? currentStyle.background
                );
            }

            return new TextStyle(
                color: style.color,
                fontSize: style.fontSize * textScaleFactor,
                fontWeight: style.fontWeight,
                fontStyle: style.fontStyle,
                letterSpacing: style.letterSpacing,
                wordSpacing: style.wordSpacing,
                textBaseline: style.textBaseline,
                height: style.height,
                decoration: style.decoration,
                decorationColor: style.decorationColor,
                fontFamily: style.fontFamily,
                background: style.background
            );
        }

        public bool Equals(TextStyle other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return Equals(this.color, other.color) && this.fontSize.Equals(other.fontSize) &&
                   this.fontWeight == other.fontWeight && this.fontStyle == other.fontStyle &&
                   this.letterSpacing.Equals(other.letterSpacing) && this.wordSpacing.Equals(other.wordSpacing) &&
                   this.textBaseline == other.textBaseline && this.height.Equals(other.height) &&
                   Equals(this.decoration, other.decoration) &&
                   Equals(this.decorationColor, other.decorationColor) &&
                   this.decorationStyle == other.decorationStyle &&
                   string.Equals(this.fontFamily, other.fontFamily);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((TextStyle) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (this.color != null ? this.color.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.fontSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.fontWeight != null ? this.fontWeight.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.fontStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ this.letterSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ this.wordSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ this.textBaseline.GetHashCode();
                hashCode = (hashCode * 397) ^ this.height.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.decoration != null ? this.decoration.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (this.decorationColor != null ? this.decorationColor.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.decorationStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.fontFamily != null ? this.fontFamily.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(TextStyle left, TextStyle right) {
            return Equals(left, right);
        }

        public static bool operator !=(TextStyle left, TextStyle right) {
            return !Equals(left, right);
        }


        public TextStyle(Color color = null, float? fontSize = null,
            FontWeight fontWeight = null, FontStyle? fontStyle = null, float? letterSpacing = null,
            float? wordSpacing = null, TextBaseline? textBaseline = null, float? height = null,
            TextDecoration decoration = null, TextDecorationStyle? decorationStyle = null, Color decorationColor = null,
            string fontFamily = null,
            Paint background = null
        ) {
            this.color = color ?? this.color;
            this.fontSize = fontSize ?? this.fontSize;
            this.fontWeight = fontWeight ?? this.fontWeight;
            this.fontStyle = fontStyle ?? this.fontStyle;
            this.letterSpacing = letterSpacing ?? this.letterSpacing;
            this.wordSpacing = wordSpacing ?? this.wordSpacing;
            this.fontSize = fontSize ?? this.fontSize;
            this.textBaseline = textBaseline ?? this.textBaseline;
            this.height = height ?? this.height;
            this.decoration = decoration ?? this.decoration;
            this.decorationStyle = decorationStyle ?? this.decorationStyle;
            this.decorationColor = decorationColor ?? this.decorationColor;
            this.fontFamily = fontFamily ?? this.fontFamily;
            this.background = background ?? this.background;
        }
    }

    public class ParagraphStyle : IEquatable<ParagraphStyle> {
        public ParagraphStyle(TextAlign? textAlign = null,
            TextDirection? textDirection = null,
            FontWeight fontWeight = null,
            FontStyle? fontStyle = null,
            int? maxLines = null,
            float? fontSize = null,
            string fontFamily = null,
            float? lineHeight = null, // todo  
            string ellipsis = null) {
            this.textAlign = textAlign;
            this.textDirection = textDirection;
            this.fontWeight = fontWeight;
            this.fontStyle = fontStyle;
            this.maxLines = maxLines;
            this.fontSize = fontSize;
            this.fontFamily = fontFamily;
            this.lineHeight = lineHeight;
            this.ellipsis = ellipsis;
        }

        public bool Equals(ParagraphStyle other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return this.textAlign == other.textAlign && this.textDirection == other.textDirection &&
                   this.fontWeight == other.fontWeight && this.fontStyle == other.fontStyle &&
                   this.maxLines == other.maxLines && this.fontSize.Equals(other.fontSize) &&
                   string.Equals(this.fontFamily, other.fontFamily) && this.lineHeight.Equals(other.lineHeight) &&
                   string.Equals(this.ellipsis, other.ellipsis);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((ParagraphStyle) obj);
        }

        public static bool operator ==(ParagraphStyle a, ParagraphStyle b) {
            return Equals(a, b);
        }

        public static bool operator !=(ParagraphStyle a, ParagraphStyle b) {
            return !(a == b);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = this.textAlign.GetHashCode();
                hashCode = (hashCode * 397) ^ this.textDirection.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.fontWeight != null ? this.fontWeight.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.fontStyle.GetHashCode();
                hashCode = (hashCode * 397) ^ this.maxLines.GetHashCode();
                hashCode = (hashCode * 397) ^ this.fontSize.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.fontFamily != null ? this.fontFamily.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ this.lineHeight.GetHashCode();
                hashCode = (hashCode * 397) ^ (this.ellipsis != null ? this.ellipsis.GetHashCode() : 0);
                return hashCode;
            }
        }

        internal TextStyle getTextStyle() {
            return new TextStyle(
                fontWeight: this.fontWeight,
                fontStyle: this.fontStyle,
                fontFamily: this.fontFamily,
                fontSize: this.fontSize,
                height: this.lineHeight
            );
        }

        public TextAlign TextAlign {
            get { return this.textAlign ?? TextAlign.left; }
        }

        public readonly TextAlign? textAlign;
        public readonly TextDirection? textDirection;
        public readonly FontWeight fontWeight;
        public readonly FontStyle? fontStyle;
        public readonly int? maxLines;
        public readonly float? fontSize;
        public readonly string fontFamily;
        public readonly float? lineHeight;
        public readonly string ellipsis;

        public bool ellipsized() {
            return !string.IsNullOrEmpty(this.ellipsis);
        }
    }

    public enum TextDecorationStyle {
        solid,
        doubleLine,
    }

    public class TextDecoration : IEquatable<TextDecoration> {
        public bool Equals(TextDecoration other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return this.mask == other.mask;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((TextDecoration) obj);
        }

        public override int GetHashCode() {
            return this.mask;
        }


        public static bool operator ==(TextDecoration a, TextDecoration b) {
            return Equals(a, b);
        }

        public static bool operator !=(TextDecoration a, TextDecoration b) {
            return !(a == b);
        }

        public static readonly TextDecoration none = new TextDecoration(0);

        public static readonly TextDecoration underline = new TextDecoration(1);

        public static readonly TextDecoration overline = new TextDecoration(2);

        public static readonly TextDecoration lineThrough = new TextDecoration(4);

        public readonly int mask;

        public TextDecoration(int mask) {
            this.mask = mask;
        }

        public bool contains(TextDecoration other) {
            return (this.mask | other.mask) == this.mask;
        }
    }

    public enum TextDirection {
        rtl,
        ltr,
    }

    public enum TextAffinity {
        upstream,
        downstream,
    }

    public class FontWeight : IEquatable<FontWeight> {
        FontWeight(int index) {
            this.index = index;
        }

        public readonly int index;

        public static readonly FontWeight w100 = new FontWeight(0);
        public static readonly FontWeight w200 = new FontWeight(1);
        public static readonly FontWeight w300 = new FontWeight(2);
        public static readonly FontWeight w400 = new FontWeight(3);
        public static readonly FontWeight w500 = new FontWeight(4);
        public static readonly FontWeight w600 = new FontWeight(5);

        public static readonly FontWeight w700 = new FontWeight(6);

        public static readonly FontWeight w800 = new FontWeight(7);

        public static readonly FontWeight w900 = new FontWeight(8);


        public static readonly FontWeight normal = w400;


        public static readonly FontWeight bold = w700;

        public static readonly List<FontWeight> values = new List<FontWeight> {
            w100, w200, w300, w400, w500, w600, w700, w800, w900
        };

        public static readonly Dictionary<int, int> indexToFontWeight = new Dictionary<int, int> {
            {0, 100},
            {1, 200},
            {2, 300},
            {3, 400},
            {4, 500},
            {5, 600},
            {6, 700},
            {7, 800},
            {8, 900},
        };

        public bool Equals(FontWeight other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return this.index == other.index;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((FontWeight) obj);
        }

        public override int GetHashCode() {
            return this.index;
        }

        public static bool operator ==(FontWeight left, FontWeight right) {
            return Equals(left, right);
        }

        public static bool operator !=(FontWeight left, FontWeight right) {
            return !Equals(left, right);
        }

        public override string ToString() {
            return $"FontWeight.w{this.weightValue}";
        }

        public int weightValue {
            get { return indexToFontWeight[this.index]; }
        }
    }


    public class TextPosition : IEquatable<TextPosition> {
        public readonly int offset;
        public readonly TextAffinity affinity;

        public TextPosition(int offset, TextAffinity affinity = TextAffinity.downstream) {
            this.offset = offset;
            this.affinity = affinity;
        }

        public override string ToString() {
            return $"Offset: {this.offset}, Affinity: {this.affinity}";
        }

        public bool Equals(TextPosition other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return this.offset == other.offset && this.affinity == other.affinity;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((TextPosition) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return (this.offset * 397) ^ (int) this.affinity;
            }
        }

        public static bool operator ==(TextPosition left, TextPosition right) {
            return Equals(left, right);
        }

        public static bool operator !=(TextPosition left, TextPosition right) {
            return !Equals(left, right);
        }
    }

    public class TextBox : IEquatable<TextBox> {
        public readonly float left;

        public readonly float top;

        public readonly float right;

        public readonly float bottom;

        public readonly TextDirection direction;

        TextBox(float left, float top, float right, float bottom, TextDirection direction) {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
            this.direction = direction;
        }

        public static TextBox fromLTBD(float left, float top, float right, float bottom, TextDirection direction) {
            return new TextBox(left, top, right, bottom, direction);
        }

        public Rect toRect() {
            return Rect.fromLTRB(this.left, this.top, this.right, this.bottom);
        }

        public float start {
            get { return this.direction == TextDirection.ltr ? this.left : this.right; }
        }

        public float end {
            get { return this.direction == TextDirection.ltr ? this.right : this.left; }
        }

        public bool Equals(TextBox other) {
            if (ReferenceEquals(null, other)) {
                return false;
            }

            if (ReferenceEquals(this, other)) {
                return true;
            }

            return this.left.Equals(other.left) && this.top.Equals(other.top) && this.right.Equals(other.right) &&
                   this.bottom.Equals(other.bottom) && this.direction == other.direction;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) {
                return false;
            }

            if (ReferenceEquals(this, obj)) {
                return true;
            }

            if (obj.GetType() != this.GetType()) {
                return false;
            }

            return this.Equals((TextBox) obj);
        }

        public override string ToString() {
            return
                $"Left: {this.left}, Top: {this.top}, Right: {this.right}, Bottom: {this.bottom}, Direction: {this.direction}";
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = this.left.GetHashCode();
                hashCode = (hashCode * 397) ^ this.top.GetHashCode();
                hashCode = (hashCode * 397) ^ this.right.GetHashCode();
                hashCode = (hashCode * 397) ^ this.bottom.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) this.direction;
                return hashCode;
            }
        }

        public static bool operator ==(TextBox left, TextBox right) {
            return Equals(left, right);
        }

        public static bool operator !=(TextBox left, TextBox right) {
            return !Equals(left, right);
        }
    }
}